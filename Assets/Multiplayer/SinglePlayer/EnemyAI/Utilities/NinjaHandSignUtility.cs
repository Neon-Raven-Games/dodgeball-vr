using System;
using Cysharp.Threading.Tasks;
using Hands.SinglePlayer.EnemyAI;
using Random = UnityEngine.Random;

namespace Multiplayer.SinglePlayer.EnemyAI.Utilities
{
    public class NinjaHandSignUtility : Utility<NinjaHandSignUtilityArgs>, IUtility
    {
        private readonly DodgeballAI _ai;
        private bool _isHandSignActive;

        public NinjaHandSignUtility(NinjaHandSignUtilityArgs args, DodgeballAI ai) : base(args,
            DodgeballAI.AIState.Special)
        {
            _ai = ai;
        }

        public override float Execute(DodgeballAI ai)
        {
            if (_isHandSignActive && args.collider.enabled) return 0f;
            _isHandSignActive = true;
            args.collider.enabled = true;
            HandSignTimer().Forget();
            // _ai.animator.SetTrigger(AIAnimationHelper.HandSign);
            
            // do we want any particle effects or is a sign enough?
            return 1f;
        }

        public override float Roll(DodgeballAI ai)
        {
            // add better logic for shadow stepping, if we have our hand symbol up, we can shadow step
            var roll = Random.Range(1, 100);
            if (roll > args.handSignDebugRoll && !_isHandSignActive)
            {
                return 1f;
            }

            return 0f;
        }

        private async UniTaskVoid HandSignTimer()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(args.handSignDuration));
            _isHandSignActive = false;
            args.collider.enabled = false;
        }
    }
}