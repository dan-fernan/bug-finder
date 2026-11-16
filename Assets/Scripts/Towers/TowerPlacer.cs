using UnityEngine;
using UnityEngine.EventSystems;

namespace BugDefense
{
    /// <summary>[Req C] Player places towers by clicking a buildable tile (mouse raycast).</summary>
    public class TowerPlacer : MonoBehaviour
    {
        BuildTile hovered;
        Material validMat, invalidMat;

        void Awake()
        {
            validMat = MaterialFactory.Lit(new Color(0.15f, 0.45f, 0.3f), 0.5f);
            invalidMat = MaterialFactory.Lit(new Color(0.55f, 0.12f, 0.12f), 0.5f);
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.IsPlaying) { SetHovered(null); return; }

            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            var tile = overUI ? null : RaycastTile();
            SetHovered(tile);

            if (GameInput.LeftClickDown && !overUI && tile != null) TryPlace(tile);
        }

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
            if (hovered == tile)
            {
                if (hovered != null) hovered.SetHighlight(hovered.CanBuild ? validMat : invalidMat);
                return;
            }
            if (hovered != null) hovered.SetHighlight(null);
            hovered = tile;
            if (hovered != null) hovered.SetHighlight(hovered.CanBuild ? validMat : invalidMat);
        }

        void TryPlace(BuildTile tile)
        {
            var hud = GameManager.Instance.Hud;
            if (tile.isPath) { if (hud != null) hud.Toast("Can't build on the path", true); return; }
            if (tile.occupant != null) { if (hud != null) hud.Toast("Tile already has a tower", true); return; }

            Tower.Create(tile);
            GameManager.Instance.RegisterTowerPlaced();
            tile.SetHighlight(invalidMat);
        }
    }
}
