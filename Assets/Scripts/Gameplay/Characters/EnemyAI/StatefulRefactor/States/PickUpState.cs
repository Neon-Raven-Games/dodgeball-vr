using System;
using System.Collections;
using Hands.SinglePlayer.EnemyAI.StatefulRefactor.BaseState;
using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI.StatefulRefactor.States
{
    public class PickUpState : BaseAIState<PickUpUtilityArgs>
    {
        public override int State => StateStruct.PickUp;

        public PickUpState(DodgeballAI ai, AIStateController controller, PickUpUtilityArgs args) : base(ai, controller,
            args)
        {
        }

        public override void FixedUpdate()
        {
        }

        public override void EnterState()
        {
            active = true;
        }

        public override void ExitState()
        {
            active = false;
            AI.StartCoroutine(LerpBackToIdleUpdate(0.1f));
        }

        public override void UpdateState()
        {
            if (!active) return;
            controller.targetModule.UpdateTarget();
            MoveTowardsTarget(AI, AI.CurrentTarget.transform.position);
            ApproachBallToPickUp(AI.BallTarget, AI);
            if (!AI.BallTarget || 
                _ballDistance > Args.pickupDistanceThreshold || 
                controller.State != StateStruct.PickUp) 
                return;
            AI.PickUpBall(AI.BallTarget);
        }

        private float _ballDistance;

        private void ApproachBallToPickUp(DodgeBall dodgeBall, DodgeballAI ai)
        {
            if (!dodgeBall || !active) return;
            var flatBall = dodgeBall.transform.position;
            flatBall.y = ai.transform.position.y;
            _ballDistance = Vector3.Distance(flatBall, ai.transform.position);
            
            var lerpFactor = Mathf.Clamp01((_ballDistance / Args.ikDistanceThreshold) - 1);
            // var position = ai.transform.position;
            // position.y = Mathf.Lerp(Args.ballPickupHeight, Args.ballIdleHeight, lerpFactor);
            // ai.transform.position = position;

            AI.ik.solvers.spine.target = dodgeBall.transform;
            AI.ik.solvers.spine.SetIKPositionWeight(Mathf.Lerp(Args.spineIKWeight, 0, lerpFactor));

            AI.ik.solvers.rightHand.target = ai.CurrentTarget.transform;
            AI.ik.solvers.rightHand.SetIKPositionWeight(Mathf.Lerp(1, 0, lerpFactor));
            AI.ik.solvers.rightHand.maintainRotationWeight = Mathf.Lerp(Args.maintainRotationWeight, 0, lerpFactor);
        }

        public override void OnTriggerEnter(Collider collider)
        {
        }

        public override void OnTriggerExit(Collision col)
        {
        }


        private IEnumerator LerpBackToIdleUpdate(float timeToIdle = 0.5f)
        {
            var currentTime = 0f;
            var originalSpineWeight = AI.ik.solvers.spine.GetIKPositionWeight();
            var originalHandWeight = AI.ik.solvers.rightHand.GetIKPositionWeight();
            var originalMaintainRotationWeight = AI.ik.solvers.rightHand.maintainRotationWeight;
            var t = 0f;
            while (t < 1)
            {
                
                t = Mathf.Clamp01(t + (Time.deltaTime / timeToIdle));
                
                // var newY = Mathf.Lerp(originaly, 0.11f, t);
                // pos = AI.transform.position;
                // pos.y = newY;
                // AI.transform.position = pos;

                AI.ik.solvers.spine.SetIKPositionWeight(
                    Mathf.Lerp(originalSpineWeight, 0, t));
                AI.ik.solvers.rightHand.SetIKPositionWeight(
                    Mathf.Lerp(originalHandWeight, 0, t));
                AI.ik.solvers.rightHand.maintainRotationWeight =
                    Mathf.Lerp(originalMaintainRotationWeight, 0, t);

                yield return null;
            }
        }
    }
}