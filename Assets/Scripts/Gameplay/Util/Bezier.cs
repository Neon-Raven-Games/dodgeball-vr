using UnityEngine;

namespace Gameplay.Util
{
    public static class Bezier
    {
        public static Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            var u = 1 - t;
            var tt = t * t;
            var uu = u * u;
        
            var p = uu * p0; // u^2 * p0
            p += 2 * u * t * p1; // 2 * u * t * p1
            p += tt * p2; // t^2 * p2

            return p;
        }

    }
}