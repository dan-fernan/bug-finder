using UnityEngine;

namespace BugDefense
{
    /// <summary>Physics projectile (dynamic Rigidbody, continuous collision). Damages the first enemy it hits.</summary>
    public class Projectile : MonoBehaviour
    {
        public float damage;
        public float lifetime = 4f;
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
            var enemy = collision.collider.GetComponentInParent<Enemy>();
            if (enemy != null) enemy.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
