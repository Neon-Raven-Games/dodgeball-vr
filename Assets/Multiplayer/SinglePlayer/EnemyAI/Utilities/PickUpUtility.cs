using System.Collections;
using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI.Utilities
{
    public class PickUpUtility : Utility<PickUpUtilityArgs>
    {
        public PickUpUtility(PickUpUtilityArgs args) : base(args)
        {
        }

        public override float Execute(DodgeballAI ai)
        {
            // check if the distance is less than the threshold
            // if it is, call the AI.PickUpBall(DodgeBall ball) method
            // return the utility value
            if (ai.CurrentTarget == null || ai.CurrentTarget.layer != LayerMask.NameToLayer("Ball") ||
                ai.hasBall || ai.IsOutOfPlay())
                return 0;
            if (Vector3.Distance(ai.transform.position, ai.CurrentTarget.transform.position) <
                args.pickupDistanceThreshold)
            {
                ai.PickUpBall(ai.CurrentTarget.GetComponent<DodgeBall>());
                return 1f;
            }
            ApproachBallToPickUp(ai.CurrentTarget.GetComponent<DodgeBall>(), ai);
            return CalculatePickUpUtility(ai);
        }

        private bool pickup;
        public void StopPickup(DodgeballAI ai)
        {
            if (!pickup && args.ik.solvers.rightHand.GetIKPositionWeight() == 0) return;
            pickup = false;
            ai.StartCoroutine(LerpBackToIdle(ai));
        }
        private IEnumerator LerpBackToIdle(DodgeballAI ai)
        {
            while (args.ik.solvers.rightHand.GetIKPositionWeight() > 0f)
            {
                var newY = Mathf.Lerp(ai.transform.position.y, 0.11f, Time.deltaTime * 3);
                var pos = ai.transform.position;
                pos.y = newY;
                ai.transform.position = pos;
                
                // why is this not lerping to idle?
                var spineSolver = args.ik.solvers.spine.GetIKPositionWeight();
                args.ik.solvers.spine.SetIKPositionWeight(Mathf.Lerp(spineSolver, 0, Time.deltaTime * 3));
                
                var rightHandSolver = args.ik.solvers.rightHand.GetIKPositionWeight();
                args.ik.solvers.rightHand.SetIKPositionWeight(Mathf.Lerp(rightHandSolver, 0, Time.deltaTime * 3));
                
                var spinePosWeight = args.ik.solvers.spine.GetIKPositionWeight();
                args.ik.solvers.rightHand.maintainRotationWeight = Mathf.Lerp(spinePosWeight, 0, Time.deltaTime * 3);
                yield return null;
            }
            
            args.ik.solvers.spine.SetIKPositionWeight(0);
            args.ik.solvers.rightHand.SetIKPositionWeight(0);
            args.ik.solvers.rightHand.maintainRotationWeight = 0;
        }

        private void ApproachBallToPickUp(DodgeBall dodgeBall, DodgeballAI ai)
        {
            pickup = true;
            var distance = Vector3.Distance(ai.transform.position, dodgeBall.transform.position);
            var lerpFactor = Mathf.Clamp01((distance / args.pickupDistanceThreshold) - 1);

            var position = ai.transform.position;
            position.y = Mathf.Lerp(-0.27f, 0.11f, lerpFactor);
            ai.transform.position = position;

            args.ik.solvers.spine.target = dodgeBall.transform;
            args.ik.solvers.spine.SetIKPositionWeight(Mathf.Lerp(0.063f, 0, lerpFactor));

            args.ik.solvers.rightHand.target = ai.CurrentTarget.transform;
            args.ik.solvers.rightHand.SetIKPositionWeight(Mathf.Lerp(1, 0, lerpFactor));
            args.ik.solvers.rightHand.maintainRotationWeight = Mathf.Lerp(0.4f, 0, lerpFactor);
        }

        public override float Roll(DodgeballAI ai) => CalculatePickUpUtility(ai);
        
        internal float CalculatePickUpUtility(DodgeballAI ai)
        {
            if (ai.CurrentTarget == null || ai.CurrentTarget.layer != LayerMask.NameToLayer("Ball") ||
                ai.hasBall || ai.IsOutOfPlay())
                return 0;

            GameObject nearestBall = FindNearestBallInPlayArea(ai.playArea, ai);
            if (nearestBall == null || !nearestBall.activeInHierarchy) return 0;

            var ball = nearestBall.GetComponent<DodgeBall>();

            if (ball._ballState != BallState.Dead)
                return 0f;
            if (!IsInPlayArea(nearestBall.transform.position, ai.friendlyTeam.playArea, ai.team))
                return 0f;

            float utility = 0;
            // Calculate utility based on the distance to the nearest ball
            var distance = Vector3.Distance(ai.transform.position, nearestBall.transform.position);
            utility += (1.0f / distance) * ai.distanceWeight;

            // Add a random component based on the difficulty factor
            utility += Random.value * ai.difficultyFactor;

            // Influence utility if another teammate is also targeting the ball
            if (IsTeammateTargetingBall(nearestBall, ai.playArea, ai))
            {
                utility *= (1.0f -
                            (ai.difficultyFactor /
                             2.0f)); // Lower the utility if another teammate is targeting the ball
            }

            return utility;
        }
    }
}