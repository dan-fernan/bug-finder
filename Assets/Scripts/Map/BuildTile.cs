using UnityEngine;

namespace BugDefense
{
    /// <summary>One grid tile. Towers can only be built on non-path tiles with no occupant.</summary>
    public class BuildTile : MonoBehaviour
    {
        public int x, z;
        public bool isPath;
        public Tower occupant;

        public Renderer visual;
        public Material baseMaterial;

        public bool CanBuild => !isPath && occupant == null;

        public void SetHighlight(Material m)
        {
            if (visual != null) visual.sharedMaterial = m != null ? m : baseMaterial;
        }
    }
}
