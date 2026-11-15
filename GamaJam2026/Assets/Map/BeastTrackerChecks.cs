using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Editor-only: no NUnit or test assembly setup needed.
public static class BeastTrackerChecks
{
    [MenuItem("Tools/Beast Tracker/Check Node Colors (Play mode)")]
    public static void CheckNodeColors()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Enter Play mode before running the node color checks.");
            return;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");
        if (shader == null)
            throw new Exception("No supported color shader found for the node color checks.");

        Material shared = new Material(shader);
        Color normal = Color.gray;
        shared.color = normal;
        GameObject a = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        GameObject b = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        a.name = "TemporaryTrackerColorCheck_A";
        b.name = "TemporaryTrackerColorCheck_B";
        Renderer rendererA = a.GetComponent<Renderer>();
        Renderer rendererB = b.GetComponent<Renderer>();
        rendererA.sharedMaterial = shared;
        rendererB.sharedMaterial = shared;

        try
        {
            TrackerNode nodeA = a.AddComponent<TrackerNode>();
            TrackerNode nodeB = b.AddComponent<TrackerNode>();
            nodeA.SetBeastPresent(true);
            ExpectColor(rendererA, Color.red, "starting beast node");
            ExpectColor(rendererB, normal, "other node remains unchanged");

            nodeA.SetBeastPresent(false);
            nodeB.SetBeastPresent(true);
            ExpectColor(rendererA, normal, "previous beast node restores original color");
            ExpectColor(rendererB, Color.red, "new beast node turns red");

            nodeA.SetLit(true);
            ExpectColor(rendererA, new Color(1f, 0.75f, 0.1f), "light color");
            nodeA.SetLit(false);
            ExpectColor(rendererA, normal, "light expiration restores original color");

            nodeA.SetPlayerPresent(true);
            ExpectColor(rendererA, Color.cyan, "player node color");
            nodeA.SetBeastPresent(true);
            ExpectColor(rendererA, Color.red, "beast remains visible on player node");
            nodeA.SetLit(false);
            ExpectColor(rendererA, Color.red, "light update preserves beast color");
            nodeA.SetBeastPresent(false);
            ExpectColor(rendererA, Color.cyan, "player color returns after beast leaves");
            if (shared.color != normal)
                throw new Exception("The shared material asset was changed.");

            Debug.Log("Beast Tracker: all node color checks passed.");
        }
        finally
        {
            UnityEngine.Object.Destroy(a);
            UnityEngine.Object.Destroy(b);
            UnityEngine.Object.Destroy(shared);
        }
    }

    private static void ExpectColor(Renderer renderer, Color expected, string label)
    {
        if (renderer.material.color != expected)
            throw new Exception("Node color check failed: " + label);
    }

    [MenuItem("Tools/Beast Tracker/Check Dijkstra")]
    public static void CheckDijkstra()
    {
        // A=0, B=1, C=2, D=3, E=4, P=5.
        var edges = new List<DijkstraPathfinder.Edge>
        {
            new DijkstraPathfinder.Edge(0, 1, 2f),
            new DijkstraPathfinder.Edge(1, 2, 2f),
            new DijkstraPathfinder.Edge(0, 3, 1f),
            new DijkstraPathfinder.Edge(3, 4, 1f),
            new DijkstraPathfinder.Edge(4, 5, 2f),
            new DijkstraPathfinder.Edge(1, 4, 2f),
            new DijkstraPathfinder.Edge(2, 5, 3f)
        };

        Expect(6, edges, 0, 5, "0,3,4,5");
        Expect(6, edges, 5, 0, "5,4,3,0");
        Expect(6, edges, 0, 2, "0,1,2");
        Expect(6, edges, 2, 5, "2,5");
        Expect(6, edges, 3, 5, "3,4,5");
        Expect(6, edges, 1, 5, "1,4,5");
        Expect(6, edges, 4, 4, "4");
        Expect(7, edges, 0, 6, "");
        Expect(1, new List<DijkstraPathfinder.Edge>(), 0, 0, "0");

        // A two-edge path is cheaper than a single expensive edge.
        Expect(3, new List<DijkstraPathfinder.Edge>
        {
            new DijkstraPathfinder.Edge(0, 2, 10f),
            new DijkstraPathfinder.Edge(0, 1, 1f),
            new DijkstraPathfinder.Edge(1, 2, 1f)
        }, 0, 2, "0,1,2");

        Expect(6, edges, 0, 5, "0,1,4,5", new HashSet<int> { 3 });
        Expect(6, edges, 0, 5, "0,1,2,5", new HashSet<int> { 4 });
        Expect(6, edges, 0, 5, "", new HashSet<int> { 1, 3 });
        Expect(6, edges, 0, 5, "", new HashSet<int> { 5 });
        Expect(6, edges, 0, 5, "0,3,4,5", new HashSet<int>());

        bool rejectedNegative = false;
        try
        {
            DijkstraPathfinder.FindPath(2, new List<DijkstraPathfinder.Edge>
            {
                new DijkstraPathfinder.Edge(0, 1, -1f)
            }, 0, 1);
        }
        catch (ArgumentException) { rejectedNegative = true; }
        if (!rejectedNegative)
            throw new Exception("Dijkstra accepted a negative cost.");

        Debug.Log("Beast Tracker: all 16 Dijkstra checks passed.");
    }

    private static void Expect(int count, IReadOnlyList<DijkstraPathfinder.Edge> edges,
        int start, int goal, string expected, ISet<int> blocked = null)
    {
        string actual = string.Join(",", DijkstraPathfinder.FindPath(count, edges, start, goal, blocked));
        if (actual != expected)
            throw new Exception("Dijkstra " + start + " -> " + goal +
                ": expected " + expected + ", got " + actual);
    }
}
