using UnityEngine;

namespace BugDefense
{
    /// <summary>
    /// Fixed tilted camera that fits the whole board (plus spawn pad and PROD rack) into the part of the
    /// screen not covered by HUD panels, for any aspect ratio. Re-frames when the window is resized.
    /// [A+] WASD / arrows pan and the mouse wheel zooms, both clamped; C resets. Uses unscaled time so it works while paused.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public float pitch = 52f;
        /// <summary>Viewport area (0..1) the board must fit inside: leaves room for the tower bar and top panels.</summary>
        public Rect safeArea = new Rect(0.02f, 0.13f, 0.96f, 0.6f);

        [Header("Pan / zoom")]
        public float panSpeed = 18f;
        public float minZoom = 0.45f, maxZoom = 1.2f;
        public Vector2 panLimit = new Vector2(14f, 10f);

        Camera cam;
        float lastAspect;
        Vector3 baseLook;   // look-at point of the fitted framing
        float baseDist;     // camera distance of the fitted framing
        Vector2 pan;
        float zoom = 1f;

        void Awake() { cam = GetComponent<Camera>(); }

        void LateUpdate()
        {
            if (cam != null && !Mathf.Approximately(cam.aspect, lastAspect)) Frame();
            if (baseDist <= 0f) return;

            if (GameInput.KeyDown(KeyCodeLite.C)) { pan = Vector2.zero; zoom = 1f; }
            float dt = Time.unscaledDeltaTime;
            pan += GameInput.PanAxis * panSpeed * zoom * dt;
            pan.x = Mathf.Clamp(pan.x, -panLimit.x, panLimit.x);
            pan.y = Mathf.Clamp(pan.y, -panLimit.y, panLimit.y);
            float scroll = GameInput.ScrollDelta;
            if (scroll != 0f) zoom = Mathf.Clamp(zoom * (1f - scroll * 0.08f), minZoom, maxZoom);

            transform.position = baseLook + new Vector3(pan.x, 0f, pan.y) - transform.forward * baseDist * zoom;
        }

        Vector3[] ContentPoints()
        {
            float ts = GridMap.TileSize;
            float minX = -2f * ts, maxX = (GridMap.Cols + 1) * ts; // spawn pad (x=-1) .. PROD (x=18)
            float maxZ = GridMap.Rows * ts;
            var prod = GridMap.GridToWorld(PathDefinition.ProdCell);
            var pts = new System.Collections.Generic.List<Vector3>
            {
                new Vector3(minX + ts, 0f, 0f), new Vector3(maxX - ts, 0f, 0f),
                new Vector3(minX + ts, 0f, maxZ), new Vector3(maxX - ts, 0f, maxZ),
                prod + new Vector3(-1f, 5.6f, 1f), prod + new Vector3(1f, 5.6f, -1f), // rack top + label
            };
            foreach (var c in PathDefinition.SpawnCells)
                pts.Add(GridMap.GridToWorld(c) + new Vector3(-1.2f, 1.8f, 0f)); // "git push" labels
            return pts.ToArray();
        }

        public void Frame()
        {
            if (cam == null) cam = GetComponent<Camera>();
            if (cam == null) return;
            lastAspect = cam.aspect;

            var pts = ContentPoints();
            var center = new Vector3(GridMap.Cols * GridMap.TileSize * 0.5f, 0f, GridMap.Rows * GridMap.TileSize * 0.5f);
            var rot = Quaternion.Euler(pitch, 0f, 0f);
            float dist = 40f, offsetZ = 0f;

            // Iteratively adjust distance (size) and look-at offset (vertical position) until the projected
            // content fits the safe area. Converges in a handful of steps.
            for (int iter = 0; iter < 40; iter++)
            {
                transform.rotation = rot;
                transform.position = center + new Vector3(0f, 0f, offsetZ) - rot * Vector3.forward * dist;

                float x0 = float.MaxValue, x1 = float.MinValue, y0 = float.MaxValue, y1 = float.MinValue;
                foreach (var p in pts)
                {
                    var v = cam.WorldToViewportPoint(p);
                    x0 = Mathf.Min(x0, v.x); x1 = Mathf.Max(x1, v.x);
                    y0 = Mathf.Min(y0, v.y); y1 = Mathf.Max(y1, v.y);
                }

                float scale = Mathf.Max((x1 - x0) / safeArea.width, (y1 - y0) / safeArea.height);
                float midErr = (y0 + y1) * 0.5f - safeArea.center.y; // > 0: content sits too high
                dist *= Mathf.Lerp(1f, scale, 0.8f);
                offsetZ += midErr * dist * 0.8f; // moving the look-at point forward pushes content down
                if (Mathf.Abs(scale - 1f) < 0.002f && Mathf.Abs(midErr) < 0.002f) break;
            }
            baseLook = center + new Vector3(0f, 0f, offsetZ);
            baseDist = dist;
        }
    }
}
