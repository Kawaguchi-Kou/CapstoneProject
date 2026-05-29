using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Application.Interfaces;

namespace Application.Services
{
    /// <summary>
    /// Singleton service that loads vietnam_phuot_graph.json once and answers
    /// Yen's K-shortest-paths queries for the route-suggestion feature.
    /// </summary>
    public class RouteGraphService : IRouteGraphService
    {
        // ─────────────────────────────────────────────
        // Graph data
        // ─────────────────────────────────────────────
        private readonly Dictionary<string, GraphNode> _nodes;
        // adjacency: nodeId → list of (neighborId, edge)
        private readonly Dictionary<string, List<(string Neighbor, GraphEdge Edge)>> _adj;

        public IReadOnlyDictionary<string, GraphNode> Nodes => _nodes;

        // ─────────────────────────────────────────────
        // Constructor — loads the JSON at startup
        // ─────────────────────────────────────────────
        public RouteGraphService(string graphJsonPath)
        {
            var json = File.ReadAllText(graphJsonPath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement.GetProperty("graph");

            // Parse nodes
            _nodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase);
            foreach (var n in root.GetProperty("nodes").EnumerateArray())
            {
                var id    = n.GetProperty("id").GetString()!;
                var label = n.GetProperty("label").GetString()!;
                var type  = n.GetProperty("type").GetString()!;
                _nodes[id] = new GraphNode { Id = id, Label = label, Type = type };
            }

            // Parse edges → undirected adjacency list
            _adj = new Dictionary<string, List<(string, GraphEdge)>>(StringComparer.OrdinalIgnoreCase);

            foreach (var n in _nodes.Keys)
                _adj[n] = new List<(string, GraphEdge)>();

            foreach (var e in root.GetProperty("edges").EnumerateArray())
            {
                var src  = e.GetProperty("source").GetString()!;
                var tgt  = e.GetProperty("target").GetString()!;
                var dist = e.GetProperty("distance_km").GetDouble();
                var rt   = e.GetProperty("route_type").GetString()!;

                var edge = new GraphEdge { Source = src, Target = tgt, DistanceKm = dist, RouteType = rt };
                var rev  = new GraphEdge { Source = tgt, Target = src, DistanceKm = dist, RouteType = rt };

                if (!_adj.ContainsKey(src)) _adj[src] = new();
                if (!_adj.ContainsKey(tgt)) _adj[tgt] = new();

                _adj[src].Add((tgt, edge));
                _adj[tgt].Add((src, rev));
            }
        }

        // ─────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────

        /// <summary>
        /// Match <paramref name="startLabel"/> and <paramref name="endLabel"/>
        /// against graph node labels (Vietnamese names).
        /// Falls back to node-id match if label doesn't hit.
        /// </summary>
        public List<RoutePath> FindTopKPaths(string startLabel, string endLabel, int k = 5)
        {
            var startId = ResolveNodeId(startLabel);
            var endId   = ResolveNodeId(endLabel);

            if (startId == null || endId == null || startId == endId)
                return new List<RoutePath>();

            return YenKShortestPaths(startId, endId, k);
        }

        // ─────────────────────────────────────────────
        // Label → node-id resolution
        // ─────────────────────────────────────────────
        private string? ResolveNodeId(string label)
        {
            if (string.IsNullOrWhiteSpace(label)) return null;

            var trimmed = label.Trim();

            // 1. Exact label match
            var byLabel = _nodes.Values
                .FirstOrDefault(n => Normalize(n.Label) == Normalize(trimmed));
            if (byLabel != null) return byLabel.Id;

            // 2. Exact id match
            if (_nodes.ContainsKey(trimmed)) return trimmed;

            // 3. Contains match (pick shortest label to avoid over-matching)
            var contains = _nodes.Values
                .Where(n => n.Label.Contains(trimmed, StringComparison.OrdinalIgnoreCase)
                         || trimmed.Contains(n.Label, StringComparison.OrdinalIgnoreCase))
                .OrderBy(n => n.Label.Length)
                .FirstOrDefault();

            return contains?.Id;
        }

        // ─────────────────────────────────────────────
        // Yen's K-Shortest Paths (Yen 1971)
        // ─────────────────────────────────────────────
        private List<RoutePath> YenKShortestPaths(string src, string dst, int k)
        {
            // A — confirmed shortest paths
            var A = new List<RoutePath>();
            // B — candidate paths (min-heap by total distance)
            var B = new SortedList<double, Queue<RoutePath>>();

            // 1st shortest path via Dijkstra
            var first = Dijkstra(src, dst, forbiddenEdges: null, forbiddenNodes: null);
            if (first == null) return A;

            A.Add(first);

            for (int ki = 1; ki < k; ki++)
            {
                var prev = A[ki - 1];

                for (int i = 0; i < prev.Nodes.Count - 1; i++)
                {
                    var spurNode  = prev.Nodes[i];
                    var rootPath  = prev.Nodes.GetRange(0, i + 1);
                    var rootDist  = PathDistanceTo(prev, i);

                    // Forbidden edges: any edge that shares the root path with already-found paths
                    var forbiddenEdges = new HashSet<(string, string)>();
                    foreach (var p in A)
                    {
                        if (p.Nodes.Count > i
                            && p.Nodes.GetRange(0, i + 1).SequenceEqual(rootPath))
                        {
                            forbiddenEdges.Add((p.Nodes[i], p.Nodes[i + 1]));
                        }
                    }

                    // Forbidden nodes: all root nodes except spurNode itself
                    var forbiddenNodes = new HashSet<string>(rootPath.SkipLast(1));

                    var spurPath = Dijkstra(spurNode, dst, forbiddenEdges, forbiddenNodes);
                    if (spurPath == null) continue;

                    // totalPath = root + spur (without the duplicate spurNode)
                    var totalNodes = rootPath
                        .Concat(spurPath.Nodes.Skip(1))
                        .ToList();

                    var totalEdges = BuildEdgesForPath(totalNodes);
                    var totalDist  = totalEdges.Sum(e => e.DistanceKm);

                    var candidate = new RoutePath
                    {
                        Nodes         = totalNodes,
                        Edges         = totalEdges,
                        TotalDistanceKm = totalDist
                    };

                    // avoid duplicates already in B
                    bool alreadyInB = false;
                    foreach (var queue in B.Values)
                        if (queue.Any(p => p.Nodes.SequenceEqual(totalNodes)))
                        { alreadyInB = true; break; }

                    if (!alreadyInB)
                    {
                        var key = totalDist;
                        // ensure unique double key
                        while (B.ContainsKey(key)) key += 1e-9;
                        if (!B.ContainsKey(key)) B[key] = new Queue<RoutePath>();
                        B[key].Enqueue(candidate);
                    }
                }

                if (!B.Any()) break;

                var bestKey   = B.Keys[0];
                var bestRoute = B[bestKey].Dequeue();
                if (!B[bestKey].Any()) B.Remove(bestKey);

                A.Add(bestRoute);
            }

            return A;
        }

        // ─────────────────────────────────────────────
        // Dijkstra
        // ─────────────────────────────────────────────
        private RoutePath? Dijkstra(
            string src,
            string dst,
            HashSet<(string, string)>? forbiddenEdges,
            HashSet<string>? forbiddenNodes)
        {
            var dist = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var prev = new Dictionary<string, (string? Node, GraphEdge? Edge)>(StringComparer.OrdinalIgnoreCase);
            var pq   = new SortedList<double, Queue<string>>();

            foreach (var n in _nodes.Keys) dist[n] = double.MaxValue;
            dist[src] = 0;

            EnqueuePq(pq, 0, src);

            while (pq.Count > 0)
            {
                var (d, u) = DequeuePq(pq);

                if (d > dist[u]) continue;
                if (u == dst)    break;

                if (!_adj.TryGetValue(u, out var neighbors)) continue;

                foreach (var (v, edge) in neighbors)
                {
                    if (forbiddenNodes != null && forbiddenNodes.Contains(v)) continue;
                    if (forbiddenEdges != null && forbiddenEdges.Contains((u, v))) continue;
                    if (!_nodes.ContainsKey(v)) continue;

                    var nd = dist[u] + edge.DistanceKm;
                    if (nd < dist[v])
                    {
                        dist[v] = nd;
                        prev[v] = (u, edge);
                        EnqueuePq(pq, nd, v);
                    }
                }
            }

            if (!prev.ContainsKey(dst) && src != dst) return null;

            // reconstruct path
            var nodes = new List<string>();
            var edges = new List<GraphEdge>();
            var cur   = dst;

            while (cur != src)
            {
                nodes.Insert(0, cur);
                if (!prev.TryGetValue(cur, out var p) || p.Node == null) return null;
                edges.Insert(0, p.Edge!);
                cur = p.Node;
            }
            nodes.Insert(0, src);

            return new RoutePath
            {
                Nodes           = nodes,
                Edges           = edges,
                TotalDistanceKm = dist[dst]
            };
        }

        // ─────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────
        private static void EnqueuePq(SortedList<double, Queue<string>> pq, double key, string val)
        {
            while (pq.ContainsKey(key)) key += 1e-12;
            pq[key] = new Queue<string>();
            pq[key].Enqueue(val);
        }

        private static (double d, string v) DequeuePq(SortedList<double, Queue<string>> pq)
        {
            var key   = pq.Keys[0];
            var queue = pq[key];
            var val   = queue.Dequeue();
            if (!queue.Any()) pq.RemoveAt(0);
            return (key, val);
        }

        private double PathDistanceTo(RoutePath path, int nodeIndex)
            => path.Edges.Take(nodeIndex).Sum(e => e.DistanceKm);

        private List<GraphEdge> BuildEdgesForPath(List<string> nodes)
        {
            var result = new List<GraphEdge>();
            for (int i = 0; i < nodes.Count - 1; i++)
            {
                var u = nodes[i];
                var v = nodes[i + 1];
                var edge = _adj.TryGetValue(u, out var neighbors)
                    ? neighbors.FirstOrDefault(x => x.Neighbor == v).Edge
                    : null;

                result.Add(edge ?? new GraphEdge { Source = u, Target = v, DistanceKm = 0 });
            }
            return result;
        }

        private static string Normalize(string s)
        {
            return s.Trim().ToLowerInvariant();
        }

        public bool NodeExists(string label)
        {
            return ResolveNodeId(label) != null;
        }

    }
}
