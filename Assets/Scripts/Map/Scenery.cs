using UnityEngine;

namespace BugDefense
{
    /// <summary>
    /// The "datacenter" around the board: a dark floor that fades into fog, clusters of server racks with
    /// blinking status LEDs behind and beside the board, glowing circuit traces running from the racks to
    /// the board, and faint lines of code printed on the floor. All built from code (no assets needed).
    /// </summary>
    public static class Scenery
    {
        static readonly Color Floor = new Color32(0x07, 0x0A, 0x12, 0xFF);
        static readonly Color TraceBlue = new Color32(0x38, 0xBD, 0xF8, 0xFF);
        static readonly Color TraceGreen = new Color32(0x1F, 0xD1, 0x7A, 0xFF);

        static readonly string[] CodeLines =
        {
            "while (bugs.Count > 0) deploy(tools);",
            "// TODO: remove before release",
            "if (env == PROD) throw new PanicException();",
            "git commit -m \"fix: fix the fix\"",
            "catch (Exception) { /* it's fine */ }",
            "assert(works_on_my_machine);",
        };

        public static void Create(Transform parent)
        {
            var root = new GameObject("Scenery").transform;
            root.SetParent(parent, false);

            float w = GridMap.Cols * GridMap.TileSize, d = GridMap.Rows * GridMap.TileSize;

            // Floor under everything + fog so the datacenter fades into the dark background.
            ShapeFactory.Prim(PrimitiveType.Cube, root, new Vector3(w * 0.5f, -0.5f, d * 0.5f + 10f),
                new Vector3(220f, 0.2f, 160f), MaterialFactory.Unlit(Floor), false, "Floor"); // unlit: no sky reflections at grazing angles
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = GameManager.BackgroundColor;
            RenderSettings.fogStartDistance = 55f;
            RenderSettings.fogEndDistance = 120f;

            // Server clusters: a long bank behind the board, smaller ones on each side.
            Cluster(root, new Vector3(-2f, 0f, d + 6f), 7, 6.5f, 3.2f, 6f);
            Cluster(root, new Vector3(w * 0.5f + 3f, 0f, d + 9f), 6, 4.5f, 2.6f, 7f);
            Cluster(root, new Vector3(w - 4f, 0f, d + 5f), 5, 3.8f, 2.4f, 5f);
            Cluster(root, new Vector3(-9f, 0f, d * 0.5f + 2f), 4, 3.5f, 2.6f, 4f, sideways: true);
            Cluster(root, new Vector3(w + 8f, 0f, 2f), 4, 3.2f, 2.4f, 4.5f, sideways: true);

            // Circuit traces from the racks to the board edges (right-angle bends, like a PCB).
            var blue = MaterialFactory.Lit(TraceBlue * 0.8f, 1.2f);
            var green = MaterialFactory.Lit(TraceGreen * 0.8f, 1.2f);
            Trace(root, blue, new Vector3(4f, 0f, d + 4.5f), new Vector3(4f, 0f, d + 1.2f), new Vector3(9f, 0f, d + 1.2f), new Vector3(9f, 0f, d + 0.3f));
            Trace(root, green, new Vector3(16f, 0f, d + 7.5f), new Vector3(16f, 0f, d + 2.2f), new Vector3(20f, 0f, d + 2.2f), new Vector3(20f, 0f, d + 0.3f));
            Trace(root, blue, new Vector3(w - 2f, 0f, d + 3.6f), new Vector3(w - 2f, 0f, d + 1.6f), new Vector3(w - 9f, 0f, d + 1.6f), new Vector3(w - 9f, 0f, d + 0.3f));
            Trace(root, green, new Vector3(-6.5f, 0f, d * 0.5f + 2f), new Vector3(-3.5f, 0f, d * 0.5f + 2f), new Vector3(-3.5f, 0f, d - 3f), new Vector3(-0.3f, 0f, d - 3f));
            Trace(root, blue, new Vector3(w + 5.5f, 0f, 4f), new Vector3(w + 3f, 0f, 4f), new Vector3(w + 3f, 0f, 8f), new Vector3(w + 0.3f, 0f, 8f));

            // Faint code printed on the floor around the board.
            FloorText(root, CodeLines[0], new Vector3(w * 0.5f, 0f, d + 3.3f), 0f);
            FloorText(root, CodeLines[1], new Vector3(w * 0.25f, 0f, -2.6f), 0f);
            FloorText(root, CodeLines[2], new Vector3(w * 0.72f, 0f, -2.6f), 0f);
            FloorText(root, CodeLines[3], new Vector3(-3.4f, 0f, d * 0.3f), 90f);
            FloorText(root, CodeLines[4], new Vector3(w + 3.6f, 0f, d * 0.65f), -90f);
            FloorText(root, CodeLines[5], new Vector3(w * 0.25f, 0f, d + 3.3f), 0f);
        }

        /// <summary>A row of racks of varying height, each with LED rows and vent stripes.</summary>
        static void Cluster(Transform root, Vector3 start, int count, float maxH, float width, float depth, bool sideways = false)
        {
            var chassis = MaterialFactory.Lit(new Color(0.08f, 0.09f, 0.13f), 0f, 0.65f);
            var trim = MaterialFactory.Lit(new Color(0.16f, 0.19f, 0.27f), 0f, 0.4f);
            var vent = MaterialFactory.Unlit(new Color(0.03f, 0.035f, 0.05f));
            Material[] ledOn =
            {
                MaterialFactory.Unlit(new Color(0.2f, 1f, 0.45f)), MaterialFactory.Unlit(new Color(0.25f, 0.75f, 1f)),
                MaterialFactory.Unlit(new Color(1f, 0.75f, 0.2f)),
            };
            var ledOff = MaterialFactory.Unlit(new Color(0.05f, 0.12f, 0.1f));

            var cluster = ShapeFactory.Empty("ServerCluster", root, start);
            if (sideways) cluster.localRotation = Quaternion.Euler(0f, 90f, 0f);
            // Deterministic variation so the skyline looks the same every run.
            var rng = new System.Random(Mathf.RoundToInt(start.x * 31f + start.z * 17f));
            float x = 0f;
            for (int i = 0; i < count; i++)
            {
                float h = maxH * (0.6f + 0.4f * (float)rng.NextDouble());
                var rack = ShapeFactory.Empty("Rack", cluster, new Vector3(x + width * 0.5f, 0f, 0f));
                ShapeFactory.Prim(PrimitiveType.Cube, rack, new Vector3(0f, h * 0.5f, 0f), new Vector3(width, h, depth), chassis, false, "Chassis");
                ShapeFactory.Prim(PrimitiveType.Cube, rack, new Vector3(0f, h + 0.05f, 0f), new Vector3(width + 0.1f, 0.1f, depth + 0.1f), trim, false, "Top");

                // Front face (-z, toward the board/camera): one "server unit" every 0.6 units.
                float front = -depth * 0.5f - 0.02f;
                for (float y = 0.45f; y < h - 0.2f; y += 0.6f)
                {
                    ShapeFactory.Prim(PrimitiveType.Cube, rack, new Vector3(-width * 0.12f, y, front), new Vector3(width * 0.6f, 0.06f, 0.02f), vent, false, "Vent");
                    int leds = 2 + rng.Next(3);
                    for (int l = 0; l < leds; l++)
                    {
                        var on = ledOn[rng.Next(ledOn.Length)];
                        var led = ShapeFactory.Prim(PrimitiveType.Cube, rack,
                            new Vector3(width * 0.3f + l * 0.14f - 0.1f, y, front - 0.01f), new Vector3(0.08f, 0.08f, 0.03f), on, false, "LED");
                        led.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        var b = led.AddComponent<Blinker>();
                        b.onMat = on; b.offMat = ledOff;
                        b.minInterval = 0.2f; b.maxInterval = 2.5f;
                    }
                }
                x += width + 0.25f + (rng.Next(4) == 0 ? 1.2f : 0f); // occasional aisle gap
            }
        }

        /// <summary>A glowing polyline on the floor (axis-aligned segments) with a pad at each end.</summary>
        static void Trace(Transform root, Material mat, params Vector3[] pts)
        {
            for (int i = 0; i < pts.Length - 1; i++)
            {
                Vector3 a = pts[i], b = pts[i + 1], dlt = b - a;
                var size = Mathf.Abs(dlt.x) > Mathf.Abs(dlt.z)
                    ? new Vector3(Mathf.Abs(dlt.x) + 0.12f, 0.03f, 0.12f)
                    : new Vector3(0.12f, 0.03f, Mathf.Abs(dlt.z) + 0.12f);
                ShapeFactory.Prim(PrimitiveType.Cube, root, (a + b) * 0.5f + Vector3.down * 0.39f, size, mat, false, "FloorTrace");
            }
            ShapeFactory.Prim(PrimitiveType.Cube, root, pts[0] + Vector3.down * 0.38f, new Vector3(0.4f, 0.04f, 0.4f), mat, false, "TracePad");
        }

        /// <summary>Monospace code lying flat on the floor, dim so it reads as texture rather than UI.</summary>
        static void FloorText(Transform root, string text, Vector3 pos, float yaw)
        {
            var go = new GameObject("FloorCode");
            go.transform.SetParent(root, false);
            go.transform.position = pos + Vector3.down * 0.38f;
            go.transform.rotation = Quaternion.Euler(90f, yaw, 0f);
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.font = UIFactory.CodeFont;
            tm.fontSize = 64;
            tm.characterSize = 0.05f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = new Color(0.35f, 0.55f, 0.75f, 0.45f);
            var mr = go.GetComponent<MeshRenderer>();
            if (tm.font != null) mr.sharedMaterial = tm.font.material;
        }
    }
}
