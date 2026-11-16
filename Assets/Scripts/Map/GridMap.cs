using UnityEngine;

namespace BugDefense
{
    /// <summary>Builds the 18x12 tile board. Path tiles glow green, buildable tiles are dark slate.</summary>
    public class GridMap : MonoBehaviour
    {
        public const int Cols = 18, Rows = 12;
        public const float TileSize = 2f;

        public static readonly Color SlateColor = new Color32(0x1A, 0x22, 0x33, 0xFF);
        public static readonly Color PathColor = new Color32(0x1F, 0xD1, 0x7A, 0xFF);

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
            var path = MaterialFactory.Lit(PathColor, 0.6f, 0.5f);

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

            // Short glowing stubs connecting the spawn pad and PROD to the board.
            var wp = PathDefinition.WorldWaypoints;
            AddConnector(wp[0], wp[1], path);
            AddConnector(wp[wp.Length - 2], wp[wp.Length - 1], path);
        }

        void AddConnector(Vector3 a, Vector3 b, Material mat)
        {
            var mid = (a + b) * 0.5f + Vector3.down * 0.1f;
            float len = Vector3.Distance(a, b);
            ShapeFactory.Prim(PrimitiveType.Cube, transform, mid, new Vector3(len, 0.2f, TileSize - 0.12f), mat, false, "PathConnector");
        }

        void OnDrawGizmos()
        {
            var wp = PathDefinition.WorldWaypoints;
            Gizmos.color = Color.green;
            for (int i = 0; i < wp.Length - 1; i++)
                Gizmos.DrawLine(wp[i] + Vector3.up * 0.5f, wp[i + 1] + Vector3.up * 0.5f);
        }
    }
}
