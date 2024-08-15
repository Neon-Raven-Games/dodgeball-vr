using System;
using Cysharp.Threading.Tasks;
using Hands.SinglePlayer.EnemyAI;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Multiplayer.SinglePlayer.EnemyAI.Utilities
{
    public class NinjaHandSignUtility : Utility<NinjaHandSignUtilityArgs>, IUtility
    {
        private bool _isHandSignActive;
        private float _nextAvailableTime = -Mathf.Infinity;
        public bool active => _isHandSignActive;

        public NinjaHandSignUtility(NinjaHandSignUtilityArgs args, DodgeballAI ai) : base(args,
            DodgeballAI.AIState.Special)
        {
        }

        public override float Execute(DodgeballAI ai)
        {
            // Check if already active or if the cooldown is not over
            if (_isHandSignActive || Time.time < _nextAvailableTime)
            {
                return 0f;
            }
            var rand = Random.Range(1, 100);
            if (rand > args.handSignDebugRoll)
            {
                return 0f;
            }
            
            _isHandSignActive = true;
            args.collider.enabled = true;
            
            args.ik.solvers.leftHand.target = args.handSignTarget;
            args.handAnimator.SetBool("Signing", true);
            
            
            LerpToHandSignPositionAndRotation(0.25f).Forget();
            HandSignTimer().Forget();

            return 1f;
        }
        private async UniTask LerpToHandSignPositionAndRotation(float duration)
        {
            float elapsedTime = 0f;
    
            while (elapsedTime < duration)
            {
                // Calculate the current lerp factor
                float t = Mathf.Clamp01(elapsedTime / duration);

                // Lerp the IK position and rotation weights from 0 to 1
                args.ik.solvers.leftHand.SetIKPosition(args.handSignTarget.position);
                args.ik.solvers.leftHand.SetIKPositionWeight(Mathf.Lerp(0, 1, t));
                args.ik.solvers.leftHand.SetIKRotation(args.handSignTarget.rotation);
                args.ik.solvers.leftHand.SetIKRotationWeight(Mathf.Lerp(0, 1, t));

                elapsedTime += Time.deltaTime;
                await UniTask.Yield();
            }

            // Ensure final values are set to 1
            args.ik.solvers.leftHand.SetIKPositionWeight(1);
            args.ik.solvers.leftHand.SetIKRotationWeight(1);
        }
        public override float Roll(DodgeballAI ai)
        {
            if (_isHandSignActive) return 0f;
            // If the cooldown is not over, don't activate
            if (Time.time < _nextAvailableTime)
            {
                return 0f;
            }

            // Add randomness to determine if hand sign should activate
            var roll = Random.Range(1, 100);
            if (roll > args.handSignDebugRoll)
            {
                Debug.Log("Hand Sign Roll active");
                return 1f; // High score indicates we should activate the hand sign
            }

            return 0f;
        }

        private async UniTaskVoid HandSignTimer()
        {
            // Wait for the duration of the hand sign
            await UniTask.Delay(TimeSpan.FromSeconds(args.handSignDuration));

            Debug.Log("Hand Sign Timer complete");
            // Deactivate hand sign and reset for next use
            _isHandSignActive = false;
            args.collider.enabled = false;
            _nextAvailableTime = Time.time + args.handSignCooldown;
            args.ik.solvers.leftHand.SetIKPositionWeight(0);
            args.ik.solvers.leftHand.SetIKRotationWeight(0);
            args.handAnimator.SetBool("Signing", false);
        }
    }
}