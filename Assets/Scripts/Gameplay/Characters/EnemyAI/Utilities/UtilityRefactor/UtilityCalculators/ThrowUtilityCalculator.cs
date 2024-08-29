using Gameplay.Characters.EnemyAI.Utilities.UtilityRefactor.Util;
using Hands.SinglePlayer.EnemyAI.Priority;
using Hands.SinglePlayer.EnemyAI.StatefulRefactor;
using RNGNeeds;
using UnityEngine;

namespace Gameplay.Characters.EnemyAI.Utilities.UtilityRefactor.UtilityCalculators
{
    public class ThrowUtilityCalculator : IUtilityCalculator
    {
        public UtilityType Type => UtilityType.Actor;
        public int State => StateStruct.Throw;
        public PriorityData PriorityData { get; set; }
        public ProbabilityList<float> probabilityList;
        public float CalculateTrajectoryUtility(Actor owner, DodgeBall ball, Vector3 trajectory) => 0f;
        public float CalculateBallUtility(Actor owner, DodgeBall ball) => 0f;

        [UtilityLink("Assets/Priorities/BaseThrow.asset", "ThrowUtilityCalculator.cs")]
        public float CalculateActorUtility(Actor owner, Actor other)
        {
            if (!owner.hasBall || owner.outOfPlay || other.team == owner.team) return 0;
            if(probabilityList.PickValue() == 0) return 0;
            
            var utility = 0f;
            var proximityScore = 1f / Vector3.Distance(owner.transform.position, other.transform.position);
            utility += proximityScore * PriorityData.GetPriorityValue(PriorityType.DistanceToEnemy);

            if (Physics.Raycast(owner.transform.position, other.transform.position - owner.transform.position,
                    out var hit, 100f))
            {
                if (hit.collider.gameObject == other.gameObject)
                {
                    utility += proximityScore * PriorityData.GetPriorityValue(PriorityType.LineOfSight);
                }
            }

            if (owner is DodgeballAI ai && ai.ActorTarget && ai.ActorTarget == other)
                utility += proximityScore * PriorityData.GetPriorityValue(PriorityType.Targeted) * 2;

            return utility;
        }
    }
}