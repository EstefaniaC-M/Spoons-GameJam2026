using System;
using System.Collections.Generic;

// Plain C#: independent of the UI, beast movement, and Cinemachine.
public static class DijkstraPathfinder
{
    public struct Edge
    {
        public int From;
        public int To;
        public float Cost;

        public Edge(int from, int to, float cost)
        {
            From = from;
            To = to;
            Cost = cost;
        }
    }

    // Connections are undirected: each edge is usable in both directions.
    // Returns a path including start and goal, or an empty list if unreachable.
    public static List<int> FindPath(
        int nodeCount, IReadOnlyList<Edge> edges, int start, int goal,
        ISet<int> blockedNodes = null)
    {
        if (nodeCount <= 0 || start < 0 || start >= nodeCount ||
            goal < 0 || goal >= nodeCount)
            throw new ArgumentOutOfRangeException(nameof(nodeCount));

        if (blockedNodes != null &&
            (blockedNodes.Contains(start) || blockedNodes.Contains(goal)))
            return new List<int>();

        var adjacency = new List<Edge>[nodeCount];
        for (int i = 0; i < nodeCount; i++)
            adjacency[i] = new List<Edge>();

        foreach (Edge edge in edges)
        {
            if (edge.From < 0 || edge.From >= nodeCount ||
                edge.To < 0 || edge.To >= nodeCount ||
                float.IsNaN(edge.Cost) || float.IsInfinity(edge.Cost) ||
                edge.Cost < 0f)
                throw new ArgumentException("Edges need valid nodes and finite nonnegative costs.");

            adjacency[edge.From].Add(edge);
            adjacency[edge.To].Add(new Edge(edge.To, edge.From, edge.Cost));
        }

        var distance = new float[nodeCount];
        var previous = new int[nodeCount];
        var visited = new bool[nodeCount];
        for (int i = 0; i < nodeCount; i++)
        {
            distance[i] = float.PositiveInfinity;
            previous[i] = -1;
        }
        distance[start] = 0f;

        // O(V^2 + E), appropriate for a small room graph.
        for (int step = 0; step < nodeCount; step++)
        {
            int current = -1;
            float best = float.PositiveInfinity;
            for (int i = 0; i < nodeCount; i++)
            {
                if (!visited[i] && distance[i] < best)
                {
                    best = distance[i];
                    current = i;
                }
            }

            if (current == -1)
                break;
            if (current == goal)
                break;

            visited[current] = true;
            foreach (Edge edge in adjacency[current])
            {
                if (visited[edge.To] ||
                    (blockedNodes != null && blockedNodes.Contains(edge.To)))
                    continue;

                float candidate = distance[current] + edge.Cost;
                if (candidate < distance[edge.To])
                {
                    distance[edge.To] = candidate;
                    previous[edge.To] = current;
                }
            }
        }

        var path = new List<int>();
        if (float.IsPositiveInfinity(distance[goal]))
            return path;

        for (int node = goal; node != -1; node = previous[node])
            path.Add(node);
        path.Reverse();
        return path;
    }
}
