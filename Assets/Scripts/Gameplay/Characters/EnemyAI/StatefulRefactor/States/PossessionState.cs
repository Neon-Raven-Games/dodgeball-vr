using System.Threading.Tasks;
using Hands.SinglePlayer.EnemyAI.StatefulRefactor.BaseState;
using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI.StatefulRefactor.States
{
    public class PossessionState : BaseAIState<PossessionArgs>
    {
        public override int State { get; }
        
        public PossessionState(DodgeballAI ai, AIStateController controller, PossessionArgs args) : base(ai, controller, args)
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