using Gameplay.Util;
using Hands.SinglePlayer.EnemyAI.StatefulRefactor.BaseState;
using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI.StatefulRefactor.NinjaStates
{
    public class NinjaOutOfPlayState : BaseAIState<NinjaOutOfPlayArgs>
    {
        public override int State => NinjaStruct.OutOfPlay;
        public NinjaOutOfPlayState(DodgeballAI ai, AIStateController controller, NinjaOutOfPlayArgs args) : base(ai, controller, args)
        {
        }

        public override void OnTriggerExit(Collision col)
        {
        }

        public override void EnterState()
        {
            Debug.Log("Ninja out of play!");
            controller.UnsubscribeRolling();
            if (AI.hasBall) AI.DropBall();
            AI.ik.solvers.rightHand.SetIKPositionWeight(0);
            AI.ik.solvers.rightHand.SetIKRotationWeight(0);
            AI.ik.solvers.lookAt.SetLookAtWeight(0);
            AI.animator.SetFloat(DodgeballAI._SXAxis, 0);
            AI.animator.SetFloat(DodgeballAI._SYAxis, 0);
            TimerManager.AddTimer(2.5f, AI.TriggerRespawn);
        }
        public override void FixedUpdate()
        {
        }

        public override void ExitState()
        {
        }

        public override void UpdateState()
        {
        }

        public override void OnTriggerEnter(Collider collider)
        {
        }
    }
}