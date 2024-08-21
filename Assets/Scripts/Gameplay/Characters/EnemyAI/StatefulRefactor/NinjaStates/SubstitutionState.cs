using Cysharp.Threading.Tasks;
using Hands.SinglePlayer.EnemyAI.StatefulRefactor.BaseState;
using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI.StatefulRefactor.NinjaStates
{
    public class SubstitutionState : DerivedAIState<NinjaState, SubstitutionUtilityArgs>
    {
        public override NinjaState State => NinjaState.Substitution;

        public SubstitutionState(DodgeballAI ai, DerivedAIStateController<NinjaState> controller, SubstitutionUtilityArgs args) : base(ai, controller, args)
        {
        }

        public override void EnterState()
        {
            AI.stayIdle = true;
            base.EnterState();
            Substitution().Forget();
        }
        private async UniTaskVoid Substitution()
        {
            InitializeSubstitution();
            await LerpSubstitutionMovement();
            await UniTask.Yield();
            ChangeState(NinjaState.Default);
        }
        public override void FixedUpdate()
        {

        }

        public override void ExitState()
        {
            Args.entryEffect.SetActive(false);
            Args.aiAvatar.SetActive(true);
            AI.stayIdle = false;
        }

        public override void UpdateState()
        {
        }

        public override void OnTriggerEnter(Collider collider)
        {
            // check to change state to substitution
        }

        public override void OnTriggerExit(Collision col)
        {

        }

        private void InitializeSubstitution()
        {
            Args.collider.enabled = false;
            AI.SetOutOfPlay(false);
            AI.animator.SetTrigger(AIAnimationHelper.SSpecialTwo);
            Args.stepDirection = new Vector3(Random.Range(0f, 1f), 0, Random.Range(0f, 1f));
            
            Args.floorSmoke.transform.position = AI.transform.position + Args.stepDirection * (Args.stepDistance / 8);
            Args.floorSmoke.SetActive(true);
            Args.logEffect.SetActive(false);
            Args.entryEffect.SetActive(false);
            var pos = AI.transform.position;
            pos.y = Args.logEffect.transform.position.y;
            Args.logEffect.transform.position = pos;
            Args.logEffect.SetActive(true);
            Args.aiAvatar.SetActive(false);
        }
        
        private async UniTask LerpSubstitutionMovement()
        {
            var exitPoint = AI.transform.position + (Args.stepDirection * Args.stepDistance);
            exitPoint.y = AI.transform.position.y;

            var t = 0f;
            while (t < 1 && !GetCancellationToken().IsCancellationRequested)
            {
                t = Mathf.Clamp01(t + (Time.deltaTime / Args.stepDuration));

                if (t > 0.2f && Args.aiAvatar.activeInHierarchy)
                {
                    Args.ik.solvers.leftHand.SetIKPositionWeight(0);
                    Args.ik.solvers.leftHand.SetIKRotationWeight(0);
                    AI.transform.position = exitPoint;
                }
                else
                {
                    Args.colorLerp.lerpValue = Mathf.Lerp(1, 0, t + 0.8f);
                }

                await UniTask.Yield();
            }

            if (GetCancellationToken().IsCancellationRequested)
            {
                Args.colorLerp.lerpValue = 0;
                Debug.LogWarning("Substitution cancelled, exiting state.");
                ChangeState(NinjaState.Default);
                return;
            }
            
            Args.entryEffect.SetActive(true);
            Args.colorLerp.lerpValue = 1;
            Args.aiAvatar.SetActive(true);
            
            exitPoint += Args.stepDirection * (Args.stepDistance / 4);
            exitPoint.y = AI.transform.position.y;
            
            t = 0;
            
            while (t < 0.9f && !GetCancellationToken().IsCancellationRequested)
            {
                t = Mathf.Clamp01(t + (Time.deltaTime / Args.rentryDuration));

                    AI.transform.position = Vector3.Lerp(AI.transform.position, exitPoint, 
                        Args.exitCurve.Evaluate(t));
                    
                    Args.colorLerp.lerpValue = Mathf.Lerp(1, 0, t);
                await UniTask.Yield();
            }

            AI.transform.position = exitPoint;
            Args.entryEffect.SetActive(false);
            ChangeState(NinjaState.Default);
        }
    }
}