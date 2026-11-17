using UnityEngine;

namespace BugDefense
{
    /// <summary>
    /// The playable levels, easiest first. Grid is 18 x 12 (x 0..17, z 0..11); spawns sit just off the
    /// left edge (x = -1) and PROD just off the right edge (x = 18).
    /// </summary>
    public static class LevelCatalog
    {
        public static readonly LevelDefinition[] All =
        {
            // One fork, one merge: bugs take the north or south branch around a central block.
            new LevelDefinition("hotfix/login", "One if/else. Cover both branches.")
                { hpScale = 1f, startingCapacity = 8 }
                .Spawn("S", -1, 5).Node("a", 4, 5)
                .Node("n1", 4, 9).Node("n2", 11, 9)
                .Node("s1", 4, 1).Node("s2", 11, 1)
                .Node("m", 11, 5).Node("t1", 14, 5).Node("t2", 14, 9).Prod("P", 18, 9)
                .Edge("S", "a")
                .Edge("a", "n1", "n2", "m")
                .Edge("a", "s1", "s2", "m")
                .Edge("m", "t1", "t2", "P"),

            // A long serpentine with two shortcuts: each loop can be skipped (if/else), 4 routes, 32-50 tiles.
            new LevelDefinition("feature/checkout", "Nested branches. Find the chokepoints.")
                { hpScale = 1.05f, startingCapacity = 9 }
                .Spawn("S", -1, 1).Node("p1", 3, 1).Node("f1", 3, 6).Node("p2", 3, 10).Node("p3", 8, 10)
                .Node("m1", 8, 6).Node("f2", 8, 4).Node("p4", 8, 1).Node("p5", 13, 1).Node("m2", 13, 4)
                .Node("p6", 13, 10).Prod("P", 18, 10)
                .Edge("S", "p1", "f1")
                .Edge("f1", "p2", "p3", "m1")     // long way round the top loop
                .Edge("f1", "m1")                 // shortcut
                .Edge("m1", "f2")
                .Edge("f2", "p4", "p5", "m2")     // long way round the bottom loop
                .Edge("f2", "m2")                 // shortcut
                .Edge("m2", "p6", "P"),

            // Two entry points (two teams pushing) merge into one core, which forks around a central block
            // and merges again before PROD: 4 routes, ~34 tiles each.
            new LevelDefinition("release/v2.0", "Two teams pushing at once. Good luck.")
                { hpScale = 1.1f, startingCapacity = 10 }
                .Spawn("S1", -1, 1).Node("a", 5, 1)
                .Spawn("S2", -1, 10).Node("b", 2, 10).Node("c", 2, 5)
                .Node("m", 5, 5).Node("n", 9, 5)
                .Node("u1", 9, 9).Node("u2", 13, 9).Node("d1", 9, 1).Node("d2", 13, 1)
                .Node("q", 13, 5).Node("s", 16, 5).Node("t", 16, 8).Prod("P", 18, 8)
                .Edge("S1", "a", "m")
                .Edge("S2", "b", "c", "m")
                .Edge("m", "n")
                .Edge("n", "u1", "u2", "q")
                .Edge("n", "d1", "d2", "q")
                .Edge("q", "s", "t", "P"),
        };

        /// <summary>Index of the level being played. Survives scene reloads (restart / next level).</summary>
        public static int CurrentIndex { get; private set; }
        /// <summary>False on a fresh Play: the level-select screen shows first.</summary>
        public static bool LevelChosen { get; private set; }

        public static LevelDefinition Current => All[Mathf.Clamp(CurrentIndex, 0, All.Length - 1)];
        public static bool HasNext => CurrentIndex < All.Length - 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { CurrentIndex = 0; LevelChosen = false; }

        public static void Choose(int index) { CurrentIndex = Mathf.Clamp(index, 0, All.Length - 1); LevelChosen = true; }
        public static void BackToMenu() { LevelChosen = false; }
    }
}
