using UnityEngine;

namespace Gameplay.InGameEvents
{
    public enum BalanceCurveType
    {
        Cooldown,
        Duration,
        Intensity
    }
    
    public class BalanceCurve : ScriptableObject
    {
        public AnimationCurve cooldownCurve;
        public AnimationCurve durationCurve;
        public AnimationCurve intensityCurve;

        public float Evaluate(BalanceCurveType curveType, int level, float start, float end)
        {
            var curve = curveType switch
            {
                BalanceCurveType.Cooldown => cooldownCurve,
                BalanceCurveType.Duration => durationCurve,
                BalanceCurveType.Intensity => intensityCurve,
                _ => throw new System.ArgumentOutOfRangeException()
            };
            return curve.Evaluate(level, start, end);
        }
        
    }

    public static class CurveExtensions
    {
        public static float Evaluate(this AnimationCurve curve, int level, float start, float end)
        {
            var normalizedLevel = Mathf.Clamp01((level - 1f) / end - 1); 
            var curveValue = curve.Evaluate(normalizedLevel);
            return Mathf.Lerp(start, end, curveValue);
        }
    }
}