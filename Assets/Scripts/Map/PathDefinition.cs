using System.Collections.Generic;
using UnityEngine;

namespace BugDefense
{
    /// <summary>
    /// [Req C] Predetermined enemy paths, read from the current level (<see cref="LevelCatalog"/>).
    /// [Req B] Not straight lines: each level's path zig-zags, forks and merges.
    /// </summary>
    public static class PathDefinition
    {
        public static LevelDefinition Level => LevelCatalog.Current;
        public static List<PathRoute> Routes => Level.Routes;
        public static Vector2Int ProdCell => Level.ProdCell;
        public static IEnumerable<Vector2Int> SpawnCells => Level.SpawnCells;
        public static HashSet<Vector2Int> PathCells() => Level.PathCells();

        /// <summary>A random route starting at the given spawn (bugs pick their branch when they enter).</summary>
        public static PathRoute RandomRoute(int spawnIndex)
        {
            var routes = Routes;
            int n = 0;
            foreach (var r in routes) if (r.spawnIndex == spawnIndex) n++;
            int pick = Random.Range(0, Mathf.Max(1, n));
            foreach (var r in routes)
                if (r.spawnIndex == spawnIndex && pick-- == 0) return r;
            return routes[0];
        }
    }
}
