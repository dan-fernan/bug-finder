using UnityEngine;

namespace BugDefense
{
    /// <summary>
    /// A debugging-tool tower. Rotates its head (yaw) and barrel (pitch) toward its target and
    /// fires physics projectiles at a fixed rate.
    /// </summary>
    public class Tower : MonoBehaviour
    {
        [Header("Stats")]
        public string displayName = "Linter";
        public float fireRate = 2.5f;        // [Req C] shots per second
        public float projectileSpeed = 26f;
        public float height = 1.8f;          // fire point Y
        public float damage = 7f;
        public float range = 8f;
        public Color color = new Color32(0x38, 0xBD, 0xF8, 0xFF);

        [Header("Aiming")]
        public bool usePrediction = true;
        public float turnSpeed = 720f;       // deg/s
        public bool showPredictionGizmo = true;

        [Header("Parts")]
        public Transform head, barrel, firePoint;

        Enemy target;
        Vector3 aimPoint;
        float cooldown, retargetTimer;
        Collider[] ownColliders;

        public Enemy Target => target;

        public static Tower Create(BuildTile tile)
        {
            var go = new GameObject("Tower");
            go.transform.SetParent(tile.transform, false); // child of tile: hovering it still resolves the tile
            go.transform.localPosition = Vector3.zero;
            var t = go.AddComponent<Tower>();
            t.BuildVisual();
            tile.occupant = t;
            return t;
        }

        void BuildVisual()
        {
            var baseMat = MaterialFactory.Lit(new Color(0.16f, 0.19f, 0.25f), 0f, 0.4f);
            var tint = MaterialFactory.Lit(color, 0.25f, 0.5f);
            var glow = MaterialFactory.Lit(color, 2f);

            float columnH = Mathf.Max(0.3f, height - 0.3f);
            // Unity cylinder is 2 units tall at scale 1, so scale.y = height/2.
            ShapeFactory.Prim(PrimitiveType.Cylinder, transform, new Vector3(0f, columnH * 0.5f, 0f),
                new Vector3(1.1f, columnH * 0.5f, 1.1f), baseMat, true, "Column");
            ShapeFactory.Prim(PrimitiveType.Cylinder, transform, new Vector3(0f, columnH - 0.05f, 0f),
                new Vector3(1.2f, 0.06f, 1.2f), glow, false, "Band");

            head = ShapeFactory.Empty("Head", transform, new Vector3(0f, height, 0f));
            ShapeFactory.Prim(PrimitiveType.Cube, head, Vector3.zero, new Vector3(0.75f, 0.5f, 0.75f), tint, false, "HeadMesh");

            barrel = ShapeFactory.Empty("Barrel", head, Vector3.zero);
            var b = ShapeFactory.Prim(PrimitiveType.Cylinder, barrel, new Vector3(0f, 0f, 0.5f),
                new Vector3(0.2f, 0.4f, 0.2f), baseMat, false, "BarrelMesh");
            b.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ShapeFactory.Prim(PrimitiveType.Cylinder, barrel, new Vector3(0f, 0f, 0.9f),
                new Vector3(0.24f, 0.03f, 0.24f), glow, false, "Muzzle").transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            firePoint = ShapeFactory.Empty("FirePoint", barrel, new Vector3(0f, 0f, 1f));

            ownColliders = GetComponentsInChildren<Collider>();
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm != null && !gm.IsPlaying) return;

            retargetTimer -= Time.deltaTime;
            if (retargetTimer <= 0f || target == null || !InRange(target))
            {
                retargetTimer = 0.2f;
                target = FindTarget();
            }

            if (target != null)
            {
                // [Req C] Aim at the enemy. [Req A] Lead it based on its velocity.
                aimPoint = usePrediction
                    ? AimPredictor.PredictInterceptPoint(firePoint.position, target.AimPosition, target.Velocity, projectileSpeed)
                    : target.AimPosition;
                AimAt(aimPoint);
            }

            // [Req C] Fire continuously at a set rate while there is a target.
            cooldown -= Time.deltaTime;
            if (cooldown <= 0f && target != null)
            {
                Fire();
                cooldown = 1f / fireRate;
            }
        }

        bool InRange(Enemy e)
        {
            var d = e.transform.position - transform.position;
            d.y = 0f;
            return d.sqrMagnitude <= range * range;
        }

        /// <summary>"First" targeting: the in-range bug furthest along the path (closest to PROD).</summary>
        Enemy FindTarget()
        {
            Enemy best = null;
            float bestProgress = float.MinValue;
            var all = EnemyRegistry.All;
            for (int i = 0; i < all.Count; i++)
            {
                var e = all[i];
                if (e == null || !InRange(e)) continue;
                float p = e.PathProgress;
                if (p > bestProgress) { bestProgress = p; best = e; }
            }
            return best;
        }

        void AimAt(Vector3 point)
        {
            Vector3 toYaw = point - head.position;
            toYaw.y = 0f;
            if (toYaw.sqrMagnitude > 0.0001f)
                head.rotation = Quaternion.RotateTowards(head.rotation, Quaternion.LookRotation(toYaw), turnSpeed * Time.deltaTime);

            Vector3 to = point - barrel.position;
            float flat = new Vector2(to.x, to.z).magnitude;
            float pitch = -Mathf.Atan2(to.y, flat) * Mathf.Rad2Deg; // positive X rotation tilts the barrel down
            barrel.localRotation = Quaternion.RotateTowards(barrel.localRotation, Quaternion.Euler(pitch, 0f, 0f), turnSpeed * Time.deltaTime);
        }

        void Fire()
        {
            Vector3 dir = (aimPoint - firePoint.position).normalized;
            var parent = GameManager.Instance != null ? GameManager.Instance.transform : null;
            Projectile.Spawn(parent, firePoint.position, dir * projectileSpeed, damage, 0.22f, color, ownColliders);
        }

        void OnDrawGizmos()
        {
            if (!showPredictionGizmo || target == null) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(aimPoint, 0.3f);
            Gizmos.DrawLine(firePoint != null ? firePoint.position : transform.position, aimPoint);
        }

#if UNITY_EDITOR
        [ContextMenu("Test AimPredictor")]
        void TestAimPredictor()
        {
            // Shooter at origin, target at (10,0,0) moving (0,0,5), projectile speed 20.
            Vector3 aim = AimPredictor.PredictInterceptPoint(Vector3.zero, new Vector3(10, 0, 0), new Vector3(0, 0, 5), 20f);
            float t = aim.z / 5f;
            Debug.Log($"Aim {aim}, target time {t:0.000}s, projectile time {aim.magnitude / 20f:0.000}s (should match)");
        }
#endif
    }
}
