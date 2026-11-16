using UnityEngine;

namespace BugDefense
{
    /// <summary>
    /// Phase 1 spawner: after a short grace period, releases one bug at a steady interval,
    /// slowly getting tougher. (Replaced by data-driven waves in a later phase.)
    /// </summary>
    public class WaveSpawner : MonoBehaviour
    {
        public float initialDelay = 8f;
        public float spawnInterval = 1.2f;
        public float baseHp = 20f;
        public float hpGrowthPerSpawn = 0.6f;
        public float speed = 4.5f;

        public int Spawned { get; private set; }
        public float TimeUntilFirst => Mathf.Max(0f, initialDelay - elapsed);

        static readonly Color SyntaxRed = new Color32(0xEF, 0x44, 0x44, 0xFF);

        float elapsed, timer;

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.IsPlaying) return;

            elapsed += Time.deltaTime;
            if (elapsed < initialDelay) return;

            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = spawnInterval;

            var wp = PathDefinition.WorldWaypoints;
            Enemy.Spawn(transform, wp[0], 1, "Syntax Error", baseHp + Spawned * hpGrowthPerSpawn, speed, 0.7f, SyntaxRed);
            Spawned++;
        }
    }
}
