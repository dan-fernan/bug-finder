using System.Collections.Generic;
using UnityEngine;

namespace BugDefense
{
    /// <summary>
    /// Small glowing "data packets" that stream along every edge of the path graph toward PROD.
    /// Purely visual: shows the direction of travel and makes forks/merges easy to read.
    /// </summary>
    public class PathFlow : MonoBehaviour
    {
        const float Speed = 2.5f;     // world units per second
        const float Spacing = 3f;     // distance between packets on an edge

        struct Packet { public Transform t; public Vector3 a, b; public float len, offset; }
        readonly List<Packet> packets = new List<Packet>();
        float clock;

        public void Init(LevelDefinition level, Material mat)
        {
            var root = ShapeFactory.Empty("Packets", transform, Vector3.zero);
            foreach (var (ca, cb) in level.EdgeCells)
            {
                Vector3 a = GridMap.GridToWorld(ca, 0.08f), b = GridMap.GridToWorld(cb, 0.08f);
                float len = Vector3.Distance(a, b);
                int n = Mathf.Max(1, Mathf.FloorToInt(len / Spacing));
                for (int i = 0; i < n; i++)
                {
                    var go = ShapeFactory.Prim(PrimitiveType.Cube, root, a, new Vector3(0.22f, 0.08f, 0.22f), mat, false, "Packet");
                    go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    packets.Add(new Packet { t = go.transform, a = a, b = b, len = len, offset = i * len / n });
                }
            }
        }

        void Update()
        {
            // Unscaled so the board still feels "alive" in the menu and on the end screens.
            clock += Time.unscaledDeltaTime * Speed;
            foreach (var p in packets)
            {
                float d = Mathf.Repeat(clock + p.offset, p.len);
                p.t.position = Vector3.Lerp(p.a, p.b, d / p.len);
            }
        }
    }
}
