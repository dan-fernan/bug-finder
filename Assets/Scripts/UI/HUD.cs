using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BugDefense
{
    /// <summary>
    /// Screen overlay styled like a code editor: status window, always-visible placement instructions
    /// ([Req B]), tower cards, hover tooltip, toasts, level select, and the end-of-game windows.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        static readonly Color TextColor = UIFactory.TextMain;
        static readonly Color Dim = UIFactory.TextDim;
        static readonly Color Green = new Color32(0x1F, 0xD1, 0x7A, 0xFF);
        static readonly Color Red = new Color32(0xF8, 0x71, 0x71, 0xFF);
        static readonly Color Blue = new Color32(0x38, 0xBD, 0xF8, 0xFF);
        static readonly Color CardBg = new Color(0.06f, 0.08f, 0.12f, 0.95f);
        static readonly Color CardSelected = new Color(0.08f, 0.2f, 0.16f, 0.98f);

        class TowerButton
        {
            public TowerType type;
            public Image bg, accent;
            public Text title, cost;
        }

        RectTransform canvasRect;
        Text status, toast, tooltipText, bannerText;
        RectTransform tooltip;
        GameObject gameOverPanel, victoryPanel, bannerPanel, helpPanel, levelPanel;
        Text gameOverBody, victoryBody;
        float toastTimer;
        readonly List<TowerButton> buttons = new List<TowerButton>();

        public void Build()
        {
            var canvas = UIFactory.CreateCanvas(transform);
            canvasRect = (RectTransform)canvas.transform;
            var root = canvas.transform;

            // Top-left status window.
            var st = UIFactory.Window("Status", root, new Vector2(0, 1), new Vector2(20, -20), new Vector2(430, 238), "status.log", Green);
            status = UIFactory.Label("StatusText", st, "", 19, TextColor, TextAnchor.UpperLeft);
            status.lineSpacing = 1.05f;

            // Top-center build-phase banner, styled as a terminal prompt.
            var banner = UIFactory.Panel("Banner", root, new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(640, 54), UIFactory.Border);
            var bannerBody = UIFactory.Panel("Body", banner.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(638, 52), UIFactory.WindowBg);
            bannerPanel = banner.gameObject;
            bannerText = UIFactory.Label("BannerText", bannerBody.transform, "", 20, TextColor, TextAnchor.MiddleCenter);
            bannerText.rectTransform.offsetMin = new Vector2(8, 2); bannerText.rectTransform.offsetMax = new Vector2(-8, -2);

            // Top-right instructions: [Req B] clear explanation of how to place towers. Always visible (H hides).
            var help = UIFactory.Window("Help", root, new Vector2(1, 1), new Vector2(-20, -20), new Vector2(560, 272), "README.md", Blue);
            helpPanel = help.parent.parent.gameObject;
            var helpText = UIFactory.Label("HelpText", help,
                "<color=#38BDF8><b>## How to deploy tools</b></color>\n" +
                "1. Press <b>1-4</b> or click a card below\n" +
                "2. Hover a <b>dark tile</b> (not the green bus)\n" +
                "3. <b>Left-click</b> to place, <b>Esc</b> to cancel\n" +
                "<color=#94A3B8>- Towers cost sprint capacity (+3/wave)</color>\n" +
                "<color=#94A3B8>- Max 14. Right-click a tower to remove</color>\n" +
                "<color=#64748B>SPACE wave  F 2x  P aim  H help  R retry</color>\n" +
                "<color=#64748B>WASD/wheel camera  C reset  M mute</color>\n" +
                "<color=#F87171>> Don't let a single bug reach PROD</color>",
                16, TextColor, TextAnchor.UpperLeft);
            helpText.lineSpacing = 1.05f;

            BuildTowerBar(root);

            // Hover tooltip (stats of an existing tower).
            var tip = UIFactory.Panel("Tooltip", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640, 104), UIFactory.Border);
            UIFactory.Panel("Body", tip.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(638, 102), UIFactory.WindowBg);
            tip.rectTransform.pivot = new Vector2(0f, 0f);
            tooltip = tip.rectTransform;
            tooltipText = UIFactory.Label("TooltipText", tip.transform, "", 17, TextColor, TextAnchor.MiddleLeft);
            tooltip.gameObject.SetActive(false);

            // Center toast.
            var toastRt = UIFactory.Rect("Toast", root, new Vector2(0.5f, 0.5f), new Vector2(0, 250), new Vector2(1300, 80));
            toast = UIFactory.Label("ToastText", toastRt, "", 30, Color.white, TextAnchor.MiddleCenter);
            toast.fontStyle = FontStyle.Bold;
            toast.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.9f);

            // End screens.
            gameOverPanel = EndPanel(root, "GameOver", "incident.log", "PRODUCTION IS DOWN", Red, out gameOverBody);
            victoryPanel = EndPanel(root, "Victory", "release.log", "RELEASE SHIPPED", Green, out victoryBody);

            BuildLevelSelect(root);
        }

        static GameObject EndPanel(Transform root, string name, string file, string titleText, Color accent, out Text body)
        {
            var content = UIFactory.Window(name, root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(960, 360), file, accent, true);
            var title = UIFactory.Label("Title", content, titleText, 60, accent, TextAnchor.UpperCenter);
            title.fontStyle = FontStyle.Bold;
            title.rectTransform.offsetMax = new Vector2(-16, -24);
            body = UIFactory.Label("Body", content, "", 24, TextColor, TextAnchor.LowerCenter);
            body.rectTransform.offsetMin = new Vector2(16, 28);
            var panel = content.parent.parent.gameObject;
            panel.SetActive(false);
            return panel;
        }

        void BuildTowerBar(Transform root)
        {
            const float w = 440f, h = 104f, gap = 14f;
            var types = TowerCatalog.All;
            float total = types.Length * w + (types.Length - 1) * gap;
            for (int i = 0; i < types.Length; i++)
            {
                var type = types[i];
                float x = -total * 0.5f + i * (w + gap) + w * 0.5f;
                var border = UIFactory.Panel("Btn_" + type.name, root, new Vector2(0.5f, 0f), new Vector2(x, 12f), new Vector2(w, h), UIFactory.Border, true);
                border.rectTransform.pivot = new Vector2(0.5f, 0f);
                var bg = UIFactory.Panel("Body", border.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(w - 2f, h - 2f), CardBg);

                var btn = border.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.targetGraphic = border;
                btn.onClick.AddListener(() =>
                {
                    var placer = GameManager.Instance != null ? GameManager.Instance.Placer : null;
                    if (placer == null) return;
                    if (placer.Selected == type) placer.Deselect(); else placer.Select(type);
                });

                // Colored accent along the top, like an editor tab.
                var accent = UIFactory.Panel("Accent", bg.transform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(w - 2f, 4f), type.color);

                var title = UIFactory.Label("Title", bg.transform, "", 22, TextColor, TextAnchor.UpperLeft);
                title.rectTransform.offsetMin = new Vector2(16, 8); title.rectTransform.offsetMax = new Vector2(-16, -12);
                var cost = UIFactory.Label("Cost", bg.transform, "", 18, Dim, TextAnchor.UpperRight);
                cost.rectTransform.offsetMin = new Vector2(16, 8); cost.rectTransform.offsetMax = new Vector2(-16, -14);
                var stats = UIFactory.Label("Stats", bg.transform, "", 14, Dim, TextAnchor.LowerLeft);
                stats.rectTransform.offsetMin = new Vector2(16, 8);
                stats.text = $"rate {type.fireRate:0.0}/s  proj {type.projectileSpeed:0}  height {type.height:0.0}\n" +
                             $"dmg {type.damage:0}{(type.splashRadius > 0f ? " splash" : "")}  range {type.range:0}\n" +
                             $"<color=#64748B>// {type.flavor}</color>";

                buttons.Add(new TowerButton { type = type, bg = bg, accent = accent, title = title, cost = cost });
            }
        }

        // ---------- Level select ----------

        void BuildLevelSelect(Transform root)
        {
            var levels = LevelCatalog.All;
            float cardH = 104f, gap = 14f;
            float h = 30f + 70f + levels.Length * (cardH + gap) + 40f;
            var content = UIFactory.Window("LevelSelect", root, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(820f, h),
                "~/bug-defense $ git checkout", Green, true);
            levelPanel = content.parent.parent.gameObject;

            var header = UIFactory.Label("Header", content, "<b>BUG DEFENSE</b>  <color=#64748B>// don't ship to prod</color>\n" +
                "<color=#94A3B8>Pick a branch to defend:</color>", 22, TextColor, TextAnchor.UpperLeft);
            header.rectTransform.offsetMin = new Vector2(24, 0); header.rectTransform.offsetMax = new Vector2(-24, -14);

            for (int i = 0; i < levels.Length; i++)
            {
                int index = i;
                var lv = levels[i];
                float y = -(84f + i * (cardH + gap));
                var card = UIFactory.Panel("Level" + i, content, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(760f, cardH), UIFactory.Border, true);
                var body = UIFactory.Panel("Body", card.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(758f, cardH - 2f), CardBg);
                Color diff = i == 0 ? Green : i == 1 ? (Color)new Color32(0xFA, 0xCC, 0x15, 0xFF) : Red;
                var stripe = UIFactory.Panel("Stripe", body.transform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(6f, cardH - 2f), diff);
                stripe.rectTransform.pivot = new Vector2(0f, 0.5f);

                int routes = lv.Routes.Count, spawns = lv.SpawnIds.Count;
                var text = UIFactory.Label("Text", body.transform,
                    $"<b>[{i + 1}] {lv.name}</b>\n" +
                    $"<color=#94A3B8>{lv.tagline}</color>\n" +
                    $"<color=#64748B>{routes} routes  |  {spawns} spawn{(spawns > 1 ? "s" : "")}  |  bug HP x{lv.hpScale:0.0}  |  start {lv.startingCapacity} pts</color>",
                    19, TextColor, TextAnchor.MiddleLeft);
                text.rectTransform.offsetMin = new Vector2(26, 8);

                // The card's border lights up in the difficulty color on hover.
                var btn = card.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.ColorTint;
                btn.targetGraphic = card;
                var colors = btn.colors;
                colors.normalColor = UIFactory.Border; colors.highlightedColor = diff; colors.selectedColor = UIFactory.Border;
                colors.pressedColor = Color.white; colors.colorMultiplier = 1f;
                btn.colors = colors;
                card.color = Color.white; // the tint above provides the actual border color
                btn.onClick.AddListener(() => { if (GameManager.Instance != null) GameManager.Instance.PlayLevel(index); });
            }

            var foot = UIFactory.Label("Footer", content, "<color=#64748B>Click a branch or press 1-3</color>", 16, Dim, TextAnchor.LowerCenter);
            foot.rectTransform.offsetMin = new Vector2(16, 12);
            levelPanel.SetActive(false);
        }

        public void ShowLevelSelect(bool show)
        {
            if (levelPanel != null) levelPanel.SetActive(show);
            if (show) { HideTooltip(); if (bannerPanel != null) bannerPanel.SetActive(false); }
        }

        // ---------- Per-frame ----------

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || status == null) return;

            if (GameInput.KeyDown(KeyCodeLite.H) && helpPanel != null) helpPanel.SetActive(!helpPanel.activeSelf);

            int waveShown = Mathf.Min(gm.WaveIndex + 1, gm.WaveCount);
            string bugs = gm.State == GameState.Wave ? $"<color=#EF4444>{gm.BugsRemaining}</color> remaining" : "<color=#64748B>--</color>";
            string capColor = gm.CapacityAvailable > 0 ? "#38BDF8" : "#F87171";
            string towerColor = gm.AtTowerCap ? "#F87171" : "#E2E8F0";
            status.text =
                $"<color=#64748B>branch  </color><color=#1FD17A>{PathDefinition.Level.name}</color>\n" +
                $"<color=#64748B>wave    </color><color=#FACC15>{waveShown}/{gm.WaveCount}</color>\n" +
                $"<color=#64748B>bugs    </color>{bugs}\n" +
                $"<color=#64748B>capacity</color> <color={capColor}>{gm.CapacityAvailable}/{gm.CapacityTotal} pts</color>\n" +
                $"<color=#64748B>towers  </color><color={towerColor}>{gm.TowerCount}/{gm.maxTowers}</color>\n" +
                $"<color=#64748B>fixed   </color><color=#1FD17A>{gm.BugsSquashed}</color>\n" +
                $"<color=#64748B>speed   </color><color={(gm.Speed > 1f ? "#FACC15" : "#E2E8F0")}>{gm.Speed:0}x</color>" +
                $"  <color=#64748B>aim</color> <color=#A78BFA>{(gm.PathAwareAim ? "path-aware" : "linear")}</color>";

            bool building = gm.State == GameState.Build;
            if (bannerPanel.activeSelf != building) bannerPanel.SetActive(building);
            if (building)
                bannerText.text = $"<color=#1FD17A>$</color> ./deploy --wave {gm.WaveIndex + 1}  " +
                                  $"<color=#64748B>starts in</color> <color=#FACC15>{Mathf.CeilToInt(gm.BuildTimeLeft)}s</color>  " +
                                  "<color=#64748B>[SPACE] now</color>";

            var selected = gm.Placer != null ? gm.Placer.Selected : null;
            foreach (var b in buttons)
            {
                bool affordable = gm.CanAfford(b.type) && !gm.AtTowerCap;
                b.bg.color = b.type == selected ? CardSelected : CardBg;
                b.title.text = $"<color=#64748B>[{b.type.hotkey}]</color> <b>{b.type.name}</b>";
                b.title.color = affordable ? TextColor : Dim;
                b.cost.text = $"{b.type.cost} pt{(b.type.cost == 1 ? "" : "s")}";
                b.cost.color = affordable ? b.type.color : Red;
                b.accent.color = affordable ? b.type.color : b.type.color * 0.35f;
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
            toast.text = (error ? "! " : "> ") + message;
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
                $"<color=#F87171>FATAL:</color> {culpritName} shipped to production. You're on call now.\n" +
                $"<color=#64748B>waves survived</color> {survived}/{(gm != null ? gm.WaveCount : 0)}   " +
                $"<color=#64748B>bugs fixed</color> {(gm != null ? gm.BugsSquashed : 0)}\n\n" +
                "<color=#FACC15>[R] retry</color>    <color=#94A3B8>[L] pick another branch</color>";
            if (bannerPanel != null) bannerPanel.SetActive(false);
            gameOverPanel.SetActive(true);
        }

        public void ShowVictory()
        {
            if (victoryPanel == null) return;
            HideTooltip();
            var gm = GameManager.Instance;
            string next = LevelCatalog.HasNext
                ? $"<color=#1FD17A>[N] next branch: {LevelCatalog.All[LevelCatalog.CurrentIndex + 1].name}</color>    "
                : "<color=#1FD17A>All branches merged. You beat Bug Defense!</color>\n";
            victoryBody.text =
                $"{PathDefinition.Level.name} merged. Zero bugs in prod.\n" +
                $"<color=#64748B>bugs fixed</color> {(gm != null ? gm.BugsSquashed : 0)}   " +
                $"<color=#64748B>towers deployed</color> {(gm != null ? gm.TowerCount : 0)}\n\n" +
                next + "<color=#FACC15>[R] replay</color>    <color=#94A3B8>[L] branches</color>";
            if (bannerPanel != null) bannerPanel.SetActive(false);
            victoryPanel.SetActive(true);
        }
    }
}
