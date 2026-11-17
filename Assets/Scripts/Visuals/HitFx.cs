using System.Collections.Generic;
using UnityEngine;

namespace BugDefense
{
    /// <summary>
    /// [A+] Cheap code-built effects: a burst of small glowing cubes that fly out and shrink,
    /// and an expanding ring for splash damage. No colliders, so they are safe to spawn inside physics callbacks.
    /// </summary>
    public class HitFx : MonoBehaviour
    {
        const float BurstLife = 0.45f;
        const float RingLife = 0.35f;
        const int RingSegments = 48;

        readonly List<Transform> bits = new List<Transform>();
        readonly List<Vector3> vels = new List<Vector3>();
        float age, life, bitSize, ringRadius;
        LineRenderer ring;

        static Transform Parent => GameManager.Instance != null ? GameManager.Instance.transform : null;

        /// <summary>A spray of <paramref name="count"/> small cubes in <paramref name="color"/>.</summary>
        public static void Burst(Vector3 pos, Color color, int count = 6, float size = 0.12f, float speed = 4f)
        {
            var go = new GameObject("HitFx");
            go.transform.SetParent(Parent, false);
            go.transform.position = pos;
            var fx = go.AddComponent<HitFx>();
            fx.life = BurstLife;
            fx.bitSize = size;
            var mat = MaterialFactory.Lit(color, 2.5f);
            for (int i = 0; i < count; i++)
            {
                var b = ShapeFactory.Prim(PrimitiveType.Cube, go.transform, Vector3.zero, Vector3.one * size, mat, false, "Bit");
                b.transform.localRotation = Random.rotation;
                var r = b.GetComponent<MeshRenderer>();
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                Vector3 v = Random.insideUnitSphere;
                v.y = Mathf.Abs(v.y) + 0.4f; // mostly upward
                fx.bits.Add(b.transform);
                fx.vels.Add(v.normalized * speed * Random.Range(0.6f, 1.2f));
            }
        }

        /// <summary>A flat ring on the ground that expands to <paramref name="radius"/> (splash damage area).</summary>
        public static void Ring(Vector3 pos, float radius, Color color)
        {
            var go = new GameObject("SplashFx", typeof(LineRenderer));
            go.transform.SetParent(Parent, false);
            go.transform.position = new Vector3(pos.x, 0.1f, pos.z);
            var fx = go.AddComponent<HitFx>();
            fx.life = RingLife;
            fx.ringRadius = radius;
            fx.ring = go.GetComponent<LineRenderer>();
            fx.ring.useWorldSpace = false;
            fx.ring.loop = true;
            fx.ring.positionCount = RingSegments;
            fx.ring.sharedMaterial = MaterialFactory.Unlit(color);
            fx.ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            fx.SetRing(0.2f, 0.25f);
        }

        void SetRing(float radius, float width)
        {
            ring.widthMultiplier = width;
            for (int i = 0; i < RingSegments; i++)
            {
                float a = i * Mathf.PI * 2f / RingSegments;
                ring.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }
        }

        void Update()
        {
            age += Time.deltaTime;
            float k = Mathf.Clamp01(age / life);
            if (ring != null)
            {
                SetRing(Mathf.Lerp(0.2f, ringRadius, 1f - (1f - k) * (1f - k)), Mathf.Lerp(0.25f, 0.02f, k));
            }
            else
            {
                float dt = Time.deltaTime;
                for (int i = 0; i < bits.Count; i++)
                {
                    vels[i] += Vector3.down * 12f * dt;
                    bits[i].localPosition += vels[i] * dt;
                    bits[i].localScale = Vector3.one * bitSize * (1f - k);
                }
            }
            if (age >= life) Destroy(gameObject);
        }
    }
}
