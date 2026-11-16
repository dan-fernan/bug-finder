using UnityEngine;

namespace BugDefense
{
    /// <summary>Flat circle drawn with a LineRenderer (64 segments) showing a tower's range.</summary>
    [RequireComponent(typeof(LineRenderer))]
    public class RangeRing : MonoBehaviour
    {
        const int Segments = 64;
        LineRenderer lr;

        public static RangeRing Create(Transform parent)
        {
            var go = new GameObject("RangeRing", typeof(LineRenderer));
            go.transform.SetParent(parent, false);
            var ring = go.AddComponent<RangeRing>();
            ring.Init();
            return ring;
        }

        void Init()
        {
            lr = GetComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.loop = true;
            lr.positionCount = Segments;
            lr.widthMultiplier = 0.1f;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.enabled = false;
        }

        public void Show(Vector3 center, float radius, Color color)
        {
            if (lr == null) Init();
            lr.sharedMaterial = MaterialFactory.Unlit(color);
            lr.startColor = lr.endColor = color;
            for (int i = 0; i < Segments; i++)
            {
                float a = i * Mathf.PI * 2f / Segments;
                lr.SetPosition(i, new Vector3(center.x + Mathf.Cos(a) * radius, 0.06f, center.z + Mathf.Sin(a) * radius));
            }
            lr.enabled = true;
        }

        public void Hide()
        {
            if (lr != null) lr.enabled = false;
        }
    }
}
