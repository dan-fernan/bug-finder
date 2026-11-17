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

        /// <summary>
        /// [A+] Path-aware prediction. Linear prediction assumes the bug keeps going straight, so it misses
        /// when a bug turns a corner mid-flight. Instead, walk the bug along its remaining waypoints:
        /// guess a flight time, find where the bug will be then, re-time the shot to that point, repeat.
        /// Converges in a few iterations because projectiles are much faster than bugs.
        /// </summary>
        public static Vector3 PredictAlongPath(Vector3 shooter, Enemy e, float projSpeed, int iterations = 5)
        {
            Vector3 p = e.AimPosition;
            for (int i = 0; i < iterations; i++)
            {
                float t = Vector3.Distance(shooter, p) / projSpeed;
                p = e.PredictPosition(t);
            }
            return p;
        }

        /// <summary>
        /// Launch velocity for a gravity projectile of speed <paramref name="v"/> to hit <paramref name="to"/>.
        /// theta = atan((v^2 +- sqrt(v^4 - g(g x^2 + 2 y v^2))) / (g x)), x = horizontal distance, y = height difference.
        /// "+" is the high (mortar) arc. Returns false if the point is out of reach.
        /// </summary>
        public static bool SolveBallistic(Vector3 from, Vector3 to, float v, bool highArc, out Vector3 velocity, out float flightTime)
        {
            float g = Mathf.Max(0.01f, -Physics.gravity.y);
            Vector3 delta = to - from;
            Vector3 flat = new Vector3(delta.x, 0f, delta.z);
            float x = flat.magnitude, y = delta.y;
            float v2 = v * v;
            float disc = v2 * v2 - g * (g * x * x + 2f * y * v2);
            velocity = Vector3.zero;
            flightTime = 0f;
            if (disc < 0f || x < 0.01f) return false;

            float sq = Mathf.Sqrt(disc);
            float theta = Mathf.Atan((v2 + (highArc ? sq : -sq)) / (g * x));
            Vector3 dir = flat / x;
            velocity = dir * (v * Mathf.Cos(theta)) + Vector3.up * (v * Mathf.Sin(theta));
            flightTime = x / (v * Mathf.Cos(theta));
            return true;
        }

        /// <summary>
        /// [A+] Ballistic intercept: guess t, predict where the target is at t, solve the launch angle to that point,
        /// take that arc's real flight time as the new t, and repeat (4-6 times is plenty).
        /// </summary>
        public static bool SolveBallisticIntercept(Vector3 shooter, Enemy e, float v, bool highArc, bool pathAware,
                                                   out Vector3 velocity, out Vector3 aimPoint, int iterations = 6)
        {
            aimPoint = e.AimPosition;
            velocity = Vector3.zero;
            float t = Vector3.Distance(shooter, aimPoint) / v;
            bool ok = false;
            for (int i = 0; i < iterations; i++)
            {
                aimPoint = pathAware ? e.PredictPosition(t) : e.AimPosition + e.Velocity * t;
                bool solved = SolveBallistic(shooter, aimPoint, v, highArc, out var vel, out var ft)
                              || (highArc && SolveBallistic(shooter, aimPoint, v, false, out vel, out ft)); // fall back to the low arc
                if (!solved) break;
                velocity = vel;
                t = ft;
                ok = true;
            }
            return ok;
        }
    }
}
