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
            // [Req A] Prefer the Kenney models; fall back to primitives if they aren't in the project.
            if (TryBuildKenney(root, t, overrideMat, withCollider, out var kenney)) return kenney;
            return BuildPrimitive(root, t, overrideMat, withCollider);
        }

        /// <summary>Model scale: Kenney pieces are 1 unit wide, tiles are 2, so ~1.6 leaves a small margin.</summary>
        const float ModelScale = 1.6f;

        /// <summary>
        /// Stacks the type's Kenney body pieces and puts its weapon on top. The stack is stretched/squashed
        /// vertically so the weapon's barrel lands exactly at <see cref="TowerType.height"/> ([Req B] height stat).
        /// </summary>
        static bool TryBuildKenney(Transform root, TowerType t, Material overrideMat, bool withCollider, out Parts parts)
        {
            parts = new Parts();
            if (t.modelStack == null || t.modelStack.Length == 0 || !ModelLibrary.Has(t.modelWeapon)) return false;
            foreach (var n in t.modelStack) if (!ModelLibrary.Has(n)) return false;

            Color bodyTint = new Color(0.62f, 0.68f, 0.8f);                   // cool grey to suit the dark board
            Color weaponTint = Color.Lerp(Color.white, t.color, 0.65f);       // per-type color on the weapon

            parts.head = ShapeFactory.Empty("Head", root, Vector3.zero);
            var weapon = ModelLibrary.Spawn(t.modelWeapon, parts.head, Vector3.zero, Vector3.one * ModelScale, overrideMat, weaponTint);
            // The weapon's child mesh (barrel / arrow / arm) is the part that pitches.
            Transform gun = weapon.transform.childCount > 0 ? weapon.transform.GetChild(0) : weapon.transform;
            Bounds gunBounds = ModelLibrary.LocalBounds(gun.gameObject, parts.head);
            Vector3 pivot = parts.head.InverseTransformPoint(gun.position);

            // Body: stack pieces in model units, then scale Y so the stack top + barrel height = t.height.
            var stack = ShapeFactory.Empty("Body", root, Vector3.zero);
            float y = 0f;
            foreach (var n in t.modelStack)
            {
                var piece = ModelLibrary.Spawn(n, stack, new Vector3(0f, y, 0f), Vector3.one, overrideMat, bodyTint);
                y = ModelLibrary.LocalBounds(piece, stack).max.y; // next piece sits on top of this one
            }
            float stackTop = Mathf.Max(0.2f, t.height - gunBounds.center.y);
            float yScale = stackTop / (y * ModelScale);
            stack.localScale = new Vector3(ModelScale, ModelScale * yScale, ModelScale);

            parts.head.localPosition = new Vector3(0f, stackTop, 0f);

            // Glowing ring under the weapon: color-codes the type (and reads well against the dark board).
            var glow = overrideMat ?? MaterialFactory.Lit(t.color, 2f);
            ShapeFactory.Prim(PrimitiveType.Cylinder, root, new Vector3(0f, stackTop + 0.01f, 0f),
                new Vector3(1.05f, 0.03f, 1.05f), glow, false, "Band");

            // Our own pitch pivot at the gun's hinge; reparent the gun under it so it tilts with aiming.
            parts.barrel = ShapeFactory.Empty("Barrel", parts.head, pivot);
            gun.SetParent(parts.barrel, true);
            parts.firePoint = ShapeFactory.Empty("FirePoint", parts.barrel,
                new Vector3(0f, gunBounds.center.y - pivot.y, Mathf.Max(gunBounds.max.z, 0.3f) + 0.15f));

            if (withCollider)
            {
                var col = root.gameObject.AddComponent<CapsuleCollider>();
                col.center = new Vector3(0f, (stackTop + 0.4f) * 0.5f, 0f);
                col.height = stackTop + 0.4f;
                col.radius = 0.7f;
            }
            return true;
        }

        static Parts BuildPrimitive(Transform root, TowerType t, Material overrideMat, bool withCollider)
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
