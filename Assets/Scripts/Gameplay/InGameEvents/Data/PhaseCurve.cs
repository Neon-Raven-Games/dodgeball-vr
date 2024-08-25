using System;
using UnityEngine;

namespace Gameplay.InGameEvents
{
    public enum PhaseCurveType
    {
        TeamOneLives,
        TeamTwoLives,
    }
    public class PhaseCurve : ScriptableObject
    {
        public AnimationCurve teamOneLives;
        public AnimationCurve teamTwoLives;

        public int Evaluate(PhaseCurveType type, int level, float start, float end)
        {
            switch (type)
            {
                case PhaseCurveType.TeamOneLives:
                    return Math.Max(1, (int) teamOneLives.Evaluate(level, start, end));
                case PhaseCurveType.TeamTwoLives:
                    return Math.Max(1, (int) teamTwoLives.Evaluate(level, start, end));
                default:
                    throw new System.ArgumentOutOfRangeException();
            }
        }
    }
}