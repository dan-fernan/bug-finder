using UnityEngine;

namespace BugDefense
{
    /// <summary>
    /// [Req C] A bug that walks the predetermined path toward PROD.
    /// Moved as a kinematic Rigidbody so physics projectiles collide with it properly.
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        public string DisplayName = "Syntax Error";
        public float maxHp = 20f;
        public float hp;
        public float speed = 4.5f;
        public int waypointIndex = 1;

        /// <summary>Current velocity (direction * speed). Used for predictive aiming.</summary>
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
        float yOffset;
        float bobPhase;
        bool dead;

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        public static Enemy Spawn(Transform parent, Vector3 groundPos, int waypointIndex,
                                  string name, float hp, float speed, float scale, Color color)
        {
            var go = new GameObject("Bug_" + name);
            go.transform.SetParent(parent, false);

            var e = go.AddComponent<Enemy>();
            e.DisplayName = name;
            e.maxHp = e.hp = hp;
            e.speed = speed;
            e.waypointIndex = waypointIndex;
            e.yOffset = 0.42f * scale;
            go.transform.position = groundPos + Vector3.up * e.yOffset;
            go.transform.localScale = Vector3.one * scale;

            var col = go.AddComponent<SphereCollider>();
            col.radius = 0.55f;

            e.rb = go.AddComponent<Rigidbody>();
            e.rb.isKinematic = true;
            e.rb.useGravity = false;
            e.rb.interpolation = RigidbodyInterpolation.Interpolate;

            e.BuildVisual(color);

            // Face along the first segment immediately.
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
        void BuildVisual(Color color)
        {
            visual = ShapeFactory.Empty("Visual", transform, Vector3.zero);
            var body = MaterialFactory.Lit(color, 0.35f, 0.5f);
            var dark = MaterialFactory.Lit(color * 0.45f, 0f, 0.2f);
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
        }

        void OnEnable() { EnemyRegistry.Register(this); }
        void OnDisable() { EnemyRegistry.Unregister(this); }

        void FixedUpdate()
        {
            if (dead || rb == null) return;
            var gm = GameManager.Instance;
            if (gm != null && !gm.IsPlaying) { Velocity = Vector3.zero; return; }

            var wp = PathDefinition.WorldWaypoints;
            Vector3 pos = rb.position;
            float step = speed * Time.fixedDeltaTime;
            Vector3 dir = Velocity.sqrMagnitude > 0f ? Velocity.normalized : transform.forward;

            // Walk, carrying leftover distance across waypoints so speed stays exact at corners.
            while (step > 0f && waypointIndex < wp.Length)
            {
                Vector3 target = wp[waypointIndex] + Vector3.up * yOffset;
                Vector3 to = target - pos;
                float dist = to.magnitude;
                if (dist > 0.0001f) dir = to / dist;
                if (dist > step) { pos += dir * step; step = 0f; }
                else { pos = target; step -= dist; waypointIndex++; }
            }

            rb.MovePosition(pos);
            Velocity = dir * speed; // direction * speed is more stable than finite differences
            var flat = Flat(dir);
            if (flat.sqrMagnitude > 0.0001f)
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.LookRotation(flat), 12f * Time.fixedDeltaTime));

            if (waypointIndex >= wp.Length) ReachedEnd();
        }

        void Update()
        {
            if (visual == null) return;
            bobPhase += Time.deltaTime * speed * 4f;
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
            if (GameManager.Instance != null) GameManager.Instance.RegisterBugSquashed();
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
