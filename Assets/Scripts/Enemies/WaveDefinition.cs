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
        public List<SpawnGroup> groups = new List<SpawnGroup>();

        public WaveDefinition(string title, params SpawnGroup[] groups)
        {
            this.title = title;
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
                new WaveDefinition("Typos",
                    G(se, 8, 1.0f)),
                new WaveDefinition("It compiles, but...",
                    G(se, 6, 0.8f), G(logic, 4, 1.5f, 5.5f)),
                new WaveDefinition("Works on my machine",
                    G(logic, 8, 1.2f), G(race, 6, 0.6f, 10.5f)),
                new WaveDefinition("Heap of trouble",
                    G(leak, 3, 3f), G(se, 10, 0.5f, 8f)),
                new WaveDefinition("Friday deploy",
                    G(logic, 6, 2f), G(race, 4, 3f, 1f), G(leak, 3, 4f, 2f)),
                new WaveDefinition("PRODUCTION OUTAGE",
                    G(boss, 1, 1f), G(race, 8, 1.0f, 5f)),
            };
        }
    }
}
