using Hands.SinglePlayer.EnemyAI.StatefulRefactor.BaseState;
using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI.StatefulRefactor.States
{
    public class IdleState<T> : BaseAIState<T> where T : UtilityArgs
    {
        public IdleState(DodgeballAI ai, AIStateController controller, T args) : base(ai, controller, args) 
        {
        }

        public override int State { get; }

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