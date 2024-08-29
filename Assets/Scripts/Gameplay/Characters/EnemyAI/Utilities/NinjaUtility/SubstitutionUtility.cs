using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Hands.SinglePlayer.EnemyAI;
using Hands.SinglePlayer.EnemyAI.StatefulRefactor;
using UnityEngine;

namespace Multiplayer.SinglePlayer.EnemyAI.Utilities
{
    public class SubstitutionUtility : Utility<SubstitutionUtilityArgs>, IUtility
    {
        public SubstitutionUtility(SubstitutionUtilityArgs args, AIState state, DodgeballAI ai) : base(args,
            state)
        {
        }

        public int State => NinjaStruct.Substitution;

        public override float Execute(DodgeballAI ai)
        {
            return -1f;
        }

        public override float Roll(DodgeballAI ai)
        {
            return -1f;
        }
    }
}