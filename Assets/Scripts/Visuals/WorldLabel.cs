using UnityEngine;

namespace BugDefense
{
    /// <summary>World-space text label (legacy TextMesh, no TextMeshPro needed) that faces the camera.</summary>
    public class WorldLabel : MonoBehaviour
    {
        public static WorldLabel Create(string text, Transform parent, Vector3 localPos, Color color, float size = 0.12f)
        {
            var go = new GameObject("Label_" + text);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.font = UIFactory.CodeFont;
            tm.fontSize = 64;
            tm.characterSize = size;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null && tm.font != null) mr.sharedMaterial = tm.font.material;

            return go.AddComponent<WorldLabel>();
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;
        }
    }
}
