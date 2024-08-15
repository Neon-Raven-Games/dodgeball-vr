using System;
using Cysharp.Threading.Tasks;
using Hands.SinglePlayer.EnemyAI;
using UnityEngine;

namespace Multiplayer.SinglePlayer.EnemyAI.Utilities
{
    public class SubstitutionUtility : Utility<ShadowStepUtilityArgs>, IUtility
    {
        private readonly Animator _animator;
        private readonly DodgeballAI _ai;
        internal bool _shadowSteppingSequencePlaying;
        private bool _isShadowStepping;
        public bool ballInTrigger;
        public Vector3 ballDirection;
        public Vector3 ballHitPoint;

        public SubstitutionUtility(ShadowStepUtilityArgs args, DodgeballAI.AIState state, DodgeballAI ai) : base(args,
            state)
        {
            _ai = ai;
            _animator = ai.animator;
        }

        public override float Execute(DodgeballAI ai)
        {
            if (_ai.currentState == DodgeballAI.AIState.Special ||
                _ai.currentState == DodgeballAI.AIState.Possession ||
                _ai.currentState == DodgeballAI.AIState.Throw ||
                _ai.currentState == DodgeballAI.AIState.PickUp)
                return 0;
            if (_shadowSteppingSequencePlaying) return 0;
            args.collider.enabled = false;
            _shadowSteppingSequencePlaying = true;
            Debug.Log("Execute sub");
            ShadowStepMove();
            ballInTrigger = false;
            _ai.SetOutOfPlay(false);
            return 1f;
        }

        public override float Roll(DodgeballAI ai)
        {
            if (_ai.currentState == DodgeballAI.AIState.Special ||
                _ai.currentState == DodgeballAI.AIState.Possession ||
                _ai.currentState == DodgeballAI.AIState.Throw ||
                _ai.currentState == DodgeballAI.AIState.PickUp)
                return 0;
            if (_shadowSteppingSequencePlaying) return 0;
            if (ballInTrigger) return float.MaxValue;
            return 0f;
        }

        public void ShadowStepMove()
        {
            _isShadowStepping = true;
            args.stepDirection = (_ai.transform.position - ballHitPoint).normalized;
            args.stepDirection.y = 0;
            _animator.SetTrigger(AIAnimationHelper.SSpecialTwo);
            ShadowStepEnter().Forget();
            args.ik.solvers.leftHand.SetIKPositionWeight(0);
            args.ik.solvers.leftHand.SetIKRotationWeight(0);
        }

        // this is not exiting properly
        private async UniTaskVoid ShadowStepExit()
        {
            if (!_ai)
            {
                Debug.Log("Ai was null and broke");
                return;
            }
            var playerPosition = _ai.transform.position;
            var exitPoint = _ai.transform.TransformPoint(args.stepDirection * args.stepDistance / 2);
            exitPoint.y = _ai.transform.position.y;

            var exitTime = 0f;
            while (exitTime < 1)
            {
                float t = Mathf.Clamp01(exitTime); // Clamp t between 0 and 1

                // Use the clamped t to evaluate the exit curve and Lerp the position
                _ai.transform.position = Vector3.Lerp(playerPosition, exitPoint, args.exitCurve.Evaluate(t));

                // Increment exitTime with respect to exitDuration
                exitTime += Time.deltaTime / args.exitDuration;
                await UniTask.Yield();
            }

            args.entryEffect.SetActive(false);
            await UniTask.Yield();
            
            _shadowSteppingSequencePlaying = false;
            ballInTrigger = false;
            
            await UniTask.Yield();
            _ai.currentState = DodgeballAI.AIState.Move;
        }

        private async UniTaskVoid ShadowStepEnter()
        {
            if (!_ai) return;
            
            args.floorSmoke.transform.position = _ai.transform.position + args.stepDirection * (args.stepDistance / 8);
            args.floorSmoke.SetActive(true);
            args.entryEffect.transform.position = ballHitPoint;
            args.entryEffect.SetActive(true); 
            // can we make the player look at the entry effect better?
            args.entryEffect.SetActive(true);

            var entryPoint = _ai.transform.TransformPoint(args.stepDirection * args.stepDistance / 2);
            entryPoint.y = _ai.transform.position.y;

            var start = _ai.transform.position;
            var entryTime = 0f;

            while (entryTime < args.stepDuration && _isShadowStepping)
            {
                if (!_ai) break;
                _ai.transform.LookAt(ballHitPoint);

                float t = Mathf.Clamp01(entryTime / args.stepDuration); // Clamping to ensure it stays between 0 and 1
                _ai.transform.position = Vector3.Lerp(start, entryPoint, t);

                entryTime += Time.deltaTime * args.entrySpeed; // Control the speed by multiplying with entrySpeed
                await UniTask.Yield();
            }
        
        }

        /// <summary>
        /// Animation event called from the the last frame of the shadow step exit animation
        /// </summary>
        public void InitialShadowStepFinished()
        {
            if (!_isShadowStepping) return;
            _isShadowStepping = false;
            Reappear().Forget();
            _ai.SetOutOfPlay(false);
        }

        // todo, validate game object disable and re-enable working
        private async UniTaskVoid Reappear()
        {
            await UniTask.Yield();
            args.aiAvatar.SetActive(false);
            await UniTask.Delay(TimeSpan.FromSeconds(args.stepDuration));

            args.aiAvatar.SetActive(true);
            args.floorSmoke.SetActive(false);
            ShadowStepExit().Forget();
        }
    }
}