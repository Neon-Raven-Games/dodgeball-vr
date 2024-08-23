using System;
using Cysharp.Threading.Tasks;
using Gameplay.Util;
using Hands.SinglePlayer.EnemyAI.StatefulRefactor.BaseState;
using UnityEngine;
using Random = System.Random;

namespace Hands.SinglePlayer.EnemyAI.StatefulRefactor.NinjaStates
{
    public class SmokeBombState : DerivedAIState<NinjaState, SmokeBombUtilityArgs>
    {
        private NinjaAgent ninja;
        public override NinjaState State => NinjaState.SmokeBomb;
        private readonly Vector3[] bezierPoints = new Vector3[4];
        
        public SmokeBombState(DodgeballAI ai, DerivedAIStateController<NinjaState> controller, SmokeBombUtilityArgs args) : base(ai, controller, args)
        {
            ninja = ai as NinjaAgent;
        }

        public override void OnTriggerExit(Collision col)
        {
        }

        public override void FixedUpdate()
        {
        }
         
        public override void EnterState()
        {
            if (AI.hasBall) AI.ThrowBall();
            AI.stayIdle = true;
            
            base.EnterState();
            DespawnToTrajectory().Forget();
        }
        
        private void InitializeSmokeBomb()
        {
            Args.shadowStepEffect.SetActive(true);
        }

        private async UniTaskVoid DespawnToTrajectory()
        {
            var currentTime = 0f;
            var duration = Args.playEffectDelay;
            
            await UniTask.Delay(TimeSpan.FromSeconds(UnityEngine.Random.Range(Args.despawnDelay, Args.despawnDelay + 0.4f)));
            InitializeSmokeBomb();
            while (currentTime < duration && !GetCancellationToken().IsCancellationRequested)
            {
                var t = Mathf.Clamp01(currentTime / duration);
                Args.colorLerp.lerpValue = Mathf.Clamp(t, 0, 0.8f);
                currentTime += Time.deltaTime;
                await UniTask.Yield();
            }
            
            Args.aiAvatar.SetActive(false);
            AI.transform.position = bezierPoints[2];
            
            Args.shadowStepEffect.SetActive(false);
            Args.colorLerp.lerpValue = 0;
            AI.transform.position = ninja.currentSmokeBombPosition;
        }

        public override void ExitState()
        {
            active = false;
            CancelTask();
        }

        public override void UpdateState()
        {
        }

        public override void OnTriggerEnter(Collider collider)
        {
        }
    }
}