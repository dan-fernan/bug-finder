using UnityEngine;

namespace BugDefense
{
    /// <summary>[Req A] Multiple enemy types with different speed and HP.</summary>
    public static class EnemyCatalog
    {
        public static readonly EnemyType SyntaxError = new EnemyType
        {
            name = "Syntax Error", hp = 20f, speed = 4.5f, scale = 0.6f,
            color = new Color32(0xEF, 0x44, 0x44, 0xFF),
        };

        public static readonly EnemyType LogicBug = new EnemyType
        {
            name = "Logic Bug", hp = 60f, speed = 2.6f, scale = 0.9f,
            color = new Color32(0xF9, 0x73, 0x16, 0xFF),
        };

        public static readonly EnemyType MemoryLeak = new EnemyType
        {
            name = "Memory Leak", hp = 220f, speed = 1.3f, scale = 1.0f,
            color = new Color32(0xA8, 0x55, 0xF7, 0xFF),
            growToScale = 1.6f, growDuration = 25f, hpPerSecond = 2f,
        };

        public static readonly EnemyType RaceCondition = new EnemyType
        {
            name = "Race Condition", hp = 12f, speed = 7.5f, scale = 0.5f,
            color = new Color32(0xEA, 0xB3, 0x08, 0xFF),
            speedJitter = 0.15f,
        };

        public static readonly EnemyType ProductionOutage = new EnemyType
        {
            name = "Production Outage", hp = 1500f, speed = 0.9f, scale = 2.2f,
            color = new Color32(0x7F, 0x0D, 0x16, 0xFF), emission = 0.5f,
            splitCount = 6, splitInto = SyntaxError,
        };

        public static readonly EnemyType[] All = { SyntaxError, LogicBug, MemoryLeak, RaceCondition, ProductionOutage };
    }
}
