using UnityEngine;

namespace BugDefense
{
    /// <summary>World-space HP bar (two unlit quads) that floats above an enemy and faces the camera.</summary>
    public class HealthBar : MonoBehaviour
    {
        const float Width = 1.1f, Height = 0.14f;

        Enemy enemy;
        Transform fill;
        Renderer fillRenderer;
        Material good, mid, low;

        public static HealthBar Create(Enemy enemy)
        {
            var go = new GameObject("HealthBar");
            go.transform.SetParent(enemy.transform, false);
            var hb = go.AddComponent<HealthBar>();
            hb.enemy = enemy;
            hb.Build();
            return hb;
        }

        void Build()
        {
            good = MaterialFactory.Unlit(new Color(0.2f, 0.9f, 0.45f));
            mid = MaterialFactory.Unlit(new Color(0.98f, 0.8f, 0.1f));
            low = MaterialFactory.Unlit(new Color(0.95f, 0.25f, 0.25f));

            var bg = ShapeFactory.Prim(PrimitiveType.Quad, transform, Vector3.zero,
                new Vector3(Width + 0.08f, Height + 0.08f, 1f), MaterialFactory.Unlit(new Color(0.05f, 0.05f, 0.08f)), false, "BarBg");
            bg.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var f = ShapeFactory.Prim(PrimitiveType.Quad, transform, new Vector3(0f, 0f, -0.01f),
                new Vector3(Width, Height, 1f), good, false, "BarFill");
            fill = f.transform;
            fillRenderer = f.GetComponent<Renderer>();
            fillRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void LateUpdate()
        {
            if (enemy == null) return;
            var parent = transform.parent;
            float s = parent != null ? parent.lossyScale.x : 1f;
            // Undo the enemy's scale so every bar is the same size; sit just above the body.
            transform.localScale = Vector3.one / Mathf.Max(0.01f, s);
            transform.position = enemy.transform.position + Vector3.up * (0.55f * s + 0.45f);
            var cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;

            float frac = enemy.maxHp > 0f ? Mathf.Clamp01(enemy.hp / enemy.maxHp) : 0f;
            fill.localScale = new Vector3(Width * frac, Height, 1f);
            fill.localPosition = new Vector3(-Width * (1f - frac) * 0.5f, 0f, -0.01f); // anchor left
            fillRenderer.sharedMaterial = frac > 0.6f ? good : frac > 0.3f ? mid : low;
        }
    }
}
