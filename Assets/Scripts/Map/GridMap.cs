using System.Collections.Generic;
using UnityEngine;

namespace BugDefense
{
    /// <summary>
    /// Builds the 18x12 tile board for the current level. Path tiles are a dark green "bus" with a glowing
    /// circuit trace down the middle (forks and merges included); buildable tiles are dark slate.
    /// </summary>
    public class GridMap : MonoBehaviour
    {
        public const int Cols = 18, Rows = 12;
        public const float TileSize = 2f;

        public static readonly Color SlateColor = new Color32(0x1A, 0x22, 0x33, 0xFF);
        public static readonly Color PathColor = new Color32(0x1F, 0xD1, 0x7A, 0xFF);
        public static readonly Color PathTileColor = new Color32(0x0E, 0x3B, 0x2C, 0xFF);

        BuildTile[,] tiles;

        public static Vector3 GridToWorld(Vector2Int c, float y = 0f) => GridToWorld(c.x, c.y, y);
        public static Vector3 GridToWorld(int x, int z, float y = 0f) =>
            new Vector3(x * TileSize + TileSize * 0.5f, y, z * TileSize + TileSize * 0.5f);

        public BuildTile GetTile(int x, int z) =>
            (tiles != null && x >= 0 && z >= 0 && x < Cols && z < Rows) ? tiles[x, z] : null;

        public void Build()
        {
            tiles = new BuildTile[Cols, Rows];
            var pathCells = PathDefinition.PathCells();
            var slate = MaterialFactory.Lit(SlateColor, 0f, 0.15f);
            var slateEdge = MaterialFactory.Lit(new Color32(0x2A, 0x36, 0x4F, 0xFF), 0f, 0.1f);
            var path = MaterialFactory.Lit(PathTileColor, 0.35f, 0.6f);
            var trace = MaterialFactory.Lit(PathColor, 2.2f);

            // Underlay slightly larger than the board: the thin gaps between tiles read as a grid border.
            ShapeFactory.Prim(PrimitiveType.Cube, transform,
                new Vector3(Cols * TileSize * 0.5f, -0.25f, Rows * TileSize * 0.5f),
                new Vector3(Cols * TileSize + 0.4f, 0.2f, Rows * TileSize + 0.4f), slateEdge, false, "BoardUnderlay");

            for (int x = 0; x < Cols; x++)
            for (int z = 0; z < Rows; z++)
            {
                bool isPath = pathCells.Contains(new Vector2Int(x, z));
                var go = new GameObject($"Tile_{x}_{z}");
                go.transform.SetParent(transform, false);
                go.transform.position = GridToWorld(x, z);

                // Full-size collider for mouse raycasts; the visual is slightly inset.
                var col = go.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, -0.1f, 0f);
                col.size = new Vector3(TileSize, 0.2f, TileSize);

                var mat = isPath ? path : slate;
                var vis = ShapeFactory.Prim(PrimitiveType.Cube, go.transform, new Vector3(0f, -0.1f, 0f),
                    new Vector3(TileSize - 0.12f, 0.2f, TileSize - 0.12f), mat, false, "Visual");

                var tile = go.AddComponent<BuildTile>();
                tile.x = x; tile.z = z; tile.isPath = isPath;
                tile.visual = vis.GetComponent<Renderer>();
                tile.baseMaterial = mat;
                tiles[x, z] = tile;
            }

            // Circuit traces along every edge, pads on every node, and stubs to the off-board spawn pads / PROD.
            var level = PathDefinition.Level;
            foreach (var (a, b) in level.EdgeCells)
            {
                Vector3 wa = GridToWorld(a), wb = GridToWorld(b);
                if (!OnBoard(a) || !OnBoard(b)) AddConnector(wa, wb, path);
                AddTrace(wa, wb, trace);
            }
            var nodeSet = new HashSet<Vector2Int>();
            foreach (var (a, b) in level.EdgeCells) { nodeSet.Add(a); nodeSet.Add(b); }
            foreach (var n in nodeSet)
                ShapeFactory.Prim(PrimitiveType.Cube, transform, GridToWorld(n, 0.03f), new Vector3(0.55f, 0.06f, 0.55f), trace, false, "TracePad");

            // Forks get a small "if" marker so the branching reads as code.
            foreach (var f in level.ForkCells())
                if (OnBoard(f))
                {
                    var lbl = WorldLabel.Create("if", transform, GridToWorld(f, 1.1f), new Color(0.75f, 1f, 0.85f), 0.11f);
                    lbl.name = "ForkLabel";
                }

            gameObject.AddComponent<PathFlow>().Init(level, trace);
        }

        static bool OnBoard(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < Cols && c.y < Rows;

        void AddTrace(Vector3 a, Vector3 b, Material mat)
        {
            var mid = (a + b) * 0.5f + Vector3.up * 0.02f;
            var d = b - a;
            var size = Mathf.Abs(d.x) > Mathf.Abs(d.z) ? new Vector3(Mathf.Abs(d.x), 0.04f, 0.16f) : new Vector3(0.16f, 0.04f, Mathf.Abs(d.z));
            ShapeFactory.Prim(PrimitiveType.Cube, transform, mid, size, mat, false, "Trace");
        }

        void AddConnector(Vector3 a, Vector3 b, Material mat)
        {
            var mid = (a + b) * 0.5f + Vector3.down * 0.1f;
            var d = b - a;
            var size = Mathf.Abs(d.x) > Mathf.Abs(d.z)
                ? new Vector3(Mathf.Abs(d.x), 0.2f, TileSize - 0.12f)
                : new Vector3(TileSize - 0.12f, 0.2f, Mathf.Abs(d.z));
            ShapeFactory.Prim(PrimitiveType.Cube, transform, mid, size, mat, false, "PathConnector");
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            foreach (var r in PathDefinition.Routes)
                for (int i = 0; i < r.points.Length - 1; i++)
                    Gizmos.DrawLine(r.points[i] + Vector3.up * 0.5f, r.points[i + 1] + Vector3.up * 0.5f);
        }
    }
}
