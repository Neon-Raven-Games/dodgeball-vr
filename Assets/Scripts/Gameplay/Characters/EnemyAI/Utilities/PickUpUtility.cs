using System.Collections;
using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI.Utilities
{
    public class PickUpUtility : Utility<PickUpUtilityArgs>, IUtility
    {
        private DodgeballAI _ai;

        public PickUpUtility(PickUpUtilityArgs args, DodgeballAI ai) : base(args, AIState.PickUp)
        {
            _ai = ai;
        }

        private float ballDistance;

        public AIState DerivedState { get; }

        public override float Execute(DodgeballAI ai)
        {
            if (!ai.CurrentTarget || !ai.targetUtility.BallTarget || ai.hasBall || ai.IsOutOfPlay()) return -1;
            
            ballDistance = Vector3.Distance(ai.transform.position, ai.targetUtility.BallTarget.transform.position);
            if (ballDistance < args.pickupDistanceThreshold)
            {
                ai.PickUpBall(ai.targetUtility.BallTarget);
                if (!ai.hasBall) return -1f;
                return 1f;
            }

            ApproachBallToPickUp(ai.targetUtility.BallTarget, ai);
            return CalculatePickUpUtility(ai);
        }

        private bool pickup;
        private bool isLerpingBackToIdle;

        internal void Update()
        {
            if (isLerpingBackToIdle) LerpBackToIdleUpdate(_ai);
        }

        private void LerpBackToIdleUpdate(DodgeballAI ai)
        {
            var newY = Mathf.Lerp(ai.transform.position.y, 0.11f, Time.deltaTime * args.lerpBackSpeed);
            var pos = ai.transform.position;
            pos.y = newY;
            ai.transform.position = pos;

            args.ik.solvers.spine.SetIKPositionWeight(Mathf.Lerp(args.ik.solvers.spine.GetIKPositionWeight(), 0,
                Time.deltaTime * args.lerpBackSpeed));
            args.ik.solvers.rightHand.SetIKPositionWeight(Mathf.Lerp(args.ik.solvers.rightHand.GetIKPositionWeight(), 0,
                Time.deltaTime * args.lerpBackSpeed));
            args.ik.solvers.rightHand.maintainRotationWeight = Mathf.Lerp(args.ik.solvers.spine.GetIKPositionWeight(),
                0, Time.deltaTime * args.lerpBackSpeed);

            if (args.ik.solvers.rightHand.GetIKPositionWeight() <= 0 ||
                args.ik.solvers.spine.GetIKPositionWeight() <= 0 ||
                args.ik.solvers.rightHand.maintainRotationWeight <= 0)
            {
                args.ik.solvers.spine.SetIKPositionWeight(0);
                args.ik.solvers.rightHand.SetIKPositionWeight(0);
                args.ik.solvers.rightHand.maintainRotationWeight = 0;
                isLerpingBackToIdle = false;
            }
        }

        public void StopPickup(DodgeballAI ai)
        {
            if (!pickup && args.ik.solvers.rightHand.GetIKPositionWeight() == 0) return;
            pickup = false;
            isLerpingBackToIdle = true;
        }

        // the ai is targetting the ball too quickly,
        // this is resulting in the ai not being able to pick up the ball
        // it is getting his hand under/infornt the dodgeball, where the colliders interact
        // we need his hand to be above the dodgeball, so maybe the hand weight should be 0.5
        private void ApproachBallToPickUp(DodgeBall dodgeBall, DodgeballAI ai)
        {
            pickup = true;
            var lerpFactor = Mathf.Clamp01((ballDistance / args.ikDistanceThreshold) - 1);
            var position = ai.transform.position;

            position.y = Mathf.Lerp(args.ballPickupHeight, args.ballIdleHeight, lerpFactor);
            ai.transform.position = position;

            args.ik.solvers.spine.target = dodgeBall.transform;
            args.ik.solvers.spine.SetIKPositionWeight(Mathf.Lerp(args.spineIKWeight, 0, lerpFactor));

            args.ik.solvers.rightHand.target = ai.CurrentTarget.transform;
            args.ik.solvers.rightHand.SetIKPositionWeight(Mathf.Lerp(1, 0, lerpFactor));
            args.ik.solvers.rightHand.maintainRotationWeight = Mathf.Lerp(args.maintainRotationWeight, 0, lerpFactor);
        }

        public override float Roll(DodgeballAI ai) => CalculatePickUpUtility(ai);

        private float CalculatePickUpUtility(DodgeballAI ai)
        {
            if (ai.targetUtility.BallTarget == null || ai.hasBall || ai.IsOutOfPlay()) return 0;
            if (!IsInPlayArea(ai.targetUtility.BallTarget.transform.position)) return 0f;
            if (ai.targetUtility.BallTarget._ballState != BallState.Dead) return 0f;

            float utility = 0;
            utility += 5f;

            utility += (1.0f / ballDistance) * ai.distanceWeight;

            utility += Random.value * ai.difficultyFactor;
            return utility;
        }

        private bool IsTeammateCloserToBall(DodgeballAI ai)
        {
            foreach (var teammateAI in ai.friendlyTeam.actors)
            {
                if (teammateAI == ai) continue;
                if (teammateAI == null) continue;

                if (teammateAI is DodgeballAI dodgeballAI)
                {
                    if (dodgeballAI.CurrentTarget == ai.CurrentTarget &&
                        Vector3.Distance(dodgeballAI.transform.position, ai.CurrentTarget.transform.position) <
                        Vector3.Distance(ai.transform.position, ai.CurrentTarget.transform.position))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}