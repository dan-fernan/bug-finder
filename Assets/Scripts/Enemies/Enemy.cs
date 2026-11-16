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

        public string DisplayName => type != null ? type.name : "Bug";

        /// <summary>Current velocity (direction * current speed). Used for predictive aiming.</summary>
        public Vector3 Velocity { get; private set; }

        /// <summary>Point towers should aim at (center of the body collider).</summary>
        public Vector3 AimPosition => transform.position;

        /// <summary>How far along the path this bug is (waypoint index + fraction). Higher = closer to PROD.</summary>
        public float PathProgress
        {
            get
            {
                var wp = PathDefinition.WorldWaypoints;
                if (waypointIndex <= 0 || waypointIndex >= wp.Length) return waypointIndex;
                Vector3 a = wp[waypointIndex - 1], b = wp[waypointIndex];
                float seg = Vector3.Distance(a, b);
                float remaining = Vector3.Distance(Flat(transform.position), Flat(b));
                return waypointIndex - (seg > 0.001f ? Mathf.Clamp01(remaining / seg) : 0f);
            }
        }

        Rigidbody rb;
        Transform visual;
        float scale, age, jitterTimer, currentSpeed, bobPhase;
        bool dead;

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
        float YOffset => 0.42f * scale; // keeps the legs on the ground at any size

        /// <summary>Spawns an enemy standing at <paramref name="groundPos"/>, heading to waypoint <paramref name="waypointIndex"/>.</summary>
        public static Enemy Spawn(Transform parent, EnemyType type, Vector3 groundPos, int waypointIndex)
        {
            var go = new GameObject("Bug_" + type.name);
            go.transform.SetParent(parent, false);

            var e = go.AddComponent<Enemy>();
            e.type = type;
            e.maxHp = e.hp = type.hp;
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
            HealthBar.Create(e);

            // Face along the current segment immediately.
            var wp = PathDefinition.WorldWaypoints;
            if (waypointIndex < wp.Length)
            {
                var d = Flat(wp[waypointIndex] - groundPos);
                if (d.sqrMagnitude > 0.0001f) go.transform.rotation = Quaternion.LookRotation(d);
            }
            e.bobPhase = Random.value * 10f;
            return e;
        }

        /// <summary>Primitive bug: flattened body, head, 6 legs, two eyes.</summary>
        void BuildVisual()
        {
            visual = ShapeFactory.Empty("Visual", transform, Vector3.zero);
            var body = MaterialFactory.Lit(type.color, type.emission, 0.5f);
            var dark = MaterialFactory.Lit(type.color * 0.45f, 0f, 0.2f);
            var eye = MaterialFactory.Unlit(Color.white);

            ShapeFactory.Prim(PrimitiveType.Sphere, visual, new Vector3(0f, 0.05f, -0.1f), new Vector3(0.85f, 0.5f, 1.05f), body, false, "Body");
            ShapeFactory.Prim(PrimitiveType.Sphere, visual, new Vector3(0f, 0.08f, 0.5f), new Vector3(0.48f, 0.4f, 0.45f), dark, false, "Head");
            ShapeFactory.Prim(PrimitiveType.Sphere, visual, new Vector3(-0.12f, 0.2f, 0.68f), Vector3.one * 0.12f, eye, false, "EyeL");
            ShapeFactory.Prim(PrimitiveType.Sphere, visual, new Vector3(0.12f, 0.2f, 0.68f), Vector3.one * 0.12f, eye, false, "EyeR");

            for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < 3; i++)
            {
                var leg = ShapeFactory.Prim(PrimitiveType.Cylinder, visual,
                    new Vector3(side * 0.48f, -0.2f, -0.4f + i * 0.3f), new Vector3(0.06f, 0.25f, 0.06f), dark, false, "Leg");
                leg.transform.localRotation = Quaternion.Euler(0f, 0f, side * 55f);
            }

            if (type.splitCount > 0) // boss: a crown of "error" spikes so it reads as special
            {
                var spike = MaterialFactory.Lit(new Color(1f, 0.2f, 0.2f), 2.5f);
                for (int i = 0; i < 5; i++)
                {
                    float a = i * Mathf.PI * 2f / 5f;
                    var s = ShapeFactory.Prim(PrimitiveType.Cube, visual, new Vector3(Mathf.Cos(a) * 0.25f, 0.32f, -0.1f + Mathf.Sin(a) * 0.3f),
                        new Vector3(0.08f, 0.22f, 0.08f), spike, false, "Spike");
                    s.transform.localRotation = Quaternion.Euler(Mathf.Sin(a) * 25f, 0f, -Mathf.Cos(a) * 25f);
                }
            }
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

            var wp = PathDefinition.WorldWaypoints;
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
                maxHp += type.hpPerSecond * dt;
                hp += type.hpPerSecond * dt;
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

        void Update()
        {
            if (visual == null) return;
            bobPhase += Time.deltaTime * currentSpeed * 4f / Mathf.Max(0.5f, scale);
            visual.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(bobPhase)) * 0.08f, 0f);
        }

        public void TakeDamage(float dmg)
        {
            if (dead) return;
            hp -= dmg;
            if (hp <= 0f) Die();
        }

        void Die()
        {
            if (dead) return;
            dead = true;
            EnemyRegistry.Unregister(this);
            var gm = GameManager.Instance;
            if (gm != null) gm.RegisterBugSquashed();

            // Boss: splits into smaller bugs that continue from the boss's spot on the path.
            if (type.splitCount > 0 && type.splitInto != null)
            {
                Vector3 back = -Flat(Velocity).normalized;
                if (back.sqrMagnitude < 0.01f) back = -Flat(transform.forward).normalized;
                for (int i = 0; i < type.splitCount; i++)
                {
                    // Stagger them slightly behind the boss so they don't overlap.
                    Vector3 p = Flat(transform.position) + back * (i * 0.45f);
                    Enemy.Spawn(transform.parent, type.splitInto, p, waypointIndex);
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
