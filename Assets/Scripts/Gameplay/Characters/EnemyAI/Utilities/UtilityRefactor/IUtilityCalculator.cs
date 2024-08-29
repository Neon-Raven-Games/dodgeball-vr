
using System;
using Hands.SinglePlayer.EnemyAI.Priority;
using UnityEngine;

namespace Gameplay.Characters.EnemyAI.Utilities.UtilityRefactor
{
    [Flags]
    public enum UtilityType
    {
        None = 0,
        Actor = 1 << 0,
        Ball = 1 << 1,
        Trajectory = 1 << 2,
    }
    public interface IUtilityCalculator
    {
        UtilityType Type { get; }
        int State { get; }
        PriorityData PriorityData { get; set; }
        float CalculateActorUtility(Actor owner, Actor other);
        float CalculateBallUtility(Actor owner, DodgeBall ball);
        float CalculateTrajectoryUtility(Actor owner, DodgeBall ball, Vector3 trajectory);
    }
}