using UnityEngine;

namespace BugDefense
{
    /// <summary>
    /// [Req B] Tower types with different rate of fire, projectile speed, and tower height.
    /// Taller towers see farther, which is why the Debugger has the most range.
    /// </summary>
    public static class TowerCatalog
    {
        public static readonly TowerType Linter = new TowerType
        {
            name = "Linter", flavor = "Catches small stuff, constantly.", hotkey = 1,
            cost = 1, fireRate = 4.0f, projectileSpeed = 30f, height = 1.0f, damage = 4f, range = 7f,
            color = new Color32(0x38, 0xBD, 0xF8, 0xFF),
            projectileScale = Vector3.one * 0.15f,
            columnWidth = 1.2f, headSize = 0.6f, barrelLength = 0.6f, barrelWidth = 0.14f,
            modelStack = new[] { "tower-round-base" }, modelWeapon = "weapon-turret",
        };

        public static readonly TowerType UnitTest = new TowerType
        {
            name = "Unit Test", flavor = "Reliable. Boring. Essential.", hotkey = 2,
            cost = 2, fireRate = 1.5f, projectileSpeed = 20f, height = 2.2f, damage = 14f, range = 9f,
            color = new Color32(0xFA, 0xCC, 0x15, 0xFF),
            projectileScale = Vector3.one * 0.25f,
            columnWidth = 1.0f, headSize = 0.8f, barrelLength = 0.8f, barrelWidth = 0.22f,
            modelStack = new[] { "tower-round-bottom-a", "tower-round-top-a" }, modelWeapon = "weapon-ballista",
        };

        public static readonly TowerType Debugger = new TowerType
        {
            name = "Debugger", flavor = "Sets a breakpoint. Hard.", hotkey = 3,
            cost = 3, fireRate = 0.5f, projectileSpeed = 14f, height = 3.8f, damage = 55f, range = 13f,
            color = new Color32(0xF8, 0x71, 0x71, 0xFF),
            projectileScale = new Vector3(0.3f, 0.3f, 0.6f), // elongated slug
            columnWidth = 0.8f, headSize = 0.9f, barrelLength = 1.3f, barrelWidth = 0.28f,
            modelStack = new[] { "tower-round-bottom-b", "tower-round-middle-a", "tower-round-middle-b", "tower-round-top-b" }, modelWeapon = "weapon-cannon",
        };

        /// <summary>[A+] Ballistic mortar: gravity-affected shell on a high arc, splash damage on impact.</summary>
        public static readonly TowerType Compiler = new TowerType
        {
            name = "Compiler", flavor = "Lobs builds. Splash: errors everywhere.", hotkey = 4,
            cost = 3, fireRate = 0.4f, projectileSpeed = 16f, height = 1.5f, damage = 30f, range = 12f,
            color = new Color32(0xA7, 0x8B, 0xFA, 0xFF),
            projectileScale = Vector3.one * 0.45f,
            columnWidth = 1.35f, headSize = 0.95f, barrelLength = 0.55f, barrelWidth = 0.5f,
            modelStack = new[] { "tower-square-bottom-a" }, modelWeapon = "weapon-catapult",
            ballistic = true, highArc = true, splashRadius = 2.5f,
        };

        public static readonly TowerType[] All = { Linter, UnitTest, Debugger, Compiler };

        public static TowerType ByHotkey(int key)
        {
            foreach (var t in All) if (t.hotkey == key) return t;
            return null;
        }
    }
}
