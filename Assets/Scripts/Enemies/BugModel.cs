using System.Collections.Generic;
using UnityEngine;

namespace BugDefense
{
    /// <summary>Body plan per enemy type, so each bug reads differently at a glance.</summary>
    public enum BugStyle { Beetle, Ladybug, Blob, Skitter, Boss }

    /// <summary>
    /// Procedural insect built from primitives: abdomen + thorax + head, eyes, antennae, mandibles and
    /// six two-segment legs. Walks with an alternating tripod gait whose cadence follows the bug's speed,
    /// with body bob/sway and antenna wiggle. Local space: +Z forward, ground at y = -0.42.
    /// </summary>
    public class BugModel : MonoBehaviour
    {
        const float Ground = -0.42f;
        const float BodyDrop = -0.08f; // body sits low between the legs, like a real insect

        class Leg
        {
            public Transform hip;
            public float side, baseYaw, phaseOffset;
        }

        readonly List<Leg> legs = new List<Leg>();
        readonly List<Transform> antennae = new List<Transform>();
        Transform body, abdomen;
        Vector3 abdomenScale;
        BugStyle style;
        float phase, legSwing = 24f, legLift = 22f, stride = 0.55f, time;

        public static BugModel Build(Transform parent, EnemyType type)
        {
            var root = ShapeFactory.Empty("Visual", parent, Vector3.zero);
            var m = root.gameObject.AddComponent<BugModel>();
            m.style = type.style;
            m.phase = Random.value * Mathf.PI * 2f; // desync bugs walking side by side
            m.BuildParts(type);
            return m;
        }

        // ---------- Construction ----------

        /// <summary>A cylinder spanning <paramref name="from"/> to <paramref name="to"/> (limb segment).</summary>
        static Transform Segment(Transform parent, Vector3 from, Vector3 to, float thickness, Material mat, string name)
        {
            Vector3 d = to - from;
            var go = ShapeFactory.Prim(PrimitiveType.Cylinder, parent, (from + to) * 0.5f,
                new Vector3(thickness, d.magnitude * 0.5f, thickness), mat, false, name);
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
            return go.transform;
        }

        static Transform Ball(Transform parent, Vector3 pos, Vector3 scale, Material mat, string name)
            => ShapeFactory.Prim(PrimitiveType.Sphere, parent, pos, scale, mat, false, name).transform;

        void BuildParts(EnemyType type)
        {
            Color c = type.color;
            var shell = MaterialFactory.Lit(c, type.emission, 0.75f);                 // glossy carapace
            var dark = MaterialFactory.Lit(Color.Lerp(c, Color.black, 0.7f), 0f, 0.4f);
            var mid = MaterialFactory.Lit(Color.Lerp(c, Color.black, 0.35f), 0f, 0.5f);
            var eyeWhite = MaterialFactory.Lit(Color.white, 0.6f, 0.9f);
            var pupil = MaterialFactory.Unlit(new Color(0.03f, 0.03f, 0.05f));

            body = ShapeFactory.Empty("Body", transform, Vector3.zero);

            // Proportions per style.
            Vector3 abdScale = new Vector3(0.72f, 0.5f, 0.85f);
            float legLen = 1f, legThick = 0.07f;
            switch (style)
            {
                case BugStyle.Ladybug: abdScale = new Vector3(0.86f, 0.55f, 0.86f); break;
                case BugStyle.Blob: abdScale = new Vector3(0.95f, 0.75f, 1.0f); legLen = 0.8f; break;
                case BugStyle.Skitter: abdScale = new Vector3(0.48f, 0.36f, 0.9f); legLen = 1.35f; legThick = 0.05f;
                    legSwing = 32f; stride = 0.7f; break;
                case BugStyle.Boss: abdScale = new Vector3(0.85f, 0.6f, 0.95f); legThick = 0.09f; break;
            }

            // Abdomen (back), thorax (middle), head (front).
            abdomen = Ball(body, new Vector3(0f, 0.06f, -0.38f), abdScale, shell, "Abdomen");
            abdomenScale = abdScale;
            Ball(body, new Vector3(0f, 0.04f, 0.06f), new Vector3(0.46f, 0.36f, 0.4f), mid, "Thorax");
            var head = Ball(body, new Vector3(0f, 0.06f, 0.36f), new Vector3(0.36f, 0.3f, 0.32f), dark, "Head");

            // Segment bands across the abdomen (thin, slightly larger darker discs).
            if (style == BugStyle.Beetle || style == BugStyle.Skitter || style == BugStyle.Boss)
                for (int i = 0; i < 3; i++)
                    Ball(body, new Vector3(0f, 0.06f, -0.2f - i * 0.2f),
                        new Vector3(abdScale.x * (1.02f - i * 0.12f), abdScale.y * (1.02f - i * 0.12f), 0.05f), dark, "Band");

            // Ladybug: split wing covers with spots.
            if (style == BugStyle.Ladybug)
            {
                Segment(body, new Vector3(0f, 0.34f, -0.02f), new Vector3(0f, 0.34f, -0.75f), 0.035f, dark, "Seam");
                var spot = MaterialFactory.Lit(new Color(0.05f, 0.05f, 0.06f), 0f, 0.6f);
                Vector3[] spots = { new Vector3(-0.2f, 0.29f, -0.25f), new Vector3(0.2f, 0.29f, -0.25f),
                                    new Vector3(-0.24f, 0.24f, -0.52f), new Vector3(0.24f, 0.24f, -0.52f), new Vector3(0f, 0.3f, -0.62f) };
                foreach (var s in spots) Ball(body, s, new Vector3(0.15f, 0.06f, 0.15f), spot, "Spot");
            }

            // Memory Leak: glowing "data" blobs inside a bulging abdomen.
            if (style == BugStyle.Blob)
            {
                var glow = MaterialFactory.Lit(Color.Lerp(c, Color.white, 0.4f), 2f);
                Ball(body, new Vector3(0.18f, 0.32f, -0.3f), Vector3.one * 0.16f, glow, "Bubble");
                Ball(body, new Vector3(-0.15f, 0.28f, -0.55f), Vector3.one * 0.12f, glow, "Bubble");
                Ball(body, new Vector3(0.02f, 0.4f, -0.5f), Vector3.one * 0.1f, glow, "Bubble");
            }

            // Boss: horn and a crown of glowing error spikes.
            if (style == BugStyle.Boss)
            {
                var spike = MaterialFactory.Lit(new Color(1f, 0.2f, 0.2f), 2.5f);
                Segment(body, new Vector3(0f, 0.12f, 0.45f), new Vector3(0f, 0.42f, 0.7f), 0.08f, dark, "Horn");
                for (int i = 0; i < 5; i++)
                {
                    float z = -0.15f - i * 0.15f;
                    Segment(body, new Vector3(0f, 0.25f + abdScale.y * 0.35f, z), new Vector3(0f, 0.5f + abdScale.y * 0.35f, z - 0.08f), 0.07f, spike, "Spike");
                }
            }

            // Eyes: white with a pupil looking forward (glowing red for the boss).
            var eyeMat = style == BugStyle.Boss ? MaterialFactory.Lit(new Color(1f, 0.15f, 0.1f), 3f) : eyeWhite;
            for (int s = -1; s <= 1; s += 2)
            {
                Ball(body, new Vector3(s * 0.12f, 0.15f, 0.47f), Vector3.one * 0.13f, eyeMat, "Eye");
                if (style != BugStyle.Boss)
                    Ball(body, new Vector3(s * 0.13f, 0.16f, 0.53f), Vector3.one * 0.06f, pupil, "Pupil");
            }

            // Mandibles.
            for (int s = -1; s <= 1; s += 2)
            {
                Segment(body, new Vector3(s * 0.08f, -0.02f, 0.48f), new Vector3(s * 0.03f, -0.05f, 0.62f), 0.04f, dark, "Mandible");
            }

            // Antennae: pivot at the head so they can wiggle.
            for (int s = -1; s <= 1; s += 2)
            {
                var pivot = ShapeFactory.Empty("Antenna", head.parent, new Vector3(s * 0.08f, 0.18f, 0.42f));
                Vector3 tip = new Vector3(s * 0.16f, 0.32f, 0.28f);
                Segment(pivot, Vector3.zero, tip, 0.025f, dark, "AntennaStalk");
                Ball(pivot, tip, Vector3.one * 0.06f, style == BugStyle.Boss ? eyeMat : shell, "AntennaTip");
                antennae.Add(pivot);
            }

            // Six legs: hip pivot on the thorax, femur up/out to a knee, tibia down to the ground.
            // Tripod gait: front-left, middle-right, back-left move together, then the other three.
            float[] zs = { 0.2f, 0.02f, -0.18f };
            float[] yaws = { 35f, 0f, -35f }; // front legs reach forward, back legs reach back
            for (int s = -1; s <= 1; s += 2)
                for (int i = 0; i < 3; i++)
                {
                    var hip = ShapeFactory.Empty("Hip", body, new Vector3(s * 0.2f, -0.04f, zs[i]));
                    Vector3 knee = new Vector3(s * 0.36f * legLen, 0.08f, 0f);
                    Vector3 foot = new Vector3(s * 0.6f * legLen, Ground - BodyDrop + 0.04f, 0f); // hip-relative, lands on the ground
                    Segment(hip, Vector3.zero, knee, legThick, dark, "Femur");
                    Segment(hip, knee, foot, legThick * 0.8f, dark, "Tibia");
                    Ball(hip, knee, Vector3.one * legThick * 1.3f, mid, "Knee");
                    bool groupA = (s < 0) == (i != 1);
                    legs.Add(new Leg { hip = hip, side = s, baseYaw = -s * yaws[i], phaseOffset = groupA ? 0f : Mathf.PI });
                    hip.localRotation = Quaternion.Euler(0f, -s * yaws[i], 0f);
                }

            // Race Condition: speed streak.
            if (style == BugStyle.Skitter)
            {
                var trail = gameObject.AddComponent<TrailRenderer>();
                trail.time = 0.25f;
                trail.startWidth = 0.35f;
                trail.endWidth = 0f;
                trail.sharedMaterial = MaterialFactory.Unlit(c);
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                trail.minVertexDistance = 0.1f;
            }
        }

        // ---------- Animation ----------

        /// <summary>Advance the walk cycle. <paramref name="speed"/> is world units/s, <paramref name="scale"/> the bug's size.</summary>
        public void Tick(float speed, float scale, float dt)
        {
            if (dt <= 0f) return;
            time += dt;
            // One full step cycle per stride length travelled, so feet don't "skate".
            phase += speed * dt / Mathf.Max(0.1f, stride * scale) * Mathf.PI;

            foreach (var leg in legs)
            {
                float p = phase + leg.phaseOffset;
                float swing = Mathf.Sin(p) * legSwing;              // forward/back around the hip
                float lift = Mathf.Max(0f, Mathf.Cos(p)) * legLift;  // raise the foot while it swings forward
                leg.hip.localRotation = Quaternion.Euler(0f, leg.baseYaw - leg.side * swing, leg.side * lift);
            }

            // Body bobs twice per cycle (each tripod push) and rolls slightly side to side.
            float bob = Mathf.Abs(Mathf.Sin(phase)) * 0.05f;
            body.localPosition = new Vector3(0f, BodyDrop + bob, 0f);
            body.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(phase) * 4f);

            // Antennae wiggle.
            for (int i = 0; i < antennae.Count; i++)
                antennae[i].localRotation = Quaternion.Euler(Mathf.Sin(time * 7f + i * 1.7f) * 12f, Mathf.Sin(time * 5f + i) * 10f, 0f);

            // Memory Leak abdomen "breathes"; the boss pulses menacingly.
            if (style == BugStyle.Blob || style == BugStyle.Boss)
            {
                float k = 1f + Mathf.Sin(time * (style == BugStyle.Blob ? 3f : 2f)) * 0.06f;
                abdomen.localScale = abdomenScale * k;
            }
        }
    }
}
