using System.Collections.Generic;
using UnityEngine;

namespace BugDefense
{
    /// <summary>The PROD server rack at the end of the path. Flashes red when a bug reaches it.</summary>
    public class ProdServer : MonoBehaviour
    {
        readonly List<Renderer> chassis = new List<Renderer>();
        readonly List<Blinker> leds = new List<Blinker>();
        Material chassisMat, alarmMat, alarmDim;
        bool alarm;

        public static ProdServer Create(Transform parent)
        {
            var go = new GameObject("ProdServer");
            go.transform.SetParent(parent, false);
            go.transform.position = GridMap.GridToWorld(PathDefinition.ProdCell);
            var prod = go.AddComponent<ProdServer>();
            prod.BuildRack();
            return prod;
        }

        void BuildRack()
        {
            chassisMat = MaterialFactory.Lit(new Color(0.09f, 0.1f, 0.13f), 0f, 0.6f);
            alarmMat = MaterialFactory.Lit(new Color(0.9f, 0.1f, 0.1f), 2.5f);
            alarmDim = MaterialFactory.Lit(new Color(0.35f, 0.03f, 0.03f), 0.4f);
            var ledOn = MaterialFactory.Unlit(new Color(0.2f, 1f, 0.45f));
            var ledOff = MaterialFactory.Unlit(new Color(0.05f, 0.25f, 0.1f));

            const int units = 5;
            const float unitH = 0.7f;
            for (int i = 0; i < units; i++)
            {
                float y = 0.35f + i * (unitH + 0.05f);
                var box = ShapeFactory.Prim(PrimitiveType.Cube, transform, new Vector3(0f, y, 0f),
                    new Vector3(1.7f, unitH, 1.7f), chassisMat, false, "RackUnit" + i);
                chassis.Add(box.GetComponent<Renderer>());

                // LEDs on the side facing the incoming path (-x) and the camera (-z).
                for (int l = 0; l < 3; l++)
                {
                    AddLed(new Vector3(-0.86f, y + 0.12f, -0.5f + l * 0.25f), ledOn, ledOff);
                    AddLed(new Vector3(-0.5f + l * 0.25f, y + 0.12f, -0.86f), ledOn, ledOff);
                }
            }
            WorldLabel.Create("PROD", transform, new Vector3(0f, units * (unitH + 0.05f) + 0.9f, 0f),
                new Color(0.3f, 1f, 0.55f), 0.14f);
        }

        void AddLed(Vector3 pos, Material on, Material off)
        {
            var led = ShapeFactory.Prim(PrimitiveType.Cube, transform, pos, Vector3.one * 0.12f, on, false, "LED");
            var b = led.AddComponent<Blinker>();
            b.onMat = on; b.offMat = off;
            leds.Add(b);
        }

        public void SetAlarm(bool on)
        {
            alarm = on;
            foreach (var b in leds) if (b != null) b.overridden = on;
            if (!on) foreach (var r in chassis) if (r != null) r.sharedMaterial = chassisMat;
        }

        void Update()
        {
            if (!alarm) return;
            // Unscaled time: the game is paused (timeScale = 0) on game over.
            bool bright = Mathf.Repeat(Time.unscaledTime, 0.5f) < 0.25f;
            var m = bright ? alarmMat : alarmDim;
            foreach (var r in chassis) if (r != null) r.sharedMaterial = m;
            foreach (var b in leds) if (b != null) b.GetComponent<Renderer>().sharedMaterial = m;
        }
    }
}
