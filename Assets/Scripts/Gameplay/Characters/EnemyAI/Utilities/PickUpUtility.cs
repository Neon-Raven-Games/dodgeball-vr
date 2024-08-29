using System.Collections;
using Cysharp.Threading.Tasks;
using Hands.SinglePlayer.EnemyAI.StatefulRefactor;
using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI.Utilities
{
    public class PickUpUtility : Utility<PickUpUtilityArgs>, IUtility
    {
        private DodgeballAI AI;
        internal bool pickup;
        private bool _isLerpingBackToIdle;
        private float _ballDistance;

        public PickUpUtility(PickUpUtilityArgs args, DodgeballAI ai) : base(args, AIState.PickUp)
        {
            AI = ai;
        }


        public int State => StateStruct.PickUp;

        public override float Execute(DodgeballAI ai)
        {
            pickup = false;
            if (!ai.CurrentTarget || !ai.BallTarget || ai.hasBall || ai.IsOutOfPlay()) return -1;
            if (!IsInPlayArea(ai.BallTarget.transform.position)) return -1f;
            if (ai.BallTarget._ballState != BallState.Dead) return -1f;

            _ballDistance = Vector3.Distance(ai.transform.position, ai.BallTarget.transform.position);
            if (_ballDistance < args.pickupDistanceThreshold)
            {
                ai.PickUpBall(ai.BallTarget);
                LerpBackToIdleUpdate().Forget();
                if (!ai.hasBall) return -1f;
                return 1f;
            }

            pickup = true;
            ApproachBallToPickUp(ai.BallTarget, ai);
            return CalculatePickUpUtility(ai);
        }

        private async UniTaskVoid LerpBackToIdleUpdate()
        {
            if (_lerpingBackToIdle) return;
            _lerpingBackToIdle = true;
            var currentTime = 0f;
            var pos = AI.transform.position;
            var originalSpineWeight = AI.ik.solvers.spine.GetIKPositionWeight();
            var originalHandWeight = AI.ik.solvers.rightHand.GetIKPositionWeight();
            var originalMaintainRotationWeight = AI.ik.solvers.rightHand.maintainRotationWeight;
            
            while (currentTime < 1)
            {
                currentTime += Time.deltaTime;
                var t = Mathf.Clamp01(currentTime / args.lerpBackSpeed);
                if (1 - t < 0.01) break;
                var newY = Mathf.Lerp(AI.transform.position.y, 0.11f, t);
                pos.y = newY;
                AI.transform.position = pos;

                AI.ik.solvers.spine.SetIKPositionWeight(
                    Mathf.Lerp(originalSpineWeight, 0, t));
                AI.ik.solvers.rightHand.SetIKPositionWeight(
                    Mathf.Lerp(originalHandWeight, 0, t));
                AI.ik.solvers.rightHand.maintainRotationWeight =
                    Mathf.Lerp(originalMaintainRotationWeight, 0, t);

                await UniTask.Yield();
            }
            _lerpingBackToIdle = false;
        }

        private bool _lerpingBackToIdle;

        public void StopPickup(DodgeballAI ai)
        {
            // if (_lerpingBackToIdle) return;
            // _lerpingBackToIdle = true;
            // pickup = false;
            // LerpBackToIdleUpdate().Forget();
        }

        // the ai is targetting the ball too quickly,
        // this is resulting in the ai not being able to pick up the ball
        // it is getting his hand under/infornt the dodgeball, where the colliders interact
        // we need his hand to be above the dodgeball, so maybe the hand weight should be 0.5
        private void ApproachBallToPickUp(DodgeBall dodgeBall, DodgeballAI ai)
        {
            if (!pickup) return;
            var lerpFactor = Mathf.Clamp01((_ballDistance / args.ikDistanceThreshold) - 1);
            var position = ai.transform.position;

            position.y = Mathf.Lerp(args.ballPickupHeight, args.ballIdleHeight, lerpFactor);
            ai.transform.position = position;

            AI.ik.solvers.spine.target = dodgeBall.transform;
            AI.ik.solvers.spine.SetIKPositionWeight(Mathf.Lerp(args.spineIKWeight, 0, lerpFactor));

            AI.ik.solvers.rightHand.target = ai.CurrentTarget.transform;
            AI.ik.solvers.rightHand.SetIKPositionWeight(Mathf.Lerp(1, 0, lerpFactor));
            AI.ik.solvers.rightHand.maintainRotationWeight = Mathf.Lerp(args.maintainRotationWeight, 0, lerpFactor);
        }

        public override float Roll(DodgeballAI ai) => CalculatePickUpUtility(ai);

        private float CalculatePickUpUtility(DodgeballAI ai)
        {
            if (ai.BallTarget == null || ai.hasBall || ai.IsOutOfPlay()) return 0;
            if (!IsInPlayArea(ai.BallTarget.transform.position)) return 0f;
            if (ai.BallTarget._ballState != BallState.Dead) return 0f;

            var utility = 5f;
            utility += (1.0f / _ballDistance) * args.pickupDistanceThreshold;
            utility += Random.value * ai.difficultyFactor;

            return utility;
        }
    }
}