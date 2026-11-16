using System.Collections.Generic;
using UnityEngine;

namespace BugDefense
{
    /// <summary>Static list of live enemies. Towers target from here (no layers / OverlapSphere needed).</summary>
    public static class EnemyRegistry
    {
        static readonly List<Enemy> enemies = new List<Enemy>();

        public static IReadOnlyList<Enemy> All => enemies;
        public static int Count => enemies.Count;

        public static void Register(Enemy e) { if (e != null && !enemies.Contains(e)) enemies.Add(e); }
        public static void Unregister(Enemy e) { enemies.Remove(e); }
        public static void Clear() { enemies.Clear(); }

        // Domain reload may be disabled in Play Mode options, so reset statics explicitly.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { enemies.Clear(); }
    }
}
