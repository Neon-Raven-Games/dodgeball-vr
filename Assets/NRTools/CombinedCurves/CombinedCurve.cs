using UnityEngine;

[CreateAssetMenu(fileName = "CombinedCurve", menuName = "Custom/CombinedCurve")]
public class CombinedCurve : ScriptableObject
{
    public Vector3Keyframe[] keyframes;
    
    public Vector3 Evaluate(float time)
    {
        if (keyframes == null || keyframes.Length == 0)
            return Vector3.zero;

        if (time <= keyframes[0].time)
            return keyframes[0].value;

        if (time >= keyframes[^1].time) return keyframes[^1].value;

        Vector3Keyframe prevKey = keyframes[0];
        Vector3Keyframe nextKey = keyframes[1];

        for (int i = 0; i < keyframes.Length - 1; i++)
        {
            if (time >= keyframes[i].time && time < keyframes[i + 1].time)
            {
                prevKey = keyframes[i];
                nextKey = keyframes[i + 1];
                break;
            }
        }

        float t = Mathf.InverseLerp(prevKey.time, nextKey.time, time);
        return HermiteInterpolate(prevKey.value, nextKey.value, t);
    }

    private Vector3 HermiteInterpolate(Vector3 p0, Vector3 p1, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        float h00 = 2 * t3 - 3 * t2 + 1;
        float h10 = t3 - 2 * t2 + t;
        float h01 = -2 * t3 + 3 * t2;
        float h11 = t3 - t2;

        // For simplicity, tangents are not considered here, assuming a simple smooth curve
        return h00 * p0 + h01 * p1;
    }
}