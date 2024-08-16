using System;
using Cysharp.Threading.Tasks;
using Hands.SinglePlayer.EnemyAI;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Multiplayer.SinglePlayer.EnemyAI.Utilities
{
    public class NinjaHandSignUtility : Utility<NinjaHandSignUtilityArgs>, IUtility
    {
        public bool active => _isHandSignActive;
        
        private static readonly int _SSigning = Animator.StringToHash("Signing");
        private bool _isHandSignActive;
        private float _nextAvailableTime = -Mathf.Infinity;

        public NinjaHandSignUtility(NinjaHandSignUtilityArgs args, DodgeballAI ai) : base(args,
            DodgeballAI.AIState.Special)
        {
        }

        public override float Execute(DodgeballAI ai)
        {
            if (_isHandSignActive || Time.time < _nextAvailableTime) return 0f;
            
            var rand = Random.Range(1, 100);
            if (rand > args.handSignDebugRoll) return 0f;
            
            _isHandSignActive = true;
            args.collider.enabled = true;
            
            args.ik.solvers.leftHand.target = args.handSignTarget;
            args.handAnimator.SetBool(_SSigning, true);
            
            LerpToHandSignPositionAndRotation(0.25f).Forget();
            HandSignTimer().Forget();

            return 1f;
        }
        private async UniTask LerpToHandSignPositionAndRotation(float duration)
        {
            float elapsedTime = 0f;
    
            while (elapsedTime < duration)
            {
                float t = Mathf.Clamp01(elapsedTime / duration);

                args.ik.solvers.leftHand.SetIKPosition(args.handSignTarget.position);
                args.ik.solvers.leftHand.SetIKPositionWeight(Mathf.Lerp(0, 0.8f, t));
                args.ik.solvers.leftHand.SetIKRotation(args.handSignTarget.rotation);
                args.ik.solvers.leftHand.SetIKRotationWeight(Mathf.Lerp(0, 0.8f, t));

                elapsedTime += Time.deltaTime;
                await UniTask.Yield();
            }

            args.ik.solvers.leftHand.SetIKPositionWeight(0.8f);
            args.ik.solvers.leftHand.SetIKRotationWeight(0.8f);
        }
        
        public override float Roll(DodgeballAI ai)
        {
            if (_isHandSignActive) return 0f;
            if (Time.time < _nextAvailableTime) return 0f;

            var roll = Random.Range(1, 100);
            return roll > args.handSignDebugRoll ? 1f : 0f;
        }

        private async UniTaskVoid HandSignTimer()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(args.handSignDuration));

            _isHandSignActive = false;
            args.collider.enabled = false;
            _nextAvailableTime = Time.time + args.handSignCooldown;
            args.ik.solvers.leftHand.SetIKPositionWeight(0);
            args.ik.solvers.leftHand.SetIKRotationWeight(0);
            args.handAnimator.SetBool(_SSigning, false);
        }

        public void Cooldown()
        {
            _isHandSignActive = false;
            args.collider.enabled = false;
            _nextAvailableTime = Time.time + args.handSignCooldown;
            args.handAnimator.SetBool(_SSigning, false);
        }
    }
}