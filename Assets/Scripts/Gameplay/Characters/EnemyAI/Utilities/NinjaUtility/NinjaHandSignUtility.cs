using System.Threading;
using Hands.SinglePlayer.EnemyAI;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Multiplayer.SinglePlayer.EnemyAI.Utilities
{
    public class NinjaHandSignUtility : Utility<NinjaHandSignUtilityArgs>, IUtility
    {
        private CancellationTokenSource _cancellationTokenSource;
        public bool active => _isHandSignActive;

        private static readonly int _SSigning = Animator.StringToHash("Signing");
        private bool _isHandSignActive;
        private float _nextAvailableTime = -Mathf.Infinity;
        private readonly object _lock = new object();

        private NinjaAgent ninja;

        public NinjaHandSignUtility(NinjaHandSignUtilityArgs args, DodgeballAI ai) : base(args, AIState.Special)
        {
            ninja = ai as NinjaAgent;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public override float Execute(DodgeballAI ai)
        {
            return -1f;
        }

        public override float Roll(DodgeballAI ai)
        {
            if (Time.time < args.nextHandSignTime) return 0f;

            var roll = Random.Range(1, 100);
            return roll > args.handSignDebugRoll ? 1f : 0f;
        }

        public CancellationToken GetCancellationToken()
        {
            return _cancellationTokenSource.Token;
        }
    }
}