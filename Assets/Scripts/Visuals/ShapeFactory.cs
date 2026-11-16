using UnityEngine;

namespace BugDefense
{
    /// <summary>Helpers for building visuals out of Unity primitives.</summary>
    public static class ShapeFactory
    {
        /// <summary>
        /// Creates a primitive under <paramref name="parent"/>. Its collider is removed unless
        /// <paramref name="keepCollider"/> is true (we add our own simple colliders instead).
        /// </summary>
        public static GameObject Prim(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 localScale,
                                      Material mat, bool keepCollider = false, string name = null)
        {
            var go = GameObject.CreatePrimitive(type);
            if (name != null) go.name = name;
            if (!keepCollider)
            {
                var col = go.GetComponent<Collider>();
                if (col != null) Object.DestroyImmediate(col);
            }
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var r = go.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;
            return go;
        }

        public static Transform Empty(string name, Transform parent, Vector3 localPos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            return t;
        }
    }
}
