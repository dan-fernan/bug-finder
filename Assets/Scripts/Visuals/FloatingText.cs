using UnityEngine;

namespace BugDefense
{
    /// <summary>[A+] World-space text that rises and fades ("fixed!" when a bug dies).</summary>
    public class FloatingText : MonoBehaviour
    {
        TextMesh tm;
        Color color;
        float age, life = 0.9f, riseSpeed = 1.6f;

        public static void Spawn(string text, Vector3 pos, Color color, float size = 0.09f)
        {
            var parent = GameManager.Instance != null ? GameManager.Instance.transform : null;
            var label = WorldLabel.Create(text, parent, Vector3.zero, color, size);
            label.transform.position = pos;
            var ft = label.gameObject.AddComponent<FloatingText>();
            ft.tm = label.GetComponent<TextMesh>();
            ft.tm.fontStyle = FontStyle.Bold;
            ft.color = color;
        }

        void Update()
        {
            age += Time.deltaTime;
            transform.position += Vector3.up * riseSpeed * Time.deltaTime;
            if (tm != null)
            {
                var c = color;
                c.a = 1f - Mathf.Clamp01((age - life * 0.4f) / (life * 0.6f)); // hold, then fade
                tm.color = c;
            }
            if (age >= life) Destroy(gameObject);
        }
    }
}
