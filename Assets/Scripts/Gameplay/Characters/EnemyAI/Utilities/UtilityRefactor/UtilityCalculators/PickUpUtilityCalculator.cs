using Gameplay.Characters.EnemyAI.Utilities.UtilityRefactor.Util;
using Hands.SinglePlayer.EnemyAI.Priority;
using Hands.SinglePlayer.EnemyAI.StatefulRefactor;
using UnityEngine;

namespace Gameplay.Characters.EnemyAI.Utilities.UtilityRefactor.UtilityCalculators
{
    public class PickUpUtilityCalculator : IUtilityCalculator
    {
        public UtilityType Type => UtilityType.Ball;
        public int State => StateStruct.PickUp;
        public PriorityData PriorityData { get; set; }

        private Bounds playAreaBounds;
        public float CalculateActorUtility(Actor owner, Actor other) => 0f;
        public float CalculateTrajectoryUtility(Actor owner, DodgeBall ball, Vector3 trajectory) => 0f;

        [UtilityLink("Assets/Priorities/BasePickup.asset", "PickUpUtilityCalculator.cs")]
        public float CalculateBallUtility(Actor owner, DodgeBall ball)
        {
            if (owner.hasBall || owner.outOfPlay || !ball.gameObject.activeInHierarchy) return 0;

            var utility = 0f;
            var proximityScore = 1f / Vector3.Distance(owner.transform.position, ball.transform.position);
            utility += proximityScore * PriorityData.GetPriorityValue(PriorityType.DistanceToBall);
            
            var ai = owner as DodgeballAI;
            if (!ai) return utility;
            
            if (ai.BallTarget && ai.BallTarget == ball)
                utility += proximityScore * PriorityData.GetPriorityValue(PriorityType.Targeted);
            if (ball.DeadBall()) utility += proximityScore * PriorityData.GetPriorityValue(PriorityType.FreeBall);
           
            if (playAreaBounds.size == Vector3.zero) playAreaBounds = new Bounds(owner.friendlyTeam.playArea.position,
                new Vector3(owner.friendlyTeam.playArea.localScale.x, 5, owner.friendlyTeam.playArea.localScale.z));
            
            if (playAreaBounds.Contains(ball.transform.position))
                utility += proximityScore * PriorityData.GetPriorityValue(PriorityType.InsidePlayArea);
            else 
                utility = 0f;

            return utility;
        }
    }
}