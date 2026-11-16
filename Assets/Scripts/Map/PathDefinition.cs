using System.Collections.Generic;
using UnityEngine;

namespace BugDefense
{
    /// <summary>
    /// [Req C] Predetermined enemy path. [Req B] Not a straight line: zig-zag with 6 turns.
    /// Edit GridWaypoints to change the route; segments must be axis-aligned.
    /// </summary>
    public static class PathDefinition
    {
        public static readonly Vector2Int[] GridWaypoints =
        {
            new Vector2Int(0, 2), new Vector2Int(5, 2), new Vector2Int(5, 8), new Vector2Int(10, 8),
            new Vector2Int(10, 3), new Vector2Int(14, 3), new Vector2Int(14, 9), new Vector2Int(17, 9),
        };

        /// <summary>Spawn pad ("git push"), just off the grid.</summary>
        public static readonly Vector2Int SpawnCell = new Vector2Int(-1, 2);
        /// <summary>PROD server, just past the last waypoint.</summary>
        public static readonly Vector2Int ProdCell = new Vector2Int(18, 9);

        static Vector3[] worldWaypoints;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { worldWaypoints = null; }

        /// <summary>Full walk route at ground level: spawn, every grid waypoint, then PROD.</summary>
        public static Vector3[] WorldWaypoints
        {
            get
            {
                if (worldWaypoints == null)
                {
                    var list = new List<Vector3> { GridMap.GridToWorld(SpawnCell) };
                    foreach (var c in GridWaypoints) list.Add(GridMap.GridToWorld(c));
                    list.Add(GridMap.GridToWorld(ProdCell));
                    worldWaypoints = list.ToArray();
                }
                return worldWaypoints;
            }
        }

        /// <summary>Every grid cell covered by the path (between consecutive grid waypoints).</summary>
        public static HashSet<Vector2Int> PathCells()
        {
            var cells = new HashSet<Vector2Int>();
            for (int i = 0; i < GridWaypoints.Length - 1; i++)
            {
                Vector2Int a = GridWaypoints[i], b = GridWaypoints[i + 1];
                var step = new Vector2Int(System.Math.Sign(b.x - a.x), System.Math.Sign(b.y - a.y));
                var p = a;
                cells.Add(p);
                while (p != b) { p += step; cells.Add(p); }
            }
            return cells;
        }
    }
}
