using UnityEngine;

namespace BugDefense
{
    /// <summary>
    /// Fixed tilted camera that frames the whole board (plus spawn and PROD) for any aspect ratio.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public float pitch = 55f;
        public float margin = 1.08f;

        Camera cam;
        float lastAspect;

        void Awake() { cam = GetComponent<Camera>(); }

        void LateUpdate()
        {
            if (cam != null && !Mathf.Approximately(cam.aspect, lastAspect)) Frame();
        }

        public void Frame()
        {
            if (cam == null) cam = GetComponent<Camera>();
            if (cam == null) return;
            lastAspect = cam.aspect;

            // Content bounds in world space: grid plus spawn pad (x=-1) and PROD rack (x=18).
            float minX = -2f * GridMap.TileSize, maxX = (GridMap.Cols + 1) * GridMap.TileSize;
            float minZ = 0f, maxZ = GridMap.Rows * GridMap.TileSize;
            var center = new Vector3((minX + maxX) * 0.5f, 0f, (minZ + maxZ) * 0.5f);
            float halfW = (maxX - minX) * 0.5f;
            float halfD = (maxZ - minZ) * 0.5f;

            float vHalf = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float hHalf = Mathf.Atan(Mathf.Tan(vHalf) * cam.aspect);
            float p = pitch * Mathf.Deg2Rad;

            // Width constraint, and depth constraint (board depth projected onto the view plane, plus
            // a perspective allowance since the near edge sits closer to the camera).
            float distW = halfW / Mathf.Tan(hHalf);
            float projectedDepth = halfD * Mathf.Sin(p) + 1.5f; // +1.5 for PROD rack height
            float distD = projectedDepth / Mathf.Tan(vHalf) + halfD * Mathf.Cos(p);
            float dist = Mathf.Max(distW, distD) * margin;

            var rot = Quaternion.Euler(pitch, 0f, 0f);
            transform.rotation = rot;
            transform.position = center - rot * Vector3.forward * dist;
        }
    }
}
