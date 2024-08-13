using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.DuckType.Jiggle
{
    public static class Extensions
    {
        public static Quaternion Append(this Quaternion source, Quaternion quaternion)
        {
            return quaternion * source;
        }

        public static Quaternion FromToRotation(this Quaternion source, Quaternion target)
        {
            return Quaternion.Inverse(source) * target;
        }

        public static Quaternion Scale(this Quaternion source, float scale)
        {
            return Quaternion.SlerpUnclamped(Quaternion.identity, source, scale);
        }

        public static Quaternion Inverse(this Quaternion source)
        {
            return Quaternion.Inverse(source);
        }

        public static List<Vector3> GetOrthogonalVectors(this Vector3 source, int numVectors)
        {
            var normalizedSource = source.normalized;
            var orthoVector = Mathf.Abs(source.normalized.y) != 1   // if source is neither "up" nor "down"
                ? Vector3.Cross(source, Vector3.up)
                : Vector3.Cross(source, Vector3.right);
            var degreesPerInst = 360f / numVectors;
            var result = new List<Vector3>();
            for (int i = 0; i < numVectors; i++)
                result.Add(Quaternion.AngleAxis(degreesPerInst * i, source) * orthoVector);
            return result;
        }
        
        public static bool HasLength(this Vector3 source)
        {
            return source.x != 0 || source.y != 0 || source.z != 0;
        }

        public static float Clamp01(this float source)
        {
            return Mathf.Max(Mathf.Min(source, 1), 0);
        }
    }
}
