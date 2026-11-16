using UnityEngine;

namespace BugDefense
{
    /// <summary>The "git push" pad where bugs enter the codebase.</summary>
    public class SpawnPortal : MonoBehaviour
    {
        Transform ring;

        public static SpawnPortal Create(Transform parent)
        {
            var go = new GameObject("SpawnPortal");
            go.transform.SetParent(parent, false);
            go.transform.position = GridMap.GridToWorld(PathDefinition.SpawnCell);

            var orange = MaterialFactory.Hex("#F97316");
            ShapeFactory.Prim(PrimitiveType.Cylinder, go.transform, new Vector3(0f, 0.05f, 0f),
                new Vector3(1.8f, 0.1f, 1.8f), MaterialFactory.Lit(new Color(0.12f, 0.12f, 0.16f)), false, "Pad");
            var ring = ShapeFactory.Prim(PrimitiveType.Cylinder, go.transform, new Vector3(0f, 0.12f, 0f),
                new Vector3(1.3f, 0.04f, 1.3f), MaterialFactory.Lit(orange, 1.5f), false, "Glow");

            WorldLabel.Create("git push", go.transform, new Vector3(0f, 1.6f, 0f), orange, 0.09f);

            var portal = go.AddComponent<SpawnPortal>();
            portal.ring = ring.transform;
            return portal;
        }

        void Update()
        {
            if (ring == null) return;
            float s = 1.2f + Mathf.Sin(Time.time * 4f) * 0.12f;
            ring.localScale = new Vector3(s, 0.04f, s);
        }
    }
}
