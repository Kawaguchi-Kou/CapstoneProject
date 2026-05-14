using System.Collections.Generic;

namespace Application.Interfaces
{
    /// <summary>
    /// Reads the static Vietnam travel graph and finds K-shortest paths
    /// between two nodes matched by their Vietnamese label.
    /// </summary>
    public interface IRouteGraphService
    {
        /// <summary>
        /// Find the top-<paramref name="k"/> shortest paths from
        /// <paramref name="startLabel"/> to <paramref name="endLabel"/>.
        /// Labels are the Vietnamese names stored in the graph's "label" field
        /// (e.g. "Hà Nội", "TP. Hồ Chí Minh").
        /// </summary>
        List<RoutePath> FindTopKPaths(string startLabel, string endLabel, int k = 5);

        /// <summary>All nodes in the graph, keyed by their node id.</summary>
        IReadOnlyDictionary<string, GraphNode> Nodes { get; }

        bool NodeExists(string label);
    }

    public class GraphNode
    {
        public string Id    { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Type  { get; set; } = string.Empty;
    }

    public class GraphEdge
    {
        public string Source     { get; set; } = string.Empty;
        public string Target     { get; set; } = string.Empty;
        public double DistanceKm { get; set; }
        public string RouteType  { get; set; } = string.Empty;
    }

    public class RoutePath
    {
        /// <summary>Ordered node IDs along the path (start … end).</summary>
        public List<string> Nodes { get; set; } = new();

        /// <summary>One edge per hop between consecutive nodes.</summary>
        public List<GraphEdge> Edges { get; set; } = new();

        public double TotalDistanceKm { get; set; }
    }
}
