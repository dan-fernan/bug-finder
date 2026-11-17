using UnityEngine;
using UnityEngine.UI;

namespace BugDefense
{
    /// <summary>Builds uGUI elements from code (no TextMeshPro: it needs an editor import dialog).</summary>
    public static class UIFactory
    {
        static Font font, codeFont;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { font = null; codeFont = null; }

        // Editor-like palette.
        public static readonly Color WindowBg = new Color(0.05f, 0.07f, 0.1f, 0.94f);
        public static readonly Color TitleBg = new Color(0.09f, 0.11f, 0.15f, 1f);
        public static readonly Color Border = new Color(0.19f, 0.23f, 0.31f, 1f);
        public static readonly Color TextMain = new Color(0.86f, 0.9f, 0.95f);
        public static readonly Color TextDim = new Color(0.5f, 0.56f, 0.65f);

        /// <summary>
        /// Monospace OS font for the "code editor" look (Menlo on macOS, Consolas on Windows...).
        /// Falls back to the built-in font if none of them is installed.
        /// </summary>
        public static Font CodeFont
        {
            get
            {
                if (codeFont != null) return codeFont;
                string[] wanted = { "Menlo", "SF Mono", "Consolas", "Monaco", "DejaVu Sans Mono", "Liberation Mono", "Courier New" };
                string[] installed = null;
                try { installed = Font.GetOSInstalledFontNames(); } catch { /* not supported on this platform */ }
                if (installed != null)
                    foreach (var name in wanted)
                        if (System.Array.IndexOf(installed, name) >= 0) { codeFont = Font.CreateDynamicFontFromOSFont(name, 32); break; }
                if (codeFont == null) codeFont = DefaultFont;
                return codeFont;
            }
        }

        public static Font DefaultFont
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                return font;
            }
        }

        public static Canvas CreateCanvas(Transform parent)
        {
            var go = new GameObject("Canvas");
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        /// <summary>Anchored rect. anchor/pivot are both set to <paramref name="anchor"/> (0..1 coords).</summary>
        public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Panel(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color color, bool blocksClicks = false)
        {
            var rt = Rect(name, parent, anchor, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = blocksClicks;
            return img;
        }

        public static Text Label(string name, Transform parent, string text, int size, Color color, TextAnchor align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(16, 12); rt.offsetMax = new Vector2(-16, -12);
            var t = go.AddComponent<Text>();
            t.font = CodeFont;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            t.raycastTarget = false;
            return t;
        }
            /// <summary>
        /// An editor-style window: dark body, 1px border, title bar with three "traffic light" squares and a
        /// file name, and a colored accent line. Returns the content area (below the title bar).
        /// </summary>
        public static RectTransform Window(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size,
                                           string title, Color accent, bool blocksClicks = false)
        {
            const float bar = 30f;
            var border = Panel(name, parent, anchor, pos, size, Border, blocksClicks);
            var body = Panel("Body", border.transform, new Vector2(0.5f, 0.5f), Vector2.zero, size - new Vector2(2f, 2f), WindowBg);

            var titleBar = Panel("TitleBar", body.transform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(size.x - 2f, bar), TitleBg);
            Color[] dots = { new Color(1f, 0.37f, 0.34f), new Color(1f, 0.74f, 0.18f), new Color(0.16f, 0.79f, 0.25f) };
            for (int i = 0; i < 3; i++)
                Panel("Dot", titleBar.transform, new Vector2(0f, 0.5f), new Vector2(12f + i * 18f, 0f), new Vector2(10f, 10f), dots[i])
                    .rectTransform.pivot = new Vector2(0f, 0.5f);
            var t = Label("Title", titleBar.transform, title, 16, TextDim, TextAnchor.MiddleLeft);
            t.rectTransform.offsetMin = new Vector2(72f, 0f); t.rectTransform.offsetMax = new Vector2(-10f, 0f);
            Panel("Accent", body.transform, new Vector2(0.5f, 1f), new Vector2(0f, -bar), new Vector2(size.x - 2f, 2f), accent);

            var content = Rect("Content", body.transform, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(size.x - 2f, size.y - 2f - bar - 2f));
            return content;
        }
    }
}
