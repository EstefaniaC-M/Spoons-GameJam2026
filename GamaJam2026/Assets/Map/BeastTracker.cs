using System;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.Events;

// Navigation state is independent of how the map is drawn.
public class BeastTracker : MonoBehaviour
{
    [Serializable]
    public class Connection
    {
        public TrackerNode a;
        public TrackerNode b;
        [Min(0.01f)] public float cost = 1f;
    }

    [Header("Graph")]
    [SerializeField] private TrackerNode[] nodes;
    [SerializeField] private Connection[] connections;
    [SerializeField] private TrackerNode beastStart;
    [SerializeField] private TrackerNode playerNode;

    [Header("Timing")]
    [SerializeField, Min(0.1f)] private float minimumMoveWait = 2f;
    [SerializeField, Min(0.1f)] private float maximumMoveWait = 5f;
    [SerializeField, Min(0.1f)] private float lightDuration = 6f;
    [SerializeField] private bool startAutomatically = true;

    [Header("Optional visual markers; can be assigned later")]
    [SerializeField] private Transform beastMarker;
    [SerializeField] private Transform playerMarker;
    [Tooltip("World-space offset from each node position, for placing dots above the drawing.")]
    [SerializeField] private Vector3 markerOffset = Vector3.zero;

    [Header("Camera interaction")]
    [Tooltip("Turn off only when testing nodes before the tracker camera is set up.")]
    [SerializeField] private bool requireTrackerView = true;
    [SerializeField] private CinemachineBrain brain;
    [SerializeField] private CinemachineVirtualCamera trackerCamera;

    [Header("Debug / game events")]
    [SerializeField] private bool logPaths = true;
    public UnityEvent onPlayerReached = new UnityEvent();

    private readonly Dictionary<TrackerNode, int> indices =
        new Dictionary<TrackerNode, int>();
    private readonly List<DijkstraPathfinder.Edge> edges =
        new List<DijkstraPathfinder.Edge>();
    private readonly Dictionary<TrackerNode, float> lightExpiry =
        new Dictionary<TrackerNode, float>();
    private readonly List<TrackerNode> expiredLights = new List<TrackerNode>();
    private readonly HashSet<int> blocked = new HashSet<int>();

    private List<int> path = new List<int>();
    private TrackerNode currentNode;
    private float nextMoveTime;
    private bool initialized;
    private bool running;

    public TrackerNode CurrentNode => currentNode;
    public bool IsRunning => running;

    private void Start()
    {
        if (!BuildGraph())
        {
            enabled = false;
            return;
        }
        foreach (TrackerNode node in nodes)
            node.Bind(this);

        initialized = true;
        ResetState();
        if (startAutomatically)
            BeginSimulation();
    }

    private bool BuildGraph()
    {
        if (nodes == null || nodes.Length == 0 || connections == null)
            return ConfigurationError("Assign the Nodes and Connections lists.");
        if (requireTrackerView && (brain == null || trackerCamera == null))
            return ConfigurationError("Assign Brain and Tracker Camera, or disable Require Tracker View for prototyping.");

        indices.Clear();
        edges.Clear();
        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i] == null || indices.ContainsKey(nodes[i]))
                return ConfigurationError("Assign each node exactly once.");
            indices.Add(nodes[i], i);
        }

        if (beastStart == null || playerNode == null ||
            !indices.ContainsKey(beastStart) || !indices.ContainsKey(playerNode))
            return ConfigurationError("Beast Start and Player Node must be in Nodes.");

        foreach (Connection connection in connections)
        {
            if (connection == null || connection.a == null || connection.b == null ||
                !indices.ContainsKey(connection.a) || !indices.ContainsKey(connection.b) ||
                connection.a == connection.b || connection.cost <= 0f ||
                float.IsNaN(connection.cost) || float.IsInfinity(connection.cost))
                return ConfigurationError("Connections need two different listed nodes and a finite positive cost.");
            edges.Add(new DijkstraPathfinder.Edge(
                indices[connection.a], indices[connection.b], connection.cost));
        }

        foreach (TrackerNode node in nodes)
        {
            if (DijkstraPathfinder.FindPath(nodes.Length, edges,
                indices[node], indices[playerNode]).Count == 0)
                return ConfigurationError("All nodes must initially connect to the player. Disconnected node: " + node.name);
        }

        // A marker must be a separate visual, never a graph node or its hierarchy.
        beastMarker = ValidateMarker(beastMarker, "Beast Marker");
        playerMarker = ValidateMarker(playerMarker, "Player Marker");
        return true;
    }

    private Transform ValidateMarker(Transform marker, string fieldName)
    {
        if (marker == null)
            return null;
        foreach (TrackerNode node in nodes)
        {
            if (marker == node.transform || node.transform.IsChildOf(marker) ||
                marker.IsChildOf(node.transform))
            {
                Debug.LogWarning(fieldName + " must be separate from the graph nodes. " +
                    "Ignoring this reference; use node colors or a separate marker object.", this);
                return null;
            }
        }
        return marker;
    }

    private bool ConfigurationError(string message)
    {
        Debug.LogError("BeastTracker: " + message, this);
        return false;
    }

    private void Update()
    {
        if (!initialized || !running)
            return;

        // Expiry is checked before movement, so routing sees current light state.
        expiredLights.Clear();
        foreach (KeyValuePair<TrackerNode, float> entry in lightExpiry)
        {
            if (Time.time >= entry.Value)
                expiredLights.Add(entry.Key);
        }
        foreach (TrackerNode node in expiredLights)
        {
            lightExpiry.Remove(node);
            node.SetLit(false);
        }
        if (expiredLights.Count > 0)
            Replan();

        if (Time.time >= nextMoveTime)
        {
            AdvanceOneNode();
            if (running)
                ScheduleNextMove();
        }
    }

    public bool TryActivateLight(TrackerNode node)
    {
        if (!initialized || !running || node == null || !indices.ContainsKey(node))
            return false;
        if (requireTrackerView &&
            (brain.IsBlending || brain.ActiveVirtualCamera != trackerCamera))
            return false;

        // First-version rule: no light directly under the beast or player.
        if (node == currentNode || node == playerNode)
            return false;

        lightExpiry[node] = Time.time + Mathf.Max(0.1f, lightDuration);
        node.SetLit(true);
        Replan();
        return true;
    }

    private void Replan()
    {
        blocked.Clear();
        foreach (TrackerNode node in lightExpiry.Keys)
            blocked.Add(indices[node]);

        path = DijkstraPathfinder.FindPath(
            nodes.Length, edges, indices[currentNode], indices[playerNode], blocked);

        if (logPaths)
        {
            var labels = new List<string>();
            foreach (int index in path)
                labels.Add(nodes[index].name);
            Debug.Log(path.Count == 0 ? "Beast: no path to player; will wander if an unlit neighbor is available." :
                "Beast route: " + string.Join(" -> ", labels), this);
        }
    }

    private void AdvanceOneNode()
    {
        // Recheck immediately before a step; never use a stale blocked route.
        Replan();

        TrackerNode next;
        if (path.Count >= 2)
        {
            // An open route always takes priority over wandering.
            next = nodes[path[1]];
        }
        else if (path.Count == 0)
        {
            next = GetRandomUnlitNeighbor();
            if (next == null)
            {
                if (logPaths)
                    Debug.Log("Beast is trapped at " + currentNode.name + "; no unlit neighbors.", this);
                return;
            }
            if (logPaths)
                Debug.Log("Beast wanders: " + currentNode.name + " -> " + next.name, this);
        }
        else
        {
            // A one-node path means it is already at the destination.
            return;
        }

        SetCurrentNode(next);
        if (currentNode == playerNode)
        {
            running = false;
            Debug.Log("Beast reached the player.", this);
            onPlayerReached.Invoke();
            return;
        }
        Replan();
    }

    private TrackerNode GetRandomUnlitNeighbor()
    {
        int currentIndex = indices[currentNode];
        var candidates = new List<int>();

        // Use the same validated, bidirectional edges as Dijkstra.
        foreach (DijkstraPathfinder.Edge edge in edges)
        {
            int neighbor = -1;
            if (edge.From == currentIndex)
                neighbor = edge.To;
            else if (edge.To == currentIndex)
                neighbor = edge.From;

            if (neighbor < 0 || neighbor == currentIndex ||
                blocked.Contains(neighbor) || candidates.Contains(neighbor))
                continue;

            candidates.Add(neighbor);
        }

        if (candidates.Count == 0)
            return null;

        int choice = UnityEngine.Random.Range(0, candidates.Count);
        return nodes[candidates[choice]];
    }

    private void ScheduleNextMove()
    {
        float minimum = Mathf.Max(0.1f, minimumMoveWait);
        float maximum = Mathf.Max(minimum, maximumMoveWait);
        nextMoveTime = Time.time + UnityEngine.Random.Range(minimum, maximum);
    }

    private void UpdateMarkers()
    {
        if (beastMarker != null)
            beastMarker.position = currentNode.transform.position + markerOffset;
        if (playerMarker != null)
            playerMarker.position = playerNode.transform.position + markerOffset;
    }

    private void SetCurrentNode(TrackerNode newNode)
    {
        if (currentNode != null)
            currentNode.SetBeastPresent(false);

        currentNode = newNode;
        currentNode.SetBeastPresent(true);
        UpdateMarkers();
    }

    private void ResetState()
    {
        running = false;
        lightExpiry.Clear();
        blocked.Clear();
        foreach (TrackerNode node in nodes)
        {
            node.SetBeastPresent(false);
            node.SetPlayerPresent(node == playerNode);
            node.SetLit(false);
        }
        SetCurrentNode(beastStart);
        Replan();
    }

    public void BeginSimulation()
    {
        if (!initialized || running)
            return;
        running = true;
        if (currentNode == playerNode)
        {
            running = false;
            onPlayerReached.Invoke();
            return;
        }
        ScheduleNextMove();
    }

    [ContextMenu("Restart Simulation (Play mode)")]
    public void RestartSimulation()
    {
        if (!Application.isPlaying || !initialized)
            return;
        ResetState();
        BeginSimulation();
    }

    private void OnValidate()
    {
        minimumMoveWait = Mathf.Max(0.1f, minimumMoveWait);
        maximumMoveWait = Mathf.Max(minimumMoveWait, maximumMoveWait);
        lightDuration = Mathf.Max(0.1f, lightDuration);
    }

    private void OnDrawGizmos()
    {
        // Scene-view authoring aid; the final 2D drawing can be added later.
        if (connections == null)
            return;
        Gizmos.color = new Color(0.3f, 0.8f, 0.6f);
        foreach (Connection connection in connections)
        {
            if (connection != null && connection.a != null && connection.b != null)
                Gizmos.DrawLine(connection.a.transform.position, connection.b.transform.position);
        }
    }
}
