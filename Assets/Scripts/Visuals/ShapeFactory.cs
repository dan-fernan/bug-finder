using System.Collections.Generic;
using UnityEngine;

namespace BugDefense
{
    /// <summary>Helpers for building visuals out of Unity primitive meshes.</summary>
    public static class ShapeFactory
    {
        static readonly Dictionary<PrimitiveType, Mesh> meshes = new Dictionary<PrimitiveType, Mesh>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { meshes.Clear(); }

        /// <summary>The built-in mesh for a primitive type, fetched once and cached.</summary>
        public static Mesh PrimitiveMesh(PrimitiveType type)
        {
            if (meshes.TryGetValue(type, out var m) && m != null) return m;
            var tmp = GameObject.CreatePrimitive(type);
            m = tmp.GetComponent<MeshFilter>().sharedMesh;
            tmp.SetActive(false);
            Object.Destroy(tmp); // deferred destroy is safe anywhere, even inside physics callbacks
            meshes[type] = m;
            return m;
        }

        /// <summary>
        /// Creates a primitive-shaped object under <paramref name="parent"/>. No collider is created unless
        /// <paramref name="keepCollider"/> is true (avoids DestroyImmediate, which is illegal in physics callbacks).
        /// </summary>
        public static GameObject Prim(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 localScale,
                                      Material mat, bool keepCollider = false, string name = null)
        {
            var go = new GameObject(name ?? type.ToString());
            go.AddComponent<MeshFilter>().sharedMesh = PrimitiveMesh(type);
            var r = go.AddComponent<MeshRenderer>();
            if (mat != null) r.sharedMaterial = mat;

            if (keepCollider)
            {
                switch (type)
                {
                    case PrimitiveType.Sphere: go.AddComponent<SphereCollider>(); break;
                    case PrimitiveType.Capsule: go.AddComponent<CapsuleCollider>(); break;
                    case PrimitiveType.Cylinder: // Unity's own cylinder primitive also uses a capsule collider
                        go.AddComponent<CapsuleCollider>(); break;
                    case PrimitiveType.Cube: go.AddComponent<BoxCollider>(); break;
                    default: go.AddComponent<MeshCollider>().sharedMesh = PrimitiveMesh(type); break;
                }
            }

            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
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
