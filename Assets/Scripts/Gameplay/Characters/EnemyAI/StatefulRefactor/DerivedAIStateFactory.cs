using System;
using Hands.SinglePlayer.EnemyAI.StatefulRefactor.BaseState;
using Hands.SinglePlayer.EnemyAI.StatefulRefactor.NinjaStates;
using Hands.SinglePlayer.EnemyAI.StatefulRefactor.States;
using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI.StatefulRefactor
{
    // ReSharper disable InconsistentNaming
    
    public readonly struct StateStruct
    {
        public const int Idle = 0;
        public const int Move = 1;
        public const int Throw = 2;
        public const int BackOff = 3;
        public const int PickUp = 4;
        public const int OutOfPlay = 5;
        public const int Possession = 6;
        public const int Dodge = 7;
        public const int Catch = 8;
    }

    public readonly struct NinjaStruct
    {
        public const int SmokeBomb = 12;
        public const int HandSign = 13;
        public const int Substitution = 14;
        public const int ShadowStep = 15;
        public const int FakeOut = 16;
        public const int OutOfPlay = 17;
    }
    
    // ReSharper restore InconsistentNaming


    public static class AIStateMachineFactory
    {
        public static AIStateController CreateStateMachine(DodgeballAI ai, params UtilityArgs[] args)
        {
            var controller = new AIStateController();
            if (ai is NinjaAgent) controller = new NinjaStateController();
            
            foreach (var arg in args)
            {
                var state = CreateState(ai, controller, arg, arg.state);
                controller.AddState(arg.state, state);
            }
            controller.Initialize(StateStruct.Move);
            return controller;
        }

        public static IDerivedAIState CreateState(DodgeballAI ai, AIStateController controller, UtilityArgs args, int state) =>
            state switch
            {
                StateStruct.Idle => new IdleState<MoveUtilityArgs>(ai, controller, args as MoveUtilityArgs),
                StateStruct.Move => new MoveState(ai, controller, args as MoveUtilityArgs),
                StateStruct.Throw => new ThrowState(ai, controller, args as ThrowUtilityArgs),
                StateStruct.BackOff => new BackOffState(ai, controller, args),
                StateStruct.PickUp => new PickUpState(ai, controller, args as PickUpUtilityArgs),
                StateStruct.OutOfPlay => new OutOfPlayState(ai, controller, args as OutOfPlayUtilityArgs),
                
                // ninjas
                NinjaStruct.Substitution => new NewSubstitutionState(ai, controller, args as SubstitutionUtilityArgs),
                NinjaStruct.ShadowStep => new NewShadowStepState(ai, controller, args as ShadowStepUtilityArgs),
                NinjaStruct.FakeOut => new NewFakeOutState(ai, controller, args as FakeoutUtilityArgs),
                NinjaStruct.OutOfPlay => new NinjaOutOfPlayState(ai, controller, args as NinjaOutOfPlayArgs),
                _ => null
            };
    }

    public static class DerivedAIStateFactory
    {
        public static IDerivedAIState CreateState<TEnum>(DodgeballAI ai, DerivedAIStateController<TEnum> controller,
            TEnum state, UtilityArgs args) where TEnum : Enum
        {
            if (state is NinjaState ninjaState)
                return GetState(ai, controller as DerivedAIStateController<NinjaState>, ninjaState, args);
            Debug.LogError($"TEnum of {state.GetType()} is not found in abstract factory");
            return null;
        }

        private static IDerivedAIState GetState(DodgeballAI ai, DerivedAIStateController<NinjaState> controller,
            NinjaState state, UtilityArgs args) =>
            state switch
            {
                NinjaState.Default => new DefaultState(ai, controller, args),
                NinjaState.HandSign => new HandSignState(ai, controller, args as NinjaHandSignUtilityArgs),
                NinjaState.Substitution => new SubstitutionState(ai, controller, args as SubstitutionUtilityArgs),
                NinjaState.ShadowStep => new ShadowStepState(ai, controller, args as ShadowStepUtilityArgs),
                NinjaState.FakeOut => new FakeOutState(ai, controller, args as FakeoutUtilityArgs),
                NinjaState.SmokeBomb => new SmokeBombState(ai, controller, args as SmokeBombUtilityArgs),
                _ => null
            };
    }
}