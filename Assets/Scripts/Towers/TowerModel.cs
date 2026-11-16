using UnityEngine;

namespace BugDefense
{
    /// <summary>
    /// Builds a tower's visual hierarchy. Shared by real towers and the placement ghost.
    /// head (yaw) -> barrel (pitch) -> firePoint are our own empties so aiming never depends on mesh pivots.
    /// </summary>
    public static class TowerModel
    {
        public struct Parts
        {
            public Transform head, barrel, firePoint;
        }

        /// <param name="overrideMat">If set, every renderer uses this material (ghost preview).</param>
        /// <param name="withCollider">Real towers keep one collider on the column for hover/raycasts.</param>
        public static Parts Build(Transform root, TowerType t, Material overrideMat = null, bool withCollider = true)
        {
            var baseMat = overrideMat ?? MaterialFactory.Lit(new Color(0.16f, 0.19f, 0.25f), 0f, 0.4f);
            var tint = overrideMat ?? MaterialFactory.Lit(t.color, 0.25f, 0.5f);
            var glow = overrideMat ?? MaterialFactory.Lit(t.color, 2f);

            // [Req B] Column height follows the type's height so the difference is obvious on screen.
            float columnH = Mathf.Max(0.25f, t.height - t.headSize * 0.5f);
            // Plinth
            ShapeFactory.Prim(PrimitiveType.Cube, root, new Vector3(0f, 0.08f, 0f),
                new Vector3(1.5f, 0.16f, 1.5f), baseMat, false, "Plinth");
            // Unity cylinders are 2 units tall at scale 1, so scale.y = height / 2.
            ShapeFactory.Prim(PrimitiveType.Cylinder, root, new Vector3(0f, columnH * 0.5f, 0f),
                new Vector3(t.columnWidth, columnH * 0.5f, t.columnWidth), baseMat, withCollider, "Column");
            // Glowing bands every ~1.2 units: makes height easy to read at a glance.
            for (float y = 0.6f; y < columnH; y += 1.2f)
                ShapeFactory.Prim(PrimitiveType.Cylinder, root, new Vector3(0f, y, 0f),
                    new Vector3(t.columnWidth + 0.08f, 0.04f, t.columnWidth + 0.08f), glow, false, "Band");

            var parts = new Parts();
            parts.head = ShapeFactory.Empty("Head", root, new Vector3(0f, t.height, 0f));
            ShapeFactory.Prim(PrimitiveType.Cube, parts.head, Vector3.zero,
                new Vector3(t.headSize, t.headSize * 0.65f, t.headSize), tint, false, "HeadMesh");

            parts.barrel = ShapeFactory.Empty("Barrel", parts.head, Vector3.zero);
            float half = t.barrelLength * 0.5f;
            var b = ShapeFactory.Prim(PrimitiveType.Cylinder, parts.barrel, new Vector3(0f, 0f, t.headSize * 0.4f + half),
                new Vector3(t.barrelWidth, half, t.barrelWidth), baseMat, false, "BarrelMesh");
            b.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            float tip = t.headSize * 0.4f + t.barrelLength;
            var muzzle = ShapeFactory.Prim(PrimitiveType.Cylinder, parts.barrel, new Vector3(0f, 0f, tip),
                new Vector3(t.barrelWidth + 0.06f, 0.03f, t.barrelWidth + 0.06f), glow, false, "Muzzle");
            muzzle.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            parts.firePoint = ShapeFactory.Empty("FirePoint", parts.barrel, new Vector3(0f, 0f, tip + 0.1f));
            return parts;
        }
    }
}
