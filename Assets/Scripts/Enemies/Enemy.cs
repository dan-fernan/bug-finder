using UnityEngine;

namespace BugDefense
{
    /// <summary>
    /// [Req C] A bug that walks the predetermined path toward PROD.
    /// Moved as a kinematic Rigidbody so physics projectiles collide with it properly.
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        public EnemyType type;
        public float maxHp;
        public float hp;
        public float speed;
        public int waypointIndex = 1;
        public float hpMultiplier = 1f; // wave difficulty ramp (inherited by boss split children)

        public string DisplayName => type != null ? type.name : "Bug";

        /// <summary>Current velocity (direction * current speed). Used for predictive aiming.</summary>
        public Vector3 Velocity { get; private set; }

        /// <summary>Point towers should aim at (center of the body collider).</summary>
        public Vector3 AimPosition => transform.position;

        /// <summary>The branch-specific route this bug is walking (chosen at spawn).</summary>
        public PathRoute Route { get; private set; }

        /// <summary>Path distance left to PROD. Lower = more dangerous. Comparable across branches.</summary>
        public float RemainingDistance
        {
            get
            {
                var wp = Route.points;
                if (waypointIndex >= wp.Length) return 0f;
                return Vector3.Distance(Flat(transform.position), Flat(wp[waypointIndex])) + Route.remainingAfter[waypointIndex];
            }
        }

        Rigidbody rb;
        Transform visual;
        BugModel bugModel;
        MeshRenderer[] bodyRenderers;
        Material[] bodyMaterials;
        float flashTimer;
        float scale, age, jitterTimer, currentSpeed;
        bool dead;

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
        float YOffset => 0.42f * scale; // keeps the legs on the ground at any size

        /// <summary>Spawns an enemy standing at <paramref name="groundPos"/>, heading to waypoint <paramref name="waypointIndex"/> of <paramref name="route"/>.</summary>
        public static Enemy Spawn(Transform parent, EnemyType type, PathRoute route, Vector3 groundPos, int waypointIndex, float hpMultiplier = 1f)
        {
            var go = new GameObject("Bug_" + type.name);
            go.transform.SetParent(parent, false);

            var e = go.AddComponent<Enemy>();
            e.type = type;
            e.Route = route;
            e.hpMultiplier = hpMultiplier;
            e.maxHp = e.hp = type.hp * hpMultiplier;
            e.speed = e.currentSpeed = type.speed;
            e.waypointIndex = waypointIndex;
            e.scale = type.scale;
            go.transform.position = new Vector3(groundPos.x, e.YOffset, groundPos.z);
            go.transform.localScale = Vector3.one * e.scale;

            var col = go.AddComponent<SphereCollider>();
            col.radius = 0.55f;

            e.rb = go.AddComponent<Rigidbody>();
            e.rb.isKinematic = true;
            e.rb.useGravity = false;
            e.rb.interpolation = RigidbodyInterpolation.Interpolate;

            e.BuildVisual();
            e.bodyRenderers = e.visual.GetComponentsInChildren<MeshRenderer>();
            e.bodyMaterials = new Material[e.bodyRenderers.Length];
            for (int i = 0; i < e.bodyRenderers.Length; i++) e.bodyMaterials[i] = e.bodyRenderers[i].sharedMaterial;
            HealthBar.Create(e);

            // Face along the current segment immediately.
            var wp = route.points;
            if (waypointIndex < wp.Length)
            {
                var d = Flat(wp[waypointIndex] - groundPos);
                if (d.sqrMagnitude > 0.0001f) go.transform.rotation = Quaternion.LookRotation(d);
            }
            return e;
        }

        /// <summary>Procedural insect with a walking animation (see BugModel).</summary>
        void BuildVisual()
        {
            bugModel = BugModel.Build(transform, type);
            visual = bugModel.transform;
        }

        void OnEnable() { EnemyRegistry.Register(this); }
        void OnDisable() { EnemyRegistry.Unregister(this); }

        void FixedUpdate()
        {
            if (dead || rb == null) return;
            var gm = GameManager.Instance;
            if (gm != null && !gm.IsPlaying) { Velocity = Vector3.zero; return; }

            float dt = Time.fixedDeltaTime;
            UpdateSpecials(dt);

            var wp = Route.points;
            Vector3 pos = rb.position;
            pos.y = YOffset;
            float step = currentSpeed * dt;
            Vector3 dir = Velocity.sqrMagnitude > 0f ? Velocity.normalized : transform.forward;

            // Walk, carrying leftover distance across waypoints so speed stays exact at corners.
            while (step > 0f && waypointIndex < wp.Length)
            {
                Vector3 target = new Vector3(wp[waypointIndex].x, YOffset, wp[waypointIndex].z);
                Vector3 to = target - pos;
                float dist = to.magnitude;
                if (dist > 0.0001f) dir = to / dist;
                if (dist > step) { pos += dir * step; step = 0f; }
                else { pos = target; step -= dist; waypointIndex++; }
            }

            rb.MovePosition(pos);
            Velocity = dir * currentSpeed; // direction * speed is more stable than finite differences
            var flat = Flat(dir);
            if (flat.sqrMagnitude > 0.0001f)
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.LookRotation(flat), 12f * dt));

            if (waypointIndex >= wp.Length) ReachedEnd();
        }

        void UpdateSpecials(float dt)
        {
            age += dt;

            // Memory Leak: grows bigger and tankier the longer it lives.
            if (type.growToScale > 0f)
            {
                scale = Mathf.Lerp(type.scale, type.growToScale, age / Mathf.Max(0.01f, type.growDuration));
                transform.localScale = Vector3.one * scale;
            }
            if (type.hpPerSecond > 0f)
            {
                maxHp += type.hpPerSecond * hpMultiplier * dt;
                hp += type.hpPerSecond * hpMultiplier * dt;
            }

            // Race Condition: speed is nondeterministic.
            currentSpeed = speed;
            if (type.speedJitter > 0f)
            {
                jitterTimer -= dt;
                if (jitterTimer <= 0f)
                {
                    jitterTimer = 0.35f;
                    jitterFactor = 1f + Random.Range(-type.speedJitter, type.speedJitter);
                }
                currentSpeed = speed * jitterFactor;
            }
        }
        float jitterFactor = 1f;

        /// <summary>
        /// [A+] Where this bug will be in <paramref name="t"/> seconds if it keeps walking its path at its current speed.
        /// Unlike pos + velocity * t, this turns the corners. Used by path-aware aiming (P).
        /// </summary>
        public Vector3 PredictPosition(float t)
        {
            var wp = Route.points;
            Vector3 pos = transform.position;
            float y = pos.y;
            float remaining = currentSpeed * Mathf.Max(0f, t);
            for (int i = waypointIndex; i < wp.Length && remaining > 0f; i++)
            {
                Vector3 target = new Vector3(wp[i].x, y, wp[i].z);
                float d = Vector3.Distance(pos, target);
                if (d >= remaining) return pos + (target - pos) / d * remaining;
                pos = target;
                remaining -= d;
            }
            return pos;
        }

        void Update()
        {
            if (visual == null) return;
            if (flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                if (flashTimer <= 0f) SetFlash(false);
            }
            bugModel.Tick(currentSpeed, scale, Time.deltaTime);
        }

        public void TakeDamage(float dmg)
        {
            if (dead) return;
            hp -= dmg;
            if (hp <= 0f) { Die(); return; }
            // [A+] Brief white flash so hits read clearly.
            if (flashTimer <= 0f) SetFlash(true);
            flashTimer = 0.06f;
        }

        void SetFlash(bool on)
        {
            if (bodyRenderers == null) return;
            var white = MaterialFactory.Unlit(Color.white);
            for (int i = 0; i < bodyRenderers.Length; i++)
                if (bodyRenderers[i] != null) bodyRenderers[i].sharedMaterial = on ? white : bodyMaterials[i];
        }

        void Die()
        {
            if (dead) return;
            dead = true;
            EnemyRegistry.Unregister(this);
            var gm = GameManager.Instance;
            if (gm != null) gm.RegisterBugSquashed();

            // [A+] Death FX: burst in the bug's color + floating "fixed!" text.
            Vector3 p0 = transform.position;
            HitFx.Burst(p0, type.color, 8 + Mathf.RoundToInt(scale * 6f), 0.12f + scale * 0.1f, 4f + scale);
            FloatingText.Spawn(type.splitCount > 0 ? "OUTAGE RESOLVED!" : "fixed!", p0 + Vector3.up * (0.6f + scale * 0.5f),
                               new Color32(0x1F, 0xD1, 0x7A, 0xFF), type.splitCount > 0 ? 0.14f : 0.08f);
            Sfx.Play(SfxId.Death, 0.7f, 0.04f);

            // Boss: splits into smaller bugs that continue from the boss's spot on the path.
            if (type.splitCount > 0 && type.splitInto != null)
            {
                Vector3 back = -Flat(Velocity).normalized;
                if (back.sqrMagnitude < 0.01f) back = -Flat(transform.forward).normalized;
                for (int i = 0; i < type.splitCount; i++)
                {
                    // Stagger them slightly behind the boss so they don't overlap.
                    Vector3 p = Flat(transform.position) + back * (i * 0.45f);
                    Enemy.Spawn(transform.parent, type.splitInto, Route, p, waypointIndex, hpMultiplier);
                }
                if (gm != null && gm.Hud != null) gm.Hud.Toast("The outage split into Syntax Errors!", true);
            }
            Destroy(gameObject);
        }

        /// <summary>[Req C] Reaching PROD ends the game immediately.</summary>
        void ReachedEnd()
        {
            if (dead) return;
            Velocity = Vector3.zero;
            if (GameManager.Instance != null) GameManager.Instance.GameOver(this);
        }
    }
}
