using System;
using System.Collections.Generic;
using UnityEngine;

namespace BugDefense
{
    public enum SfxId { ShootLight, ShootMid, ShootHeavy, ShootMortar, Hit, Splash, Death, Place, Error, WaveStart, GameOver, Victory }

    /// <summary>
    /// [A+] Sound effects synthesized in code (no audio assets needed). One 2D AudioSource plays one-shots;
    /// each sound is rate-limited so a row of Linters doesn't turn into noise. M toggles mute.
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        const int SampleRate = 44100;
        static Sfx instance;
        static readonly Dictionary<SfxId, AudioClip> clips = new Dictionary<SfxId, AudioClip>();
        static readonly Dictionary<SfxId, float> lastPlayed = new Dictionary<SfxId, float>();
        public static bool Muted { get; private set; }

        AudioSource source;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            instance = null;
            clips.Clear();
            lastPlayed.Clear();
            Muted = false;
        }

        public static Sfx Create(Transform parent)
        {
            var go = new GameObject("Sfx");
            go.transform.SetParent(parent, false);
            var s = go.AddComponent<Sfx>();
            s.source = go.AddComponent<AudioSource>();
            s.source.playOnAwake = false;
            s.source.spatialBlend = 0f;
            s.source.ignoreListenerPause = true;
            instance = s;
            if (clips.Count == 0) BuildClips();
            AudioListener.volume = Muted ? 0f : 1f; // mute survives restarts, resets when Play mode restarts

            // The scene camera normally has a listener; make sure one exists so sound works anywhere.
            if (FindAnyObjectByType<AudioListener>() == null && Camera.main != null)
                Camera.main.gameObject.AddComponent<AudioListener>();
            return s;
        }

        void OnDestroy() { if (instance == this) instance = null; }

        public static void ToggleMute()
        {
            Muted = !Muted;
            AudioListener.volume = Muted ? 0f : 1f;
        }

        public static void Play(SfxId id, float volume = 1f, float minInterval = 0.05f)
        {
            if (instance == null || instance.source == null || Muted) return;
            if (!clips.TryGetValue(id, out var clip)) return;
            float now = Time.unscaledTime;
            if (lastPlayed.TryGetValue(id, out var last) && now - last < minInterval) return;
            lastPlayed[id] = now;
            instance.source.PlayOneShot(clip, volume);
        }

        // ---------- Synthesis ----------

        static float Square(float phase) => Mathf.Repeat(phase, 1f) < 0.5f ? 1f : -1f;
        static float Sine(float phase) => Mathf.Sin(phase * Mathf.PI * 2f);
        static readonly System.Random rng = new System.Random(1234);
        static float Noise() => (float)(rng.NextDouble() * 2.0 - 1.0);

        /// <summary>Builds a clip from f(t, phaseAccumulator) with a linear attack and exponential decay.</summary>
        static AudioClip Make(string name, float duration, float gain, Func<float, float> freqAt, Func<float, float, float> wave, float decay = 6f)
        {
            int n = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                phase += freqAt(t) / SampleRate;
                float env = Mathf.Min(1f, t / 0.004f) * Mathf.Exp(-decay * t / duration);
                data[i] = wave(phase, t) * env * gain;
            }
            var clip = AudioClip.Create(name, n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static void BuildClips()
        {
            clips[SfxId.ShootLight] = Make("shoot_light", 0.05f, 0.12f, t => 1500f - 6000f * t, (p, t) => Square(p));
            clips[SfxId.ShootMid] = Make("shoot_mid", 0.08f, 0.16f, t => 900f - 3000f * t, (p, t) => Square(p) * 0.6f + Sine(p) * 0.4f);
            clips[SfxId.ShootHeavy] = Make("shoot_heavy", 0.18f, 0.3f, t => 260f - 600f * t, (p, t) => Sine(p) * 0.7f + Noise() * 0.3f);
            clips[SfxId.ShootMortar] = Make("shoot_mortar", 0.22f, 0.35f, t => 140f - 250f * t, (p, t) => Sine(p) * 0.8f + Noise() * 0.2f);
            clips[SfxId.Hit] = Make("hit", 0.05f, 0.12f, t => 0f, (p, t) => Noise(), 8f);
            clips[SfxId.Splash] = Make("splash", 0.35f, 0.35f, t => 70f, (p, t) => Sine(p) * 0.5f + Noise() * 0.5f, 5f);
            clips[SfxId.Death] = Make("death", 0.16f, 0.2f, t => 1100f - 5000f * t, (p, t) => Square(p) * 0.5f + Sine(p) * 0.5f, 3f);
            clips[SfxId.Place] = Make("place", 0.14f, 0.2f, t => t < 0.06f ? 660f : 990f, (p, t) => Sine(p), 2.5f);
            clips[SfxId.Error] = Make("error", 0.18f, 0.18f, t => 140f, (p, t) => Square(p), 1.5f);
            clips[SfxId.WaveStart] = Make("wave", 0.36f, 0.2f, t => t < 0.12f ? 523f : t < 0.24f ? 659f : 784f, (p, t) => Square(p) * 0.4f + Sine(p) * 0.6f, 1.5f);
            clips[SfxId.GameOver] = Make("gameover", 1.2f, 0.25f, t => (Mathf.Repeat(t, 0.3f) < 0.15f) ? 880f : 620f, (p, t) => Square(p) * 0.5f + Sine(p) * 0.5f, 1.5f);
            clips[SfxId.Victory] = Make("victory", 0.9f, 0.22f, t => t < 0.15f ? 523f : t < 0.3f ? 659f : t < 0.45f ? 784f : 1047f, (p, t) => Sine(p) * 0.7f + Square(p) * 0.3f, 1.2f);
        }
    }
}
