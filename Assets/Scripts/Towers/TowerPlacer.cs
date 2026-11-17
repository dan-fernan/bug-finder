using UnityEngine;
using UnityEngine.EventSystems;

namespace BugDefense
{
    /// <summary>
    /// [Req C] Player places towers with the mouse. Pick a type (1-3 or HUD button), a translucent ghost
    /// plus range ring follows the hovered tile (green = valid, red = invalid), left-click to place.
    /// Right-click a placed tower (with nothing selected) to remove it for a refund.
    /// </summary>
    public class TowerPlacer : MonoBehaviour
    {
        public TowerType Selected { get; private set; }

        static readonly Color ValidColor = new Color(0.25f, 1f, 0.55f);
        static readonly Color InvalidColor = new Color(1f, 0.3f, 0.3f);
        static readonly Color InfoColor = new Color(0.75f, 0.85f, 1f);

        BuildTile hovered;
        Transform ghost;
        TowerType ghostType;
        bool ghostValid;
        Renderer[] ghostRenderers;
        RangeRing ring;
        Material ghostValidMat, ghostInvalidMat, tileValidMat, tileInvalidMat;

        void Awake()
        {
            ghostValidMat = MaterialFactory.Transparent(new Color(0.3f, 1f, 0.55f, 0.45f));
            ghostInvalidMat = MaterialFactory.Transparent(new Color(1f, 0.25f, 0.25f, 0.45f));
            tileValidMat = MaterialFactory.Lit(new Color(0.15f, 0.45f, 0.3f), 0.5f);
            tileInvalidMat = MaterialFactory.Lit(new Color(0.55f, 0.12f, 0.12f), 0.5f);
            ring = RangeRing.Create(transform);
        }

        public void Select(TowerType type)
        {
            Selected = type;
            if (type != null && GameManager.Instance != null && GameManager.Instance.Hud != null)
                GameManager.Instance.Hud.Toast($"{type.name} selected - click a dark tile", false, 1.2f);
        }

        public void Deselect() => Selected = null;

        void Update()
        {
            var gm = GameManager.Instance;
            var hud = gm != null ? gm.Hud : null;
            if (gm == null || !gm.IsPlaying)
            {
                SetHovered(null);
                HidePreview();
                if (hud != null) hud.HideTooltip();
                return;
            }

            HandleKeys();

            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            var tile = overUI ? null : RaycastTile();
            SetHovered(tile);

            if (Selected != null) UpdatePlacing(tile, overUI);
            else UpdateInspecting(tile, overUI);
        }

        void HandleKeys()
        {
            for (int k = 1; k <= 4; k++)
            {
                if (!GameInput.KeyDown(KeyCodeLite.Alpha1 + (k - 1))) continue;
                var t = TowerCatalog.ByHotkey(k);
                if (t == null) continue;
                if (Selected == t) Deselect(); else Select(t); // pressing the same key again toggles off
            }
            if (GameInput.KeyDown(KeyCodeLite.Escape)) Deselect();
        }

        // ---------- Placing ----------

        void UpdatePlacing(BuildTile tile, bool overUI)
        {
            var hud = GameManager.Instance.Hud;
            if (hud != null) hud.HideTooltip();

            if (GameInput.RightClickDown) { Deselect(); HidePreview(); return; }

            if (tile == null) { HidePreview(); return; }

            string error = GetPlacementError(tile, Selected);
            ShowGhost(tile, Selected, error == null);
            tile.SetHighlight(error == null ? tileValidMat : tileInvalidMat);

            if (GameInput.LeftClickDown && !overUI)
            {
                if (error != null) { if (hud != null) hud.Toast(error, true); Sfx.Play(SfxId.Error); return; }
                Place(tile, Selected);
                Sfx.Play(SfxId.Place, 0.8f);
            }
        }

        /// <summary>Returns null if placement is allowed, otherwise a player-facing reason.</summary>
        public string GetPlacementError(BuildTile tile, TowerType type)
        {
            var gm = GameManager.Instance;
            if (tile.isPath) return "Tile is on the path";
            if (tile.occupant != null) return "Tile already has a tower";
            if (gm.AtTowerCap) return $"Tower limit reached ({gm.maxTowers} max)";
            if (!gm.CanAfford(type)) return $"Not enough sprint capacity ({type.name} needs {type.cost} pts)";
            return null;
        }

        void Place(BuildTile tile, TowerType type)
        {
            Tower.Create(tile, type);
            GameManager.Instance.OnTowerPlaced(type);
            // Selection stays active so several towers can be placed in a row.
        }

        // ---------- Inspecting / removing ----------

        void UpdateInspecting(BuildTile tile, bool overUI)
        {
            HideGhost();
            var hud = GameManager.Instance.Hud;
            var tower = tile != null ? tile.occupant : null;

            if (tile != null) tile.SetHighlight(null);

            if (tower == null)
            {
                ring.Hide();
                if (hud != null) hud.HideTooltip();
                return;
            }

            ring.Show(tower.transform.position, tower.type.range, InfoColor);
            if (hud != null)
            {
                var gm = GameManager.Instance;
                int refund = gm.State == GameState.Wave ? Mathf.CeilToInt(tower.type.cost * gm.midWaveRefund) : tower.type.cost;
                hud.ShowTooltip(
                    $"<b>{tower.type.name}</b>  <i>{tower.type.flavor}</i>\n{tower.type.StatLine}\n" +
                    $"<color=#94A3B8>Right-click to remove (+{refund} pts)</color>",
                    GameInput.MousePosition);
            }

            if (GameInput.RightClickDown && !overUI) Remove(tower);
        }

        void Remove(Tower tower)
        {
            var gm = GameManager.Instance;
            int refund = gm.OnTowerRemoved(tower.type);
            if (tower.Tile != null) tower.Tile.occupant = null;
            if (gm.Hud != null) gm.Hud.Toast($"Removed {tower.type.name} (+{refund} pts)", false);
            Destroy(tower.gameObject);
            ring.Hide();
        }

        // ---------- Helpers ----------

        BuildTile RaycastTile()
        {
            var cam = Camera.main;
            if (cam == null) return null;
            Ray ray = cam.ScreenPointToRay(GameInput.MousePosition);
            if (!Physics.Raycast(ray, out var hit, 500f)) return null;
            return hit.collider.GetComponentInParent<BuildTile>();
        }

        void SetHovered(BuildTile tile)
        {
            if (hovered == tile) return;
            if (hovered != null) hovered.SetHighlight(null);
            hovered = tile;
        }

        void ShowGhost(BuildTile tile, TowerType type, bool valid)
        {
            if (ghost == null || ghostType != type)
            {
                HideGhost(true);
                ghost = new GameObject("Ghost_" + type.name).transform;
                ghost.SetParent(transform, false);
                TowerModel.Build(ghost, type, valid ? ghostValidMat : ghostInvalidMat, false);
                ghostRenderers = ghost.GetComponentsInChildren<Renderer>();
                foreach (var r in ghostRenderers) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                ghostType = type;
                ghostValid = valid;
            }
            if (ghostValid != valid)
            {
                foreach (var r in ghostRenderers) r.sharedMaterial = valid ? ghostValidMat : ghostInvalidMat;
                ghostValid = valid;
            }
            ghost.gameObject.SetActive(true);
            ghost.position = tile.transform.position;
            ring.Show(tile.transform.position, type.range, valid ? ValidColor : InvalidColor);
        }

        void HideGhost(bool destroy = false)
        {
            if (ghost == null) return;
            if (destroy) { Destroy(ghost.gameObject); ghost = null; ghostType = null; }
            else ghost.gameObject.SetActive(false);
        }

        void HidePreview()
        {
            HideGhost();
            ring.Hide();
        }
    }
}
