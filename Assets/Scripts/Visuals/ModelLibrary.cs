using System.Collections.Generic;
using UnityEngine;

namespace BugDefense
{
    /// <summary>
    /// [Req A] Non-basic assets: loads Kenney Tower Defense Kit models (CC0) from Resources/Models/Kenney.
    /// Every caller has a primitive fallback, so the game still runs if the models are missing.
    /// </summary>
    public static class ModelLibrary
    {
        const string Root = "Models/Kenney/";
        static readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
        static readonly Dictionary<string, Material> tinted = new Dictionary<string, Material>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { prefabs.Clear(); tinted.Clear(); }

        /// <summary>The model prefab, or null if it isn't in the project (result cached either way).</summary>
        public static GameObject Load(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (prefabs.TryGetValue(name, out var p)) return p;
            p = Resources.Load<GameObject>(Root + name);
            prefabs[name] = p;
            return p;
        }

        public static bool Has(string name) => Load(name) != null;

        /// <summary>
        /// Instantiates a model under <paramref name="parent"/>. Imported colliders are removed (we add our own simple ones).
        /// <paramref name="overrideMat"/> replaces every material (placement ghost); otherwise <paramref name="tint"/>
        /// multiplies the model's own colormap material. Returns null if the model is missing.
        /// </summary>
        public static GameObject Spawn(string name, Transform parent, Vector3 localPos, Vector3 localScale,
                                       Material overrideMat = null, Color? tint = null)
        {
            var prefab = Load(name);
            if (prefab == null) return null;
            var go = Object.Instantiate(prefab, parent, false);
            go.name = name;
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = localScale;

            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.Destroy(c);
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (overrideMat != null)
                {
                    var mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++) mats[i] = overrideMat;
                    r.sharedMaterials = mats;
                }
                else if (tint.HasValue)
                {
                    var mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++) mats[i] = Tinted(mats[i], tint.Value);
                    r.sharedMaterials = mats;
                }
            }
            return go;
        }

        /// <summary>A cached copy of <paramref name="src"/> with its base color multiplied by <paramref name="tint"/>.</summary>
        static Material Tinted(Material src, Color tint)
        {
            if (src == null) return null;
            string key = src.name + "_" + ColorUtility.ToHtmlStringRGBA(tint);
            if (tinted.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(src) { name = key };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", tint);
            if (m.HasProperty("_Color")) m.SetColor("_Color", tint);
            tinted[key] = m;
            return m;
        }

        /// <summary>Renderer bounds of <paramref name="go"/> expressed in <paramref name="space"/>'s local coordinates.</summary>
        public static Bounds LocalBounds(GameObject go, Transform space)
        {
            bool any = false;
            var b = new Bounds();
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue;
                var mb = mf.sharedMesh.bounds;
                // Transform the 8 corners of the mesh bounds into the target space.
                for (int i = 0; i < 8; i++)
                {
                    var corner = mb.center + Vector3.Scale(mb.extents,
                        new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var p = space.InverseTransformPoint(mf.transform.TransformPoint(corner));
                    if (!any) { b = new Bounds(p, Vector3.zero); any = true; }
                    else b.Encapsulate(p);
                }
            }
            return b;
        }
    }
}
