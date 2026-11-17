using System.Collections.Generic;
using UnityEngine;

namespace BugDefense
{
    /// <summary>Physics projectile (dynamic Rigidbody, continuous collision). Damages the first enemy it hits.</summary>
    public class Projectile : MonoBehaviour
    {
        public float damage;
        public float lifetime = 4f;
        /// <summary>> 0: explodes on any impact, damaging every bug within this radius (Compiler).</summary>
        public float splashRadius;
        Color color;
        bool spent;

        public static Projectile Spawn(Transform parent, Vector3 pos, Vector3 velocity, float damage,
                                       Vector3 scale, Color color, Collider[] ignore, bool useGravity = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); // keeps its SphereCollider (not a trigger)
            go.name = "Projectile";
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            if (velocity.sqrMagnitude > 0.0001f) go.transform.rotation = Quaternion.LookRotation(velocity); // elongated shots point forward
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Lit(color, 3f);

            var col = go.GetComponent<Collider>();
            if (ignore != null)
                foreach (var c in ignore) if (c != null) Physics.IgnoreCollision(col, c);

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.05f;
            rb.useGravity = useGravity;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = velocity;
#else
            rb.velocity = velocity;
#endif

            var p = go.AddComponent<Projectile>();
            p.damage = damage;
            p.color = color;
            if (useGravity) p.lifetime = 6f; // high arcs stay airborne longer
            return p;
        }

        void Update()
        {
            lifetime -= Time.deltaTime;
            if (lifetime <= 0f || transform.position.y < -5f) Destroy(gameObject);
        }

        void OnCollisionEnter(Collision collision)
        {
            if (spent) return;
            spent = true;
            Vector3 pos = transform.position;
            if (splashRadius > 0f)
            {
                Explode(pos);
            }
            else
            {
                var enemy = collision.collider.GetComponentInParent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                    Sfx.Play(SfxId.Hit, 0.6f, 0.06f);
                }
                HitFx.Burst(pos, color, enemy != null ? 5 : 3, 0.08f + transform.localScale.x * 0.25f, 3.5f);
            }
            Destroy(gameObject);
        }

        /// <summary>[A+] Splash damage: hits every bug within the radius (horizontal distance).</summary>
        void Explode(Vector3 pos)
        {
            HitFx.Ring(pos, splashRadius, color);
            HitFx.Burst(pos, color, 10, 0.18f, 6f);
            Sfx.Play(SfxId.Splash, 0.8f);
            // Copy first: damage can kill bugs (and a boss split adds new ones) while we iterate.
            var targets = new List<Enemy>(EnemyRegistry.All);
            foreach (var e in targets)
            {
                if (e == null) continue;
                Vector3 d = e.transform.position - pos;
                d.y = 0f;
                if (d.magnitude <= splashRadius + 0.5f * e.transform.localScale.x) e.TakeDamage(damage);
            }
        }
    }
}
