using System;
using Cysharp.Threading.Tasks;
using Gameplay.Util;
using Hands.SinglePlayer.EnemyAI.StatefulRefactor.BaseState;
using UnityEngine;

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
            AI.stayIdle = true;
            base.EnterState();
            InitializeSmokeBomb();
            DespawnToTrajectory().Forget();
        }
        
        private void InitializeSmokeBomb()
        {
            Args.shadowStepEffect.SetActive(true);
            CalculateBezierPath(ninja.currentSmokeBombPosition);
        }

        private void CalculateBezierPath(Vector3 pos)
        {
            pos.y = AI.transform.position.y;
            var midPoint = (pos + AI.transform.position) / 2;
            midPoint.y += Args.jumpHeight;
            bezierPoints[0] = AI.transform.position;
            bezierPoints[1] = midPoint;
            bezierPoints[2] = pos;
        }

        private async UniTaskVoid DespawnToTrajectory()
        {
            await UniTask.WaitForSeconds(Args.despawnDelay);
            Args.aiAvatar.SetActive(false);
            Args.trailRenderer.SetActive(true);

            var currentTime = 0f;
            var duration = Args.jumpSeconds;
            while (currentTime < duration && !GetCancellationToken().IsCancellationRequested)
            {
                var t = Mathf.Clamp01(currentTime / duration);
                AI.transform.position = Bezier.CalculateBezierPoint(t, bezierPoints[0], bezierPoints[1], bezierPoints[2]);
                currentTime += Time.deltaTime;
                await UniTask.Yield();
            }
            
            Args.shadowStepEffect.SetActive(false);
            Args.trailRenderer.SetActive(false);
            AI.transform.position = ninja.currentSmokeBombPosition;
        }

        public override void ExitState()
        {
            active = false;
            Args.aiAvatar.SetActive(true);
            AI.stayIdle = false;
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