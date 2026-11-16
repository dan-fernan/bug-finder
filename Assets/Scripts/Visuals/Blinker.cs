using UnityEngine;

namespace BugDefense
{
    /// <summary>Toggles a renderer between two materials at a random-ish interval (server rack LEDs).</summary>
    public class Blinker : MonoBehaviour
    {
        public Material onMat, offMat;
        public float minInterval = 0.15f, maxInterval = 0.9f;
        public bool overridden; // ProdServer takes over during the alarm

        Renderer rend;
        float timer;
        bool on = true;

        void Awake() { rend = GetComponent<Renderer>(); timer = Random.Range(minInterval, maxInterval); }

        void Update()
        {
            if (overridden || rend == null) return;
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = Random.Range(minInterval, maxInterval);
            on = !on;
            rend.sharedMaterial = on ? onMat : offMat;
        }
    }
}
