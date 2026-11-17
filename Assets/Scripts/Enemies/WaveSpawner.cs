using System.Collections.Generic;
using UnityEngine;

namespace BugDefense
{
    /// <summary>Spawns the groups of the current wave at the "git push" pad. Waves are data (WaveDefinition).</summary>
    public class WaveSpawner : MonoBehaviour
    {
        [System.NonSerialized] public List<WaveDefinition> waves; // built from code (catalog references)

        void Awake() { waves = WaveDefinition.CreateDefault(); }

        public bool Running { get; private set; }
        /// <summary>Enemies of the current wave not yet spawned.</summary>
        public int PendingCount { get; private set; }
        public bool AllSpawned => !Running || PendingCount == 0;

        WaveDefinition current;
        float elapsed;
        int[] spawnedPerGroup;

        public void StartWave(int index)
        {
            current = waves[index];
            elapsed = 0f;
            spawnedPerGroup = new int[current.groups.Count];
            PendingCount = current.TotalCount;
            Running = true;
        }

        public void Stop() { Running = false; PendingCount = 0; }

        void Update()
        {
            if (!Running || current == null) return;
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Wave) return;

            elapsed += Time.deltaTime;
            var spawnPos = PathDefinition.WorldWaypoints[0];
            for (int g = 0; g < current.groups.Count; g++)
            {
                var group = current.groups[g];
                // Spawn every enemy of this group whose scheduled time has passed.
                while (spawnedPerGroup[g] < group.count &&
                       elapsed >= group.startTime + spawnedPerGroup[g] * group.interval)
                {
                    // The boss already has hand-tuned HP; the ramp applies to regular bugs only.
                    float mult = group.type.splitCount > 0 ? 1f : current.hpMultiplier;
                    Enemy.Spawn(transform, group.type, spawnPos, 1, mult);
                    spawnedPerGroup[g]++;
                    PendingCount--;
                }
            }
            if (PendingCount <= 0) Running = false;
        }
    }
}
