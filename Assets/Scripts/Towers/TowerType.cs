using UnityEngine;

namespace BugDefense
{
    /// <summary>Plain data describing one tower type. All values are tunable.</summary>
    [System.Serializable]
    public class TowerType
    {
        public string name;
        public string flavor;
        public int hotkey;              // 1-based number key
        public int cost;                // sprint capacity points
        public float fireRate;          // shots per second
        public float projectileSpeed;   // units per second
        public float height;            // [Req B] fire point Y: tower height differs per type
        public float damage;
        public float range;             // horizontal range from the tower center
        public Color color;

        [Header("Ballistic (Compiler)")]
        public bool ballistic;          // gravity projectile on an arc instead of a straight shot
        public bool highArc = true;     // mortar-style lob
        public float splashRadius;      // > 0: damages every bug within this radius of the impact

        [Header("Visuals")]
        public Vector3 projectileScale = Vector3.one * 0.2f;
        public float columnWidth = 1.1f;
        public float headSize = 0.75f;
        public float barrelLength = 0.8f;
        public float barrelWidth = 0.2f;

        public string StatLine =>
            $"Rate {fireRate:0.0}/s | Proj {projectileSpeed:0} | Height {height:0.0} | Dmg {damage:0} | Range {range:0}";
    }
}
