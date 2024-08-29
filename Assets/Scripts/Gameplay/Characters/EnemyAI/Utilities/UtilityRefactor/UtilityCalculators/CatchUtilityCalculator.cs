using Hands.SinglePlayer.EnemyAI.Priority;
using Hands.SinglePlayer.EnemyAI.StatefulRefactor;
using UnityEngine;

namespace Gameplay.Characters.EnemyAI.Utilities.UtilityRefactor.UtilityCalculators
{
    /// <summary>
    /// Calculates the catch utility from the callback of the ball thrown event.
    /// </summary>
    public class CatchUtilityCalculator : IUtilityCalculator
    {
        public int State => StateStruct.Catch;
        public UtilityType Type => UtilityType.Trajectory;
        public PriorityData PriorityData { get; set; }

        public float CalculateActorUtility(Actor owner, Actor other) => 0f;

        public float CalculateBallUtility(Actor owner, DodgeBall ball) => 0f;

        public float CalculateTrajectoryUtility(Actor owner, DodgeBall ball, Vector3 trajectory)
        {
            if (owner.hasBall || owner.outOfPlay) return 0;

            var utility = 0f;
            if (ball.team != owner.team && !ball.DeadBall())
            {
                var proximityScore = 1f / Vector3.Distance(owner.transform.position, ball.transform.position);
                if (Vector3.Dot(owner.transform.forward, trajectory) > 0.5f)
                    utility += proximityScore * PriorityData.GetPriorityValue(PriorityType.InTrajectory);
                utility += PriorityData.GetPriorityValue(PriorityType.LineOfSight);
            }

            return utility;
        }
    }
}