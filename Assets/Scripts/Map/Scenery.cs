using UnityEngine;

namespace BugDefense
{
    /// <summary>
    /// [Req A] Decorative Kenney crystals and rocks around the edge of the board (never on buildable tiles).
    /// Purely cosmetic: if the models are missing, nothing is placed.
    /// </summary>
    public static class Scenery
    {
        public static void Create(Transform parent)
        {
            var root = new GameObject("Scenery").transform;
            root.SetParent(parent, false);

            float w = GridMap.Cols * GridMap.TileSize, d = GridMap.Rows * GridMap.TileSize;
            // (model, x, z, scale, yaw) -- just outside the left, right and back edges (the front edge is under the HUD).
            var items = new (string, float, float, float, float)[]
            {
                ("detail-crystal-large", w + 1.4f, 3f, 2.2f, 20f),
                ("detail-crystal", w + 1.1f, 6.5f, 2.6f, 70f),
                ("detail-rocks-large", w + 1.3f, 12f, 2f, 140f),
                ("detail-crystal-large", -1.5f, d - 2.5f, 2f, 200f),
                ("detail-rocks", -1.3f, d - 7f, 2.4f, 30f),
                ("detail-crystal", -1.2f, 10f, 2.4f, 300f),
                ("detail-crystal-large", 9f, d + 1.4f, 2.4f, 95f),
                ("detail-rocks-large", 20f, d + 1.3f, 1.8f, 10f),
                ("detail-crystal", 27f, d + 1.2f, 2.6f, 250f),
            };
            foreach (var (model, x, z, s, yaw) in items)
            {
                var go = ModelLibrary.Spawn(model, root, new Vector3(x, 0f, z), Vector3.one * s);
                if (go != null) go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            }
        }
    }
}
