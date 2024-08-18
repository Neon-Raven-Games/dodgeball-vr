using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Hands.SinglePlayer.EnemyAI;
using UnityEngine;

namespace Multiplayer.SinglePlayer.EnemyAI.Utilities
{
    public class SubstitutionUtility : Utility<SubstitutionUtilityArgs>, IUtility
    {
        private readonly Animator _animator;
        private readonly DodgeballAI _ai;
        public bool inSequence => args.sequencePlaying;

        public SubstitutionUtility(SubstitutionUtilityArgs args, DodgeballAI.AIState state, DodgeballAI ai) : base(args,
            state)
        {
            _ai = ai;
            _animator = ai.animator;
        }

        public void BallInTrigger()
        {
            args.ballInTrigger = true;
        }

        public override float Execute(DodgeballAI ai)
        {
            if (_ai.currentState == DodgeballAI.AIState.PickUp || args.sequencePlaying)
                return 0;
            args.sequencePlaying = true;
            ShadowStepMove();
            return 1f;
        }

        public override float Roll(DodgeballAI ai)
        {
            if (_ai.currentState == DodgeballAI.AIState.Special)
                return 0;
            if (args.sequencePlaying) return 0;
            if (args.ballInTrigger) return float.MaxValue;
            return 0f;
        }

        public void ShadowStepMove()
        {
            Substitution().Forget();
        }

        private async UniTaskVoid Substitution()
        {
            if (!_ai)
            {
                Debug.Log("Ai was null and broke");
                return;
            }

            args.sequencePlaying = true;
            InitializeSubstitution();

            await LerpSubstitutionMovement();
            await UniTask.Yield();
            _ai.currentState = DodgeballAI.AIState.Move;
            args.sequencePlaying = false;
            args.ballInTrigger = false;
        }

        private async Task LerpSubstitutionMovement()
        {
            var exitPoint = _ai.transform.position + (args.stepDirection * args.stepDistance);
            exitPoint.y = _ai.transform.position.y;

            var t = 0f;
            while (t < 1)
            {
                t = Mathf.Clamp01(t + (Time.deltaTime / args.stepDuration));

                if (t > 0.2f && args.aiAvatar.activeInHierarchy)
                {
                    args.aiAvatar.SetActive(false);
                    args.ik.solvers.leftHand.SetIKPositionWeight(0);
                    args.ik.solvers.leftHand.SetIKRotationWeight(0);
                    _ai.transform.position = exitPoint;
                }
                else
                {
                    args.colorLerp.lerpValue = Mathf.Lerp(1, 0, t + 0.8f);
                }

                await UniTask.Yield();
            }

            args.entryEffect.SetActive(true);
            args.colorLerp.lerpValue = 1;
            args.aiAvatar.SetActive(true);
            
            exitPoint += args.stepDirection * (args.stepDistance / 4);
            exitPoint.y = _ai.transform.position.y;
            
            t = 0;
            args.sequencePlaying = false;
            while (t < 0.9f)
            {
                t = Mathf.Clamp01(t + (Time.deltaTime / args.rentryDuration));

                    _ai.transform.position = Vector3.Lerp(_ai.transform.position, exitPoint, 
                        args.exitCurve.Evaluate(t));
                    
                    args.colorLerp.lerpValue = Mathf.Lerp(1, 0, t);
                await UniTask.Yield();
            }

            _ai.transform.position = exitPoint;
            args.entryEffect.SetActive(false);
        }


        private void InitializeSubstitution()
        {
            _ai.currentState = DodgeballAI.AIState.Special;
            args.collider.enabled = false;
            _ai.SetOutOfPlay(false);
            args.ballInTrigger = false;

            _animator.SetTrigger(AIAnimationHelper.SSpecialTwo);
            args.stepDirection = new Vector3(Random.Range(0f, 1f), 0, Random.Range(0f, 1f));
            
            args.floorSmoke.transform.position = _ai.transform.position + args.stepDirection * (args.stepDistance / 8);
            args.floorSmoke.SetActive(true);
            args.logEffect.SetActive(false);
            args.entryEffect.SetActive(false);
            var pos = _ai.transform.position;
            pos.y = args.logEffect.transform.position.y;
            args.logEffect.transform.position = pos;
            args.logEffect.SetActive(true);
        }

        public void Reset()
        {
            args.ballInTrigger = false;
            
        }
    }
}