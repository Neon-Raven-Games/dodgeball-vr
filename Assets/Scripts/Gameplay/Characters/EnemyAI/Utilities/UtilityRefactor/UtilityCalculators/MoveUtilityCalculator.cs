using Hands.SinglePlayer.EnemyAI.Priority;
using Hands.SinglePlayer.EnemyAI.StatefulRefactor;
using UnityEngine;

namespace Gameplay.Characters.EnemyAI.Utilities.UtilityRefactor.UtilityCalculators
{
    public class MoveUtilityCalculator : IUtilityCalculator
    {
        public UtilityType Type { get; }
        public int State => StateStruct.Move;
        public PriorityData PriorityData { get; set; }
        public float CalculateActorUtility(Actor owner, Actor other)
        {
            return 0.01f;
        }

        public float CalculateBallUtility(Actor owner, DodgeBall ball)
        {
            return 0.01f;
        }

        public float CalculateTrajectoryUtility(Actor owner, DodgeBall ball, Vector3 trajectory)
        {
            return 0.01f;
        }
    }
}