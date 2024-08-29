using System.Collections;
using Hands.SinglePlayer.EnemyAI.StatefulRefactor.BaseState;
using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI.StatefulRefactor.NinjaStates
{
    public class NewSubstitutionState : BaseAIState<SubstitutionUtilityArgs>
    {
        public override int State => NinjaStruct.Substitution;
        public NewSubstitutionState(DodgeballAI ai, AIStateController controller, SubstitutionUtilityArgs args) : base(ai, controller, args)
        {
        }
        private void InitializeSubstitution()
        {
            Args.collider.enabled = false;
            
            if (!AI) return;
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
            AI.aiAvatar.SetActive(false);
        }
        private IEnumerator LerpSubstitutionMovement()
        {
            var exitPoint = AI.transform.position + (Args.stepDirection * Args.stepDistance);
            exitPoint.y = AI.transform.position.y;

            var t = 0f;
            while (t < 1)
            {
                t = Mathf.Clamp01(t + (Time.deltaTime / Args.stepDuration));

                if (t > 0.2f && AI.aiAvatar.activeInHierarchy)
                {
                    AI.ik.solvers.leftHand.SetIKPositionWeight(0);
                    AI.ik.solvers.leftHand.SetIKRotationWeight(0);
                    AI.transform.position = exitPoint;
                }
                else
                {
                    Args.colorLerp.lerpValue = Mathf.Lerp(1, 0, t + 0.8f);
                }

                yield return null;
            }
            
            Args.entryEffect.SetActive(true);
            Args.colorLerp.lerpValue = 1;
            AI.aiAvatar.SetActive(true);
            
            exitPoint += Args.stepDirection * (Args.stepDistance / 4);
            exitPoint.y = AI.transform.position.y;
            
            t = 0;
            
            while (t < 1f)
            {
                t = Mathf.Clamp01(t + (Time.deltaTime / Args.rentryDuration));

                    AI.transform.position = Vector3.Lerp(AI.transform.position, exitPoint, 
                        Args.exitCurve.Evaluate(t));
                    
                    Args.colorLerp.lerpValue = Mathf.Lerp(1, 0, t);
                    yield return null;
            }

            AI.transform.position = exitPoint;
            Args.entryEffect.SetActive(false);
            ChangeState(StateStruct.Move);
        }
        public override void OnTriggerExit(Collision col)
        {
        }

        public override void FixedUpdate()
        {
        }

        public override void EnterState()
        {
            if (active) return;
            active = true;
            InitializeSubstitution();
            AI.StartCoroutine(LerpSubstitutionMovement());
        }

        public override void ExitState()
        {
            active = false;
        }

        public override void UpdateState()
        {
        }

        public override void OnTriggerEnter(Collider collider)
        {
        }
    }
}