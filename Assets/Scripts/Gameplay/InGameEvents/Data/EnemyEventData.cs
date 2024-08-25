using System;
using Gameplay.Util;
using UnityEngine;

namespace Gameplay.InGameEvents
{
    [Serializable]
    public class EnemyEventData : EventBalanceData
    {
        public BalanceCurve balanceCurve;
        public int eventLevelCap = 100;
    
        public float eventDuration;
        public float eventCooldown;
        public float eventIntensity;

        public float EventDuration => eventDuration;
        public float EventCooldown => eventCooldown;
        
        public void UpdateEventData()
        {
            eventDuration = GetCurveValue(BalanceCurveType.Duration);
            eventCooldown = GetCurveValue(BalanceCurveType.Cooldown);
            eventIntensity = GetCurveValue(BalanceCurveType.Intensity);
        }

        private float GetCurveValue(BalanceCurveType curveType)
        {
            var normalizedLevel = Mathf.Clamp01((eventLevel - 1f) / eventLevelCap - 1);
            if (!balanceCurve) return 0f;
            switch (curveType)
            {
                case BalanceCurveType.Duration:
                    return balanceCurve.durationCurve.Evaluate(normalizedLevel);
                case BalanceCurveType.Cooldown:
                    return balanceCurve.cooldownCurve.Evaluate(normalizedLevel);
                case BalanceCurveType.Intensity:
                    return balanceCurve.intensityCurve.Evaluate(normalizedLevel);
                default:
                    return 0;
            }
        }

        public void UpdateEventCooldownAndDuration(Action invokeEvent)
        {
            eventLevel++;
            UpdateEventData();
            TimerManager.AddTimer(EventCooldown, invokeEvent);
        }
    }
}