using System.Collections.Generic;

namespace BugDefense
{
    /// <summary>
    /// One wave as data: groups of enemies. Each group starts at <see cref="SpawnGroup.startTime"/> seconds
    /// after the wave begins, and groups run in parallel, so they can be sequential or interleaved.
    /// </summary>
    [System.Serializable]
    public class WaveDefinition
    {
        [System.Serializable]
        public class SpawnGroup
        {
            public EnemyType type;
            public int count;
            public float interval;
            public float startTime;

            public SpawnGroup(EnemyType type, int count, float interval, float startTime = 0f)
            {
                this.type = type; this.count = count; this.interval = interval; this.startTime = startTime;
            }
        }

        public string title;
        /// <summary>Difficulty ramp: every bug in this wave spawns with base HP times this.</summary>
        public float hpMultiplier = 1f;
        public List<SpawnGroup> groups = new List<SpawnGroup>();

        public WaveDefinition(string title, float hpMultiplier, params SpawnGroup[] groups)
        {
            this.title = title;
            this.hpMultiplier = hpMultiplier;
            this.groups.AddRange(groups);
        }

        public int TotalCount
        {
            get { int n = 0; foreach (var g in groups) n += g.count; return n; }
        }

        static SpawnGroup G(EnemyType t, int count, float interval, float start = 0f) => new SpawnGroup(t, count, interval, start);

        /// <summary>The six waves of the game. Wave 6 is the boss wave.</summary>
        public static List<WaveDefinition> CreateDefault()
        {
            var se = EnemyCatalog.SyntaxError;
            var logic = EnemyCatalog.LogicBug;
            var leak = EnemyCatalog.MemoryLeak;
            var race = EnemyCatalog.RaceCondition;
            var boss = EnemyCatalog.ProductionOutage;
            return new List<WaveDefinition>
            {
                new WaveDefinition("Typos", 1.0f,
                    G(se, 12, 0.8f)),
                new WaveDefinition("It compiles, but...", 1.1f,
                    G(se, 10, 0.6f), G(logic, 7, 1.1f, 4f)),
                new WaveDefinition("Works on my machine", 1.25f,
                    G(logic, 10, 0.9f), G(race, 12, 0.4f, 7f)),
                new WaveDefinition("Heap of trouble", 1.3f,
                    G(leak, 4, 2.5f), G(se, 18, 0.35f, 5f), G(race, 8, 0.45f, 11f)),
                new WaveDefinition("Friday deploy", 1.6f,
                    G(logic, 12, 1.1f), G(race, 12, 0.9f, 1f), G(leak, 6, 2.5f, 2f)),
                new WaveDefinition("PRODUCTION OUTAGE", 1.6f,
                    G(boss, 1, 1f), G(leak, 3, 3.5f, 3f), G(race, 16, 0.6f, 5f), G(logic, 10, 1.0f, 10f)),
            };
        }
    }
}
