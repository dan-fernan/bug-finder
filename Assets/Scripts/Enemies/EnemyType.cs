using UnityEngine;

namespace BugDefense
{
    /// <summary>Plain data describing one enemy (bug) type. All values are tunable.</summary>
    [System.Serializable]
    public class EnemyType
    {
        public string name;
        public float hp;
        public float speed;             // units per second
        public float scale = 1f;
        public Color color;
        public float emission = 0.35f;
        public BugStyle style = BugStyle.Beetle; // body plan used by BugModel

        [Header("Specials")]
        public float growToScale;       // Memory Leak: grows to this scale over growDuration (0 = no growth)
        public float growDuration = 20f;
        public float hpPerSecond;       // Memory Leak: gains max/current HP over time
        public float speedJitter;       // Race Condition: +/- fraction of speed, re-rolled frequently
        public int splitCount;          // Boss: spawns this many children on death
        [System.NonSerialized] public EnemyType splitInto; // reference, not a serialized copy (avoids recursion)
    }
}
