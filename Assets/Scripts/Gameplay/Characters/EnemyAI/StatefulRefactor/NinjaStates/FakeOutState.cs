using System;
using Cysharp.Threading.Tasks;
using Hands.SinglePlayer.EnemyAI.StatefulRefactor.BaseState;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Hands.SinglePlayer.EnemyAI.StatefulRefactor.NinjaStates
{
    public class FakeOutState : DerivedAIState<NinjaState, FakeoutUtilityArgs>
    {
        public override NinjaState State => NinjaState.FakeOut;
        private NinjaAgent _ninja;
        public FakeOutState(DodgeballAI ai, DerivedAIStateController<NinjaState> controller,
            FakeoutUtilityArgs args) : base(ai, controller, args)
        {
            _ninja = ai as NinjaAgent;
            args.nextRollTime = Time.time + Random.Range(args.rollIntervalMin, args.rollIntervalMax);
        }

        public override void EnterState()
        {
            base.EnterState();
            AI.hasBall = true;
            AI.hasSpecials = false;
            RunFakeOutAppearEffect().Forget();
        }

        public override void ExitState()
        {
            active = false;
            AI.hasSpecials = true;
        }

        public override void UpdateState()
        {
            
        }
        
        private async UniTaskVoid RunFakeOutAppearEffect()
        {
            AI.rightBallIndex.SetBallType(BallType.Dodgeball);
            var dodgeball = AI.rightBallIndex._currentDodgeball;
            dodgeball.transform.localScale = Vector3.zero;
            var time = 0f;
            try
            {

                while (time < 1 && !GetCancellationToken().IsCancellationRequested)
                {
                    dodgeball.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, time);
                    time += Time.deltaTime / Args.entryDuration;
                    await UniTask.Yield();
                }

                if (GetCancellationToken().IsCancellationRequested)
                {
                    dodgeball.transform.localScale = Vector3.one;
                    dodgeball.gameObject.SetActive(false);
                    AI.rightBallIndex._currentDodgeball = null;
                    ChangeState(NinjaState.Default);
                    active = false;
                    return;
                }
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            dodgeball.transform.localScale = Vector3.one;
            Args.fakeoutBall.gameObject.SetActive(true);
            AI.PickUpBall(Args.fakeoutBall);
            AI.SetPossessedBall(Args.fakeoutBall);
            Args.fakeoutBall.gameObject.SetActive(false);
            Args.entryEffect.SetActive(false);
            await UniTask.Yield();
            ChangeState(NinjaState.Default);
        }
        public override void OnTriggerEnter(Collider collider)
        {
        }

        public override void OnTriggerExit(Collision col)
        {
        }

        public override void FixedUpdate()
        {
        }
    }
}