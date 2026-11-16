using UnityEngine;

namespace BugDefense
{
    /// <summary>[Req A] Aim at the predicted intercept point from target position and velocity.</summary>
    public static class AimPredictor
    {
        /// <summary>
        /// Returns the point to aim at so a constant-speed (no gravity) projectile hits a target
        /// moving with constant velocity. Falls back to the target's current position if no solution.
        /// Solves |D + V t| = s t  =>  (V.V - s^2) t^2 + 2 (D.V) t + D.D = 0
        /// where D = target - shooter, V = target velocity, s = projectile speed.
        /// </summary>
        public static Vector3 PredictInterceptPoint(Vector3 shooter, Vector3 target, Vector3 targetVel, float projSpeed)
        {
            Vector3 d = target - shooter;
            float a = Vector3.Dot(targetVel, targetVel) - projSpeed * projSpeed;
            float b = 2f * Vector3.Dot(d, targetVel);
            float c = Vector3.Dot(d, d);
            float t;
            if (Mathf.Abs(a) < 1e-4f) // speeds nearly equal -> linear equation
            {
                if (Mathf.Abs(b) < 1e-4f) return target;
                t = -c / b;
            }
            else
            {
                float disc = b * b - 4f * a * c;
                if (disc < 0f) return target; // projectile can never catch it
                float sq = Mathf.Sqrt(disc);
                float t1 = (-b - sq) / (2f * a);
                float t2 = (-b + sq) / (2f * a);
                t = (t1 > 0f && t2 > 0f) ? Mathf.Min(t1, t2) : Mathf.Max(t1, t2); // smallest positive root
            }
            return t > 0f ? target + targetVel * t : target;
        }
    }
}
