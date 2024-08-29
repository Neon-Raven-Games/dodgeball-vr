using Hands.SinglePlayer.EnemyAI.StatefulRefactor.BaseState;
using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI.StatefulRefactor.States
{
    public class BackOffState : BaseAIState<UtilityArgs>
    {
        public override int State => StateStruct.Idle;
        public BackOffState(DodgeballAI ai, AIStateController controller, UtilityArgs args) : base(ai, controller, args)
        {
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

        public override void OnTriggerExit(Collision col)
        {
        }

    }
}