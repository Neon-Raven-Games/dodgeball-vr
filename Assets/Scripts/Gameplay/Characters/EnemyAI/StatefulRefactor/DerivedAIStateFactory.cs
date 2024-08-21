using System;
using Hands.SinglePlayer.EnemyAI.StatefulRefactor.BaseState;
using Hands.SinglePlayer.EnemyAI.StatefulRefactor.NinjaStates;
using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI.StatefulRefactor
{
    public static class DerivedAIStateFactory
    {
        public static IDerivedAIState CreateState<TEnum>(DodgeballAI ai, DerivedAIStateController<TEnum> controller, TEnum state, UtilityArgs args) where TEnum : Enum
        {
            if (state is NinjaState ninjaState) return GetState(ai, controller as DerivedAIStateController<NinjaState>, ninjaState, args);
            Debug.LogError($"TEnum of {state.GetType()} is not found in abstract factory");
            return null;
        }

        private static IDerivedAIState GetState(DodgeballAI ai, DerivedAIStateController<NinjaState> controller, NinjaState state, UtilityArgs args) =>
            state switch
            {
                NinjaState.Default => new DefaultState(ai, controller, args),
                NinjaState.HandSign => new HandSignState(ai,controller, args as NinjaHandSignUtilityArgs),
                NinjaState.Substitution => new SubstitutionState(ai, controller, args as SubstitutionUtilityArgs),
                NinjaState.ShadowStep => new ShadowStepState(ai, controller, args as ShadowStepUtilityArgs),
                NinjaState.FakeOut => new FakeOutState(ai, controller, args as FakeoutUtilityArgs),
                NinjaState.SmokeBomb => new SmokeBombState(ai, controller, args as SmokeBombUtilityArgs),
                _ => null
            };
    }
}