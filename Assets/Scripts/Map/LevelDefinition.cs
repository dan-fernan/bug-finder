using System.Collections.Generic;
using UnityEngine;

namespace BugDefense
{
    /// <summary>
    /// One spawn-to-PROD walk through a level's path graph. Bugs follow a single route; routes share
    /// segments, so on the board the path visibly forks and merges.
    /// </summary>
    public class PathRoute
    {
        /// <summary>World-space corners at ground level: spawn, every turn/fork node, then PROD.</summary>
        public Vector3[] points;
        /// <summary>remainingAfter[i] = path length from points[i] to PROD.</summary>
        public float[] remainingAfter;
        public int spawnIndex;

        public PathRoute(List<Vector2Int> cells, int spawnIndex)
        {
            this.spawnIndex = spawnIndex;
            points = new Vector3[cells.Count];
            for (int i = 0; i < cells.Count; i++) points[i] = GridMap.GridToWorld(cells[i]);
            remainingAfter = new float[points.Length];
            for (int i = points.Length - 2; i >= 0; i--)
                remainingAfter[i] = remainingAfter[i + 1] + Vector3.Distance(points[i], points[i + 1]);
        }

        public float Length => remainingAfter.Length > 0 ? remainingAfter[0] : 0f;
    }

    /// <summary>
    /// [Req B] Paths are not straight lines: each level is a directed graph of grid nodes. Edges must be
    /// axis-aligned. Every spawn-to-PROD walk through the graph is a route; bugs pick one at random,
    /// so the path forks ("if / else") and merges back together on the way to PROD.
    /// </summary>
    public class LevelDefinition
    {
        public string name;
        public string tagline;
        public float hpScale = 1f;          // extra difficulty on top of the per-wave ramp
        public int startingCapacity = 8;

        readonly Dictionary<string, Vector2Int> nodes = new Dictionary<string, Vector2Int>();
        readonly List<(string from, string to)> edges = new List<(string, string)>();
        readonly List<string> spawns = new List<string>();
        string prod;
        List<PathRoute> routes;

        public LevelDefinition(string name, string tagline) { this.name = name; this.tagline = tagline; }

        public LevelDefinition Node(string id, int x, int z) { nodes[id] = new Vector2Int(x, z); return this; }
        public LevelDefinition Spawn(string id, int x, int z) { Node(id, x, z); spawns.Add(id); return this; }
        public LevelDefinition Prod(string id, int x, int z) { Node(id, x, z); prod = id; return this; }

        /// <summary>Adds a chain of edges: Edge("a","b","c") = a->b, b->c.</summary>
        public LevelDefinition Edge(params string[] chain)
        {
            for (int i = 0; i < chain.Length - 1; i++) edges.Add((chain[i], chain[i + 1]));
            return this;
        }

        public IReadOnlyList<string> SpawnIds => spawns;
        public Vector2Int Cell(string id) => nodes[id];
        public Vector2Int ProdCell => nodes[prod];
        public IEnumerable<Vector2Int> SpawnCells { get { foreach (var s in spawns) yield return nodes[s]; } }

        /// <summary>Edges as cell pairs (for drawing the path and the data-flow packets).</summary>
        public IEnumerable<(Vector2Int a, Vector2Int b)> EdgeCells { get { foreach (var e in edges) yield return (nodes[e.from], nodes[e.to]); } }

        /// <summary>All spawn-to-PROD routes (depth-first enumeration of the DAG), cached.</summary>
        public List<PathRoute> Routes
        {
            get
            {
                if (routes != null) return routes;
                routes = new List<PathRoute>();
                for (int s = 0; s < spawns.Count; s++)
                    Walk(spawns[s], new List<Vector2Int>(), s, 0);
                return routes;
            }
        }

        void Walk(string id, List<Vector2Int> sofar, int spawnIndex, int depth)
        {
            if (depth > 64) { Debug.LogError($"Level {name}: path graph has a cycle"); return; }
            sofar.Add(nodes[id]);
            if (id == prod) routes.Add(new PathRoute(new List<Vector2Int>(sofar), spawnIndex));
            else
                foreach (var e in edges)
                    if (e.from == id) Walk(e.to, sofar, spawnIndex, depth + 1);
            sofar.RemoveAt(sofar.Count - 1);
        }

        /// <summary>Every grid cell covered by an edge (may include off-board cells such as the spawn pad).</summary>
        public HashSet<Vector2Int> PathCells()
        {
            var cells = new HashSet<Vector2Int>();
            foreach (var (a, b) in EdgeCells)
            {
                if (a.x != b.x && a.y != b.y) Debug.LogError($"Level {name}: edge {a}->{b} is not axis-aligned");
                var step = new Vector2Int(System.Math.Sign(b.x - a.x), System.Math.Sign(b.y - a.y));
                var p = a;
                cells.Add(p);
                while (p != b) { p += step; cells.Add(p); }
            }
            return cells;
        }

        /// <summary>Nodes where the path splits (more than one outgoing edge).</summary>
        public IEnumerable<Vector2Int> ForkCells()
        {
            var count = new Dictionary<string, int>();
            foreach (var e in edges) count[e.from] = count.TryGetValue(e.from, out var n) ? n + 1 : 1;
            foreach (var kv in count) if (kv.Value > 1) yield return nodes[kv.Key];
        }
    }
}
