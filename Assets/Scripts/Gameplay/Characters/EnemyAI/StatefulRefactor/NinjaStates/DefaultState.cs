using Hands.SinglePlayer.EnemyAI.StatefulRefactor.BaseState;
using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI.StatefulRefactor.NinjaStates
{
    public class DefaultState : DerivedAIState<NinjaState, UtilityArgs>
    {
        public override NinjaState State => NinjaState.Default;
        public DefaultState(DodgeballAI ai, DerivedAIStateController<NinjaState> controller, UtilityArgs args) 
            : base(ai, controller, args)
        {
        }

        public override void EnterState()
        {
            ResetIKWeights();
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

        public override void FixedUpdate()
        {
        }
    }
}