using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BugDefense
{
    /// <summary>
    /// Screen overlay: status panel, always-visible placement instructions ([Req B]),
    /// tower buttons, hover tooltip, toasts, and the game over panel.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        static readonly Color PanelBg = new Color(0.04f, 0.06f, 0.1f, 0.88f);
        static readonly Color TextColor = new Color(0.85f, 0.9f, 0.95f);
        static readonly Color Dim = new Color(0.45f, 0.5f, 0.58f);
        static readonly Color Green = new Color32(0x1F, 0xD1, 0x7A, 0xFF);
        static readonly Color ButtonBg = new Color(0.08f, 0.11f, 0.17f, 0.95f);
        static readonly Color ButtonSelected = new Color(0.12f, 0.3f, 0.24f, 0.98f);

        class TowerButton
        {
            public TowerType type;
            public Image bg, stripe;
            public Text title, stats;
        }

        RectTransform canvasRect;
        Text status, toast, tooltipText;
        RectTransform tooltip;
        GameObject gameOverPanel, victoryPanel, bannerPanel;
        Text gameOverBody, victoryBody, bannerText;
        GameObject helpPanel;
        float toastTimer;
        readonly List<TowerButton> buttons = new List<TowerButton>();

        public void Build()
        {
            var canvas = UIFactory.CreateCanvas(transform);
            canvasRect = (RectTransform)canvas.transform;
            var root = canvas.transform;

            // Top-left status panel.
            var statusPanel = UIFactory.Panel("Status", root, new Vector2(0, 1), new Vector2(20, -20), new Vector2(420, 222), PanelBg);
            status = UIFactory.Label("StatusText", statusPanel.transform, "", 23, TextColor, TextAnchor.UpperLeft);

            // Top-center build-phase banner ("Wave 2/6 starts in 7s - press SPACE").
            var banner = UIFactory.Panel("Banner", root, new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(660, 64), PanelBg);
            bannerPanel = banner.gameObject;
            bannerText = UIFactory.Label("BannerText", banner.transform, "", 24, TextColor, TextAnchor.MiddleCenter);
            bannerText.rectTransform.offsetMin = new Vector2(8, 4); bannerText.rectTransform.offsetMax = new Vector2(-8, -4);

            // Top-right instructions panel: [Req B] clear explanation of how to place towers. Always visible (H hides).
            var help = UIFactory.Panel("Help", root, new Vector2(1, 1), new Vector2(-20, -20), new Vector2(600, 236), PanelBg);
            helpPanel = help.gameObject;
            UIFactory.Label("HelpText", help.transform,
                "<b><color=#1FD17A>HOW TO DEPLOY TOOLS</color></b>\n" +
                "1. Press <b>1-4</b> (or click a button below) to pick a tower\n" +
                "2. Move the mouse over a <b>dark tile</b> (not the green path)\n" +
                "3. <b>Left-click</b> to place. <b>Right-click / Esc</b> to cancel\n" +
                "Each tower costs sprint capacity; +3 per cleared wave.\n" +
                "Max 14 towers. Right-click a tower to remove it.\n" +
                "<color=#94A3B8>SPACE: start wave   F: 2x speed   P: aim mode   H: help</color>\n" +
                "<color=#94A3B8>WASD/wheel: camera   C: reset   M: mute   R: restart</color>\n" +
                "<color=#F87171>Don't let a single bug reach PROD!</color>",
                19, TextColor, TextAnchor.UpperLeft);

            BuildTowerBar(root);

            // Hover tooltip (stats of an existing tower).
            var tip = UIFactory.Panel("Tooltip", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620, 110), PanelBg);
            tip.rectTransform.pivot = new Vector2(0f, 0f);
            tooltip = tip.rectTransform;
            tooltipText = UIFactory.Label("TooltipText", tip.transform, "", 20, TextColor, TextAnchor.MiddleLeft);
            tooltip.gameObject.SetActive(false);

            // Center toast.
            var toastRt = UIFactory.Rect("Toast", root, new Vector2(0.5f, 0.5f), new Vector2(0, 260), new Vector2(1200, 80));
            toast = UIFactory.Label("ToastText", toastRt, "", 34, Color.white, TextAnchor.MiddleCenter);
            toast.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.85f);

            // End screens.
            gameOverPanel = EndPanel(root, "GameOver", "PRODUCTION IS DOWN", new Color(1f, 0.3f, 0.3f),
                new Color(0.12f, 0.02f, 0.03f, 0.93f), out gameOverBody);
            victoryPanel = EndPanel(root, "Victory", "RELEASE SHIPPED", Green,
                new Color(0.02f, 0.1f, 0.06f, 0.93f), out victoryBody);
        }

        static GameObject EndPanel(Transform root, string name, string titleText, Color titleColor, Color bg, out Text body)
        {
            var panel = UIFactory.Panel(name, root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(960, 380), bg, true);
            var title = UIFactory.Label("Title", panel.transform, titleText, 72, titleColor, TextAnchor.UpperCenter);
            title.fontStyle = FontStyle.Bold;
            body = UIFactory.Label("Body", panel.transform, "", 32, TextColor, TextAnchor.LowerCenter);
            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }

        void BuildTowerBar(Transform root)
        {
            const float w = 440f, h = 110f, gap = 14f;
            var types = TowerCatalog.All;
            float total = types.Length * w + (types.Length - 1) * gap;
            for (int i = 0; i < types.Length; i++)
            {
                var type = types[i];
                float x = -total * 0.5f + i * (w + gap) + w * 0.5f;
                var bg = UIFactory.Panel("Btn_" + type.name, root, new Vector2(0.5f, 0f), new Vector2(x, 12f), new Vector2(w, h), ButtonBg, true);
                bg.rectTransform.pivot = new Vector2(0.5f, 0f);

                var btn = bg.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.targetGraphic = bg;
                btn.onClick.AddListener(() =>
                {
                    var placer = GameManager.Instance != null ? GameManager.Instance.Placer : null;
                    if (placer == null) return;
                    if (placer.Selected == type) placer.Deselect(); else placer.Select(type);
                });

                var stripe = UIFactory.Panel("Stripe", bg.transform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(10f, h), type.color);

                var title = UIFactory.Label("Title", bg.transform, "", 26, TextColor, TextAnchor.UpperLeft);
                title.rectTransform.offsetMin = new Vector2(26, 10);
                var stats = UIFactory.Label("Stats", bg.transform, "", 17, Dim, TextAnchor.LowerLeft);
                stats.rectTransform.offsetMin = new Vector2(26, 10);
                stats.text = $"{type.StatLine}\n<i>{type.flavor}</i>";

                buttons.Add(new TowerButton { type = type, bg = bg, stripe = stripe, title = title, stats = stats });
            }
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || status == null) return;

            if (GameInput.KeyDown(KeyCodeLite.H) && helpPanel != null) helpPanel.SetActive(!helpPanel.activeSelf);

            int waveShown = Mathf.Min(gm.WaveIndex + 1, gm.WaveCount);
            string bugs = gm.State == GameState.Wave
                ? $"Bugs remaining: <color=#EF4444>{gm.BugsRemaining}</color>"
                : "Bugs remaining: <color=#94A3B8>-</color>";

            string capColor = gm.CapacityAvailable > 0 ? "#38BDF8" : "#F87171";
            string towerColor = gm.AtTowerCap ? "#F87171" : "#E2E8F0";
            status.text =
                $"<b>BUG DEFENSE</b>   Wave <color=#FACC15>{waveShown} / {gm.WaveCount}</color>\n" +
                bugs + "\n" +
                $"Capacity: <color={capColor}>{gm.CapacityAvailable} / {gm.CapacityTotal} pts</color>\n" +
                $"Towers: <color={towerColor}>{gm.TowerCount} / {gm.maxTowers}</color>\n" +
                $"Bugs squashed: <color=#1FD17A>{gm.BugsSquashed}</color>\n" +
                $"Speed: <color={(gm.Speed > 1f ? "#FACC15" : "#E2E8F0")}>{gm.Speed:0}x</color>   " +
                $"Aim: <color=#A78BFA>{(gm.PathAwareAim ? "path-aware" : "linear")}</color>";

            bool building = gm.State == GameState.Build;
            if (bannerPanel.activeSelf != building) bannerPanel.SetActive(building);
            if (building)
                bannerText.text = $"Wave {gm.WaveIndex + 1}/{gm.WaveCount} starts in <color=#FACC15>{Mathf.CeilToInt(gm.BuildTimeLeft)}s</color>" +
                                  " - press <b>SPACE</b> to start now";

            var selected = gm.Placer != null ? gm.Placer.Selected : null;
            foreach (var b in buttons)
            {
                bool affordable = gm.CanAfford(b.type) && !gm.AtTowerCap;
                b.bg.color = b.type == selected ? ButtonSelected : ButtonBg;
                b.title.text = $"<b>[{b.type.hotkey}] {b.type.name}</b>  -  {b.type.cost} pt{(b.type.cost == 1 ? "" : "s")}";
                b.title.color = affordable ? TextColor : Dim;
                b.stripe.color = affordable ? b.type.color : b.type.color * 0.4f;
            }

            if (toastTimer > 0f)
            {
                toastTimer -= Time.unscaledDeltaTime;
                var c = toast.color;
                c.a = Mathf.Clamp01(toastTimer / 0.5f);
                toast.color = c;
                if (toastTimer <= 0f) toast.text = "";
            }
        }

        public void Toast(string message, bool error = false, float duration = 2f)
        {
            if (toast == null) return;
            toast.text = message;
            toast.color = error ? new Color(1f, 0.45f, 0.45f) : Green;
            toastTimer = duration;
        }

        public void ShowTooltip(string text, Vector2 screenPos)
        {
            if (tooltip == null) return;
            tooltipText.text = text;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, null, out var local))
            {
                // Keep it on screen: flip to the left of the cursor near the right edge.
                var half = canvasRect.rect.size * 0.5f;
                var size = tooltip.sizeDelta;
                float x = local.x + 24f;
                if (x + size.x > half.x) x = local.x - 24f - size.x;
                float y = Mathf.Min(local.y + 24f, half.y - size.y);
                tooltip.anchoredPosition = new Vector2(x, y);
            }
            tooltip.gameObject.SetActive(true);
        }

        public void HideTooltip()
        {
            if (tooltip != null && tooltip.gameObject.activeSelf) tooltip.gameObject.SetActive(false);
        }

        public void ShowGameOver(string culpritName)
        {
            if (gameOverPanel == null) return;
            HideTooltip();
            var gm = GameManager.Instance;
            int survived = gm != null ? gm.WaveIndex : 0;
            gameOverBody.text =
                $"{culpritName} shipped to production. You're on call now.\n" +
                $"Waves survived: {survived} / {(gm != null ? gm.WaveCount : 0)}    Bugs squashed: {(gm != null ? gm.BugsSquashed : 0)}\n\n" +
                "<color=#FACC15>Press R to restart</color>";
            if (bannerPanel != null) bannerPanel.SetActive(false);
            gameOverPanel.SetActive(true);
        }

        public void ShowVictory()
        {
            if (victoryPanel == null) return;
            HideTooltip();
            var gm = GameManager.Instance;
            victoryBody.text =
                "Release shipped. Zero bugs in prod.\n" +
                $"Bugs squashed: {(gm != null ? gm.BugsSquashed : 0)}    Towers deployed: {(gm != null ? gm.TowerCount : 0)}\n\n" +
                "<color=#FACC15>Press R to play again</color>";
            if (bannerPanel != null) bannerPanel.SetActive(false);
            victoryPanel.SetActive(true);
        }
    }
}
