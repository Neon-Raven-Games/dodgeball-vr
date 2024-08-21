using System;
using Cysharp.Threading.Tasks;
using Hands.SinglePlayer.EnemyAI.StatefulRefactor.BaseState;
using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI.StatefulRefactor.NinjaStates
{
    public class HandSignState : DerivedAIState<NinjaState, NinjaHandSignUtilityArgs>
    {
        public override NinjaState State => NinjaState.HandSign;

        private static readonly int _SSigning = Animator.StringToHash("Signing");
        private float _currentHandLerpWeight;

        public HandSignState(DodgeballAI ai, DerivedAIStateController<NinjaState> controller,
            NinjaHandSignUtilityArgs args) : base(ai, controller, args)
        {
            args.nextHandSignTime = Time.time + args.handSignCooldown;
        }

        public override void EnterState()
        {
            AI.hasSpecials = false;
            base.EnterState();
            InitializeState();

            LerpToHandSignPositionAndRotation(0.25f, _currentHandLerpWeight, 0.8f).Forget();
            HandSignTimer().Forget();
        }


        public override void ExitState()
        {
            Cooldown();
        }

        public override void UpdateState()
        {
        }

        public override void FixedUpdate()
        {
        }

        public override void OnTriggerEnter(Collider collider)
        {
            if (!ColliderOnBallLayer(collider)) return;

            var db = collider.gameObject.GetComponent<DodgeBall>();
            if (HitWithBall(collider, db)) CancelTask();
        }

        private bool HitWithBall(Collider collider, DodgeBall db) =>
            db._ballState == BallState.Live && db._team != AI.team && !AI.IsColliderOwner(collider);

        public override void OnTriggerExit(Collision col)
        {
        }

        #region Control Flow

        private void Cooldown()
        {
            if (active) CancelTask();

            Args.collider.enabled = false;
            Args.nextHandSignTime = Time.time + Args.handSignCooldown + 2;
            Args.handAnimator.SetBool(_SSigning, false);
            active = false;

            LerpToHandSignPositionAndRotation(0.1f, _currentHandLerpWeight, 0f).Forget();
            AI.hasSpecials = true;
        }

        private void InitializeState()
        {
            Args.collider.enabled = true;
            Args.ik.solvers.leftHand.target = Args.handSignTarget;
            Args.handAnimator.SetBool(_SSigning, true);
            AssignIKWeight();
        }

        private void AssignIKWeight() =>
            _currentHandLerpWeight = Args.ik.solvers.leftHand.GetIKPositionWeight();

        #endregion

        #region Behavior
        
        private async UniTaskVoid HandSignTimer()
        {
            try
            {
                while (true)
                {
                    await UniTask.Yield(GetCancellationToken());
                    if (AI.hasBall)
                    {
                        await UniTask.WaitForSeconds(1.5f);
                        if (active && AI.hasBall)
                        {
                            ChangeState(NinjaState.ShadowStep);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                ChangeState(NinjaState.Substitution);
            }
        }

        private async UniTask LerpToHandSignPositionAndRotation(float duration, float fromValue, float toValue)
        {
            var elapsedTime = 0f;

            while (elapsedTime < duration && !GetCancellationToken().IsCancellationRequested)
            {
                var t = Mathf.Clamp01(elapsedTime / duration);

                Args.ik.solvers.leftHand.SetIKPosition(Args.handSignTarget.position);
                Args.ik.solvers.leftHand.SetIKPositionWeight(Mathf.Lerp(fromValue, toValue, t));
                Args.ik.solvers.leftHand.SetIKRotation(Args.handSignTarget.rotation);
                Args.ik.solvers.leftHand.SetIKRotationWeight(Mathf.Lerp(fromValue, toValue, t));

                elapsedTime += Time.deltaTime;
                await UniTask.Yield();
            }

            Args.ik.solvers.leftHand.SetIKPositionWeight(toValue);
            Args.ik.solvers.leftHand.SetIKRotationWeight(toValue);
        }

        #endregion
    }
}