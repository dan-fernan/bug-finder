using UnityEngine;

namespace BugDefense
{
    /// <summary>
    /// A debugging-tool tower. Rotates its head (yaw) and barrel (pitch) toward the predicted
    /// intercept point and fires physics projectiles at its type's fire rate.
    /// </summary>
    public class Tower : MonoBehaviour
    {
        public TowerType type;

        [Header("Aiming")]
        public bool usePrediction = true;
        public float turnSpeed = 720f; // deg/s
        public bool showPredictionGizmo = true;

        [Header("Parts")]
        public Transform head, barrel, firePoint;

        public BuildTile Tile { get; private set; }
        public Enemy Target => target;

        Enemy target;
        Vector3 aimPoint;
        Vector3 launchVelocity; // ballistic towers only
        bool hasSolution;
        float cooldown, retargetTimer;
        Collider[] ownColliders;

        public static Tower Create(BuildTile tile, TowerType type)
        {
            var go = new GameObject("Tower_" + type.name);
            go.transform.SetParent(tile.transform, false); // child of tile: hovering it still resolves the tile
            go.transform.localPosition = Vector3.zero;

            var t = go.AddComponent<Tower>();
            t.type = type;
            t.Tile = tile;
            var parts = TowerModel.Build(go.transform, type);
            t.head = parts.head; t.barrel = parts.barrel; t.firePoint = parts.firePoint;
            t.ownColliders = go.GetComponentsInChildren<Collider>();
            // Small random start offset so a row of identical towers doesn't fire in lockstep.
            t.cooldown = Random.Range(0f, 1f / type.fireRate);
            tile.occupant = t;
            return t;
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

            hasSolution = false;
            if (target != null)
            {
                bool pathAware = gm != null && gm.PathAwareAim;
                if (type.ballistic)
                {
                    // [A+] Gravity arc: solve the launch angle to where the bug will be when the shell lands.
                    hasSolution = AimPredictor.SolveBallisticIntercept(firePoint.position, target, type.projectileSpeed,
                        type.highArc, pathAware && usePrediction, out launchVelocity, out aimPoint);
                    if (!usePrediction && hasSolution) // no lead: lob at where the bug is right now
                        hasSolution = AimPredictor.SolveBallistic(firePoint.position, target.AimPosition, type.projectileSpeed,
                            type.highArc, out launchVelocity, out _);
                    if (hasSolution) AimAt(firePoint.position + launchVelocity); // barrel points along the launch direction
                }
                else
                {
                    // [Req C] Aim at the enemy. [Req A] Lead it based on its velocity and position.
                    // [A+] P toggles path-aware prediction (follows the bug around corners).
                    if (!usePrediction) aimPoint = target.AimPosition;
                    else if (pathAware) aimPoint = AimPredictor.PredictAlongPath(firePoint.position, target, type.projectileSpeed);
                    else aimPoint = AimPredictor.PredictInterceptPoint(firePoint.position, target.AimPosition, target.Velocity, type.projectileSpeed);
                    hasSolution = true;
                    AimAt(aimPoint);
                }
            }

            // [Req C] Fire continuously at a set rate while there is a target.
            cooldown -= Time.deltaTime;
            if (cooldown <= 0f && target != null && hasSolution)
            {
                Fire();
                cooldown = 1f / type.fireRate;
            }
            else if (cooldown < 0f) cooldown = 0f; // ready to fire the moment a target appears
        }

        bool InRange(Enemy e)
        {
            var d = e.transform.position - transform.position;
            d.y = 0f;
            return d.sqrMagnitude <= type.range * type.range;
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
            var parent = GameManager.Instance != null ? GameManager.Instance.transform : null;
            if (type.ballistic)
            {
                var shell = Projectile.Spawn(parent, firePoint.position, launchVelocity, type.damage,
                                             type.projectileScale, type.color, ownColliders, true);
                shell.splashRadius = type.splashRadius;
                Sfx.Play(SfxId.ShootMortar, 0.9f);
                return;
            }
            Vector3 dir = (aimPoint - firePoint.position).normalized;
            Projectile.Spawn(parent, firePoint.position, dir * type.projectileSpeed, type.damage,
                             type.projectileScale, type.color, ownColliders);
            Sfx.Play(type.fireRate >= 3f ? SfxId.ShootLight : type.fireRate >= 1f ? SfxId.ShootMid : SfxId.ShootHeavy,
                     type.fireRate >= 3f ? 0.5f : 0.8f, 0.07f);
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
