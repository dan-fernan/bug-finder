using UnityEngine;
using UnityEngine.UI;

namespace BugDefense
{
    /// <summary>Screen overlay: status, placement instructions, toasts, and the game over panel.</summary>
    public class HUD : MonoBehaviour
    {
        static readonly Color PanelBg = new Color(0.04f, 0.06f, 0.1f, 0.85f);
        static readonly Color TextColor = new Color(0.85f, 0.9f, 0.95f);
        static readonly Color Green = new Color32(0x1F, 0xD1, 0x7A, 0xFF);

        Text status, toast;
        GameObject gameOverPanel;
        Text gameOverBody;
        float toastTimer;

        public void Build()
        {
            var canvas = UIFactory.CreateCanvas(transform);
            var root = canvas.transform;

            // Top-left status panel.
            var statusPanel = UIFactory.Panel("Status", root, new Vector2(0, 1), new Vector2(20, -20), new Vector2(380, 150), PanelBg);
            status = UIFactory.Label("StatusText", statusPanel.transform, "", 24, TextColor, TextAnchor.UpperLeft);

            // Top-right instructions panel: how to place towers.
            var help = UIFactory.Panel("Help", root, new Vector2(1, 1), new Vector2(-20, -20), new Vector2(560, 190), PanelBg);
            UIFactory.Label("HelpText", help.transform,
                "<b><color=#1FD17A>HOW TO DEPLOY TOOLS</color></b>\n" +
                "1. Move the mouse over a dark tile (not the green path)\n" +
                "2. Left-click to place a Linter tower\n" +
                "Towers auto-aim and fire at bugs in range.\n" +
                "R: restart\n" +
                "<color=#F87171>Don't let a single bug reach PROD!</color>",
                22, TextColor, TextAnchor.UpperLeft);

            // Center toast.
            var toastRt = UIFactory.Rect("Toast", root, new Vector2(0.5f, 0.5f), new Vector2(0, 220), new Vector2(1000, 80));
            toast = UIFactory.Label("ToastText", toastRt, "", 34, Color.white, TextAnchor.MiddleCenter);
            toast.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);

            // Game over panel.
            var go = UIFactory.Panel("GameOver", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 360),
                new Color(0.12f, 0.02f, 0.03f, 0.92f), true);
            gameOverPanel = go.gameObject;
            var title = UIFactory.Label("Title", go.transform, "PRODUCTION IS DOWN", 72, new Color(1f, 0.3f, 0.3f), TextAnchor.UpperCenter);
            title.fontStyle = FontStyle.Bold;
            gameOverBody = UIFactory.Label("Body", go.transform, "", 32, TextColor, TextAnchor.LowerCenter);
            gameOverPanel.SetActive(false);
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || status == null) return;

            string state;
            var sp = gm.Spawner;
            if (sp != null && sp.TimeUntilFirst > 0f) state = $"First bugs in <color=#FACC15>{Mathf.CeilToInt(sp.TimeUntilFirst)}s</color>";
            else state = $"Bugs on the path: <color=#EF4444>{EnemyRegistry.Count}</color>";

            status.text =
                "<b>BUG DEFENSE</b>\n" +
                state + "\n" +
                $"Bugs squashed: <color=#1FD17A>{gm.BugsSquashed}</color>\n" +
                $"Towers: {gm.TowersPlaced}";

            if (toastTimer > 0f)
            {
                toastTimer -= Time.unscaledDeltaTime;
                var c = toast.color;
                c.a = Mathf.Clamp01(toastTimer / 0.5f);
                toast.color = c;
                if (toastTimer <= 0f) toast.text = "";
            }
        }

        public void Toast(string message, bool error = false)
        {
            if (toast == null) return;
            toast.text = message;
            toast.color = error ? new Color(1f, 0.45f, 0.45f) : Green;
            toastTimer = 2f;
        }

        public void ShowGameOver(string culpritName)
        {
            if (gameOverPanel == null) return;
            var gm = GameManager.Instance;
            gameOverBody.text =
                $"{culpritName} shipped to production. You're on call now.\n" +
                $"Bugs squashed: {(gm != null ? gm.BugsSquashed : 0)}\n\n" +
                "<color=#FACC15>Press R to restart</color>";
            gameOverPanel.SetActive(true);
        }
    }
}
