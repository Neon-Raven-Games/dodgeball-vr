using System;
using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI.Priority
{
    // to be displayed as a slider
    // max value set on the priority data
    [Serializable]
    public class Priority
    {
        public PriorityType priority;
        public float score;
    }
    
    // to populate at runtime for O(1) access
    public enum PriorityType
    {
        DistanceToEnemy,
        FreeBall,
        PossessedBall,
        Enemy,
        EnemyTarget,
        Team,
        TeamTarget,
        LineOfSight,
        InTrajectory,
        EnemyPossession,
        InsidePlayArea,
        OutsidePlayArea,
        DistanceToTarget,
        DistanceToBall,
        Targeted
    }
}