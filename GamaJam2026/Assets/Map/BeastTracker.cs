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
    [SerializeField] private TrackerNode beastStart;
    [SerializeField] private TrackerNode playerNode;
    [Tooltip("Optional. Leave empty to use every TrackerNode in the scene. " +
             "Nodes reached through Neighbors lists are added automatically.")]
    [SerializeField] private TrackerNode[] nodes;
    [Tooltip("Optional. Corridors normally come from each node's Neighbors list; " +
             "use this only to give a corridor a custom cost.")]
    [SerializeField] private Connection[] connections;
    [Tooltip("On: Neighbors corridors cost their length. Off: every corridor costs 1.")]
    [SerializeField] private bool neighborCostIsDistance = true;

    [Header("Movement opportunities (FNAF-style)")]
    [Tooltip("Seconds between movement opportunities.")]
    [SerializeField, Min(0.1f)] private float opportunityInterval = 5f;
    [Tooltip("Aggression / AI level. Each opportunity rolls 1..Roll Max; " +
             "the beast moves if the roll is <= this. 0 = never moves, Roll Max = always moves.")]
    [SerializeField, Min(0)] private int aggression = 10;
    [Tooltip("Sides on the movement die (FNAF uses 20).")]
    [SerializeField, Min(1)] private int rollMax = 20;

    [Header("Retreat when blocked")]
    [Tooltip("How many steps the beast runs away from the player when a light blocks its way.")]
    [SerializeField, Min(0)] private int retreatSteps = 3;
    [Tooltip("Random delay between retreat steps, for erratic scrambling.")]
    [SerializeField, Min(0.05f)] private float retreatStepDelayMin = 0.2f;
    [SerializeField, Min(0.05f)] private float retreatStepDelayMax = 0.8f;

    [Header("Lights")]
    [SerializeField, Min(0.1f)] private float lightDuration = 6f;
    [Tooltip("How many different nodes may be lit at once. Active timers cannot be refreshed.")]
    [SerializeField, Min(1)] private int maximumActiveLights = 1;
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
    private float[] distanceToPlayer;
    private TrackerNode currentNode;
    private float nextMoveTime;
    private int retreatStepsLeft;
    private float nextRetreatTime;
    private bool initialized;
    private bool running;

    public TrackerNode CurrentNode => currentNode;
    public bool IsRunning => running;
    public bool IsRetreating => retreatStepsLeft > 0;

    // Raise this over time (e.g. per night or per minute) to ramp difficulty.
    public int Aggression
    {
        get => aggression;
        set => aggression = Mathf.Max(0, value);
    }

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
        if (requireTrackerView && (brain == null || trackerCamera == null))
            return ConfigurationError("Assign Brain and Tracker Camera, or disable Require Tracker View for prototyping.");
        if (beastStart == null || playerNode == null)
            return ConfigurationError("Assign Beast Start and Player Node.");
        if (connections == null)
            connections = new Connection[0];

        // Collect nodes: the Nodes list (or the whole scene if it is empty),
        // plus start, player, and anything reachable through Neighbors.
        var collected = new List<TrackerNode>();
        void AddNode(TrackerNode node)
        {
            if (node != null && !collected.Contains(node))
                collected.Add(node);
        }
        if (nodes != null)
            foreach (TrackerNode node in nodes)
                AddNode(node);
        if (collected.Count == 0)
            foreach (TrackerNode node in FindObjectsByType<TrackerNode>(FindObjectsSortMode.None))
                AddNode(node);
        AddNode(beastStart);
        AddNode(playerNode);
        foreach (Connection connection in connections)
        {
            if (connection != null)
            {
                AddNode(connection.a);
                AddNode(connection.b);
            }
        }
        for (int i = 0; i < collected.Count; i++)
            foreach (TrackerNode neighbor in collected[i].Neighbors)
                AddNode(neighbor);
        nodes = collected.ToArray();

        indices.Clear();
        edges.Clear();
        for (int i = 0; i < nodes.Length; i++)
            indices.Add(nodes[i], i);

        // Each corridor is added once, whichever side lists it.
        var seen = new HashSet<(int, int)>();
        bool TryMarkCorridor(int a, int b) =>
            seen.Add(a < b ? (a, b) : (b, a));

        foreach (Connection connection in connections)
        {
            if (connection == null || connection.a == null || connection.b == null ||
                connection.a == connection.b || connection.cost <= 0f ||
                float.IsNaN(connection.cost) || float.IsInfinity(connection.cost))
                return ConfigurationError("Connections need two different nodes and a finite positive cost.");
            int a = indices[connection.a], b = indices[connection.b];
            if (TryMarkCorridor(a, b))
                edges.Add(new DijkstraPathfinder.Edge(a, b, connection.cost));
        }

        foreach (TrackerNode node in nodes)
        {
            foreach (TrackerNode neighbor in node.Neighbors)
            {
                if (neighbor == null || neighbor == node)
                    continue;
                int a = indices[node], b = indices[neighbor];
                if (!TryMarkCorridor(a, b))
                    continue;
                float cost = neighborCostIsDistance
                    ? Mathf.Max(0.01f, Vector3.Distance(node.transform.position, neighbor.transform.position))
                    : 1f;
                edges.Add(new DijkstraPathfinder.Edge(a, b, cost));
            }
        }

        foreach (TrackerNode node in nodes)
        {
            if (DijkstraPathfinder.FindPath(nodes.Length, edges,
                indices[node], indices[playerNode]).Count == 0)
                return ConfigurationError("All nodes must initially connect to the player. Disconnected node: " + node.name);
        }

        // Map distance to the player (ignoring lights), used to pick retreat directions.
        distanceToPlayer = DijkstraPathfinder.DistancesFrom(nodes.Length, edges, indices[playerNode]);

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

        // While scrambling away, retreat steps replace movement opportunities.
        if (retreatStepsLeft > 0)
        {
            if (Time.time >= nextRetreatTime)
                RetreatOneStep();
            return;
        }

        if (Time.time >= nextMoveTime)
        {
            MovementOpportunity();
            if (running && retreatStepsLeft == 0)
                ScheduleNextMove();
        }
    }

    // FNAF-style: roll 1..rollMax; move only if the roll is <= aggression.
    private void MovementOpportunity()
    {
        int roll = UnityEngine.Random.Range(1, Mathf.Max(1, rollMax) + 1);
        bool success = roll <= aggression;
        if (logPaths)
            Debug.Log($"Beast opportunity: rolled {roll} vs aggression {aggression} -> " +
                (success ? "MOVE" : "stay"), this);
        if (success)
            AdvanceOneNode();
    }

    public bool TryActivateLight(TrackerNode node)
    {
        if (!initialized || !running || node == null || !indices.ContainsKey(node))
            return false;
        if (requireTrackerView &&
            (brain.IsBlending || brain.ActiveVirtualCamera != trackerCamera))
            return false;

        // Clicking a lit node never refreshes its timer, even with spare capacity.
        if (lightExpiry.ContainsKey(node))
            return false;

        // Each expired light frees one slot. Existing lights keep their timers.
        if (lightExpiry.Count >= Mathf.Max(1, maximumActiveLights))
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
            Debug.Log(path.Count == 0 ? "Beast: lights cut off every route; it will retreat on its next move." :
                "Beast route: " + string.Join(" -> ", labels), this);
        }
    }

    private void AdvanceOneNode()
    {
        // Recheck immediately before a step; never use a stale blocked route.
        Replan();

        // Its preferred route ignores lights. If a light sits on the very next
        // room of that route, or lights cut off every route, it has been stopped.
        List<int> direct = DijkstraPathfinder.FindPath(
            nodes.Length, edges, indices[currentNode], indices[playerNode]);
        bool lightInFace = direct.Count >= 2 && blocked.Contains(direct[1]);
        if (lightInFace || path.Count == 0)
        {
            StartRetreat();
            return;
        }
        if (path.Count < 2)
            return; // Already at the player.

        SetCurrentNode(nodes[path[1]]);
        if (currentNode == playerNode)
        {
            running = false;
            Debug.Log("Beast reached the player.", this);
            onPlayerReached.Invoke();
            return;
        }
        Replan();
    }

    private void StartRetreat()
    {
        if (retreatSteps <= 0)
        {
            if (logPaths)
                Debug.Log("Beast is blocked at " + currentNode.name + " and waits.", this);
            return;
        }
        if (logPaths)
            Debug.Log("Beast is blocked at " + currentNode.name + "! Retreating " +
                retreatSteps + " steps.", this);
        retreatStepsLeft = retreatSteps;
        nextRetreatTime = Time.time + RetreatDelay();
    }

    // Each retreat step goes to the unlit neighbor farthest from the player.
    // It stops early if no neighbor is farther away (cornered).
    private void RetreatOneStep()
    {
        int currentIndex = indices[currentNode];
        float bestDistance = distanceToPlayer[currentIndex];
        var best = new List<int>();

        foreach (int neighbor in NeighborsOf(currentIndex))
        {
            if (blocked.Contains(neighbor) || neighbor == indices[playerNode])
                continue;
            float d = distanceToPlayer[neighbor];
            if (d > bestDistance + 0.0001f)
            {
                bestDistance = d;
                best.Clear();
                best.Add(neighbor);
            }
            else if (best.Count > 0 && Mathf.Abs(d - bestDistance) <= 0.0001f)
            {
                best.Add(neighbor);
            }
        }

        if (best.Count == 0)
        {
            if (logPaths)
                Debug.Log("Beast is cornered at " + currentNode.name + "; retreat ends.", this);
            EndRetreat();
            return;
        }

        TrackerNode next = nodes[best[UnityEngine.Random.Range(0, best.Count)]];
        if (logPaths)
            Debug.Log("Beast retreats: " + currentNode.name + " -> " + next.name, this);
        SetCurrentNode(next);

        retreatStepsLeft--;
        if (retreatStepsLeft > 0)
            nextRetreatTime = Time.time + RetreatDelay();
        else
            EndRetreat();
    }

    private void EndRetreat()
    {
        retreatStepsLeft = 0;
        Replan();
        ScheduleNextMove();
    }

    private float RetreatDelay()
    {
        float minimum = Mathf.Max(0.05f, retreatStepDelayMin);
        float maximum = Mathf.Max(minimum, retreatStepDelayMax);
        return UnityEngine.Random.Range(minimum, maximum);
    }

    private IEnumerable<int> NeighborsOf(int index)
    {
        foreach (DijkstraPathfinder.Edge edge in edges)
        {
            if (edge.From == index)
                yield return edge.To;
            else if (edge.To == index)
                yield return edge.From;
        }
    }

    private void ScheduleNextMove()
    {
        nextMoveTime = Time.time + Mathf.Max(0.1f, opportunityInterval);
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
        retreatStepsLeft = 0;
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
        opportunityInterval = Mathf.Max(0.1f, opportunityInterval);
        rollMax = Mathf.Max(1, rollMax);
        aggression = Mathf.Max(0, aggression);
        retreatSteps = Mathf.Max(0, retreatSteps);
        retreatStepDelayMin = Mathf.Max(0.05f, retreatStepDelayMin);
        retreatStepDelayMax = Mathf.Max(retreatStepDelayMin, retreatStepDelayMax);
        lightDuration = Mathf.Max(0.1f, lightDuration);
        maximumActiveLights = Mathf.Max(1, maximumActiveLights);
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
