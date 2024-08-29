using System;
using System.Collections.Generic;
using System.Reflection;
using Gameplay.Characters.EnemyAI.Utilities.UtilityRefactor;
using Hands.SinglePlayer.EnemyAI.StatefulRefactor.BaseState;
using Hands.SinglePlayer.EnemyAI.StatefulRefactor.States;
using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI.StatefulRefactor
{
    public class AIStateController
    {
        public IDerivedAIState currentState;
        public int State;
        public Dictionary<int, IDerivedAIState> states = new();
        private int ballLayer = LayerMask.NameToLayer("Ball");
        private StateMatrix _matrix;
        private Dictionary<int, string> _stateMap;

        protected DodgeballAI AI;
        // todo, this needs to only be there in the editor
        public AIStateController()
        {
            _stateMap = new Dictionary<int, string>();

            PopulateStateMap(typeof(StateStruct));
            PopulateStateMap(typeof(NinjaStruct));
        }
        private void PopulateStateMap(Type type)
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            foreach (var field in fields)
            {
                if (field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(int))
                {
                    int value = (int)field.GetRawConstantValue();
                    string name = field.Name;
                    _stateMap[value] = name;
                }
            }
        }
        public void SetAndStartStateMatrix(StateMatrix matrix, DodgeballAI ai)
        {
            AI = ai;
            _matrix = matrix;
            SubscribeRolling();
        }

        protected virtual void OnCalculationComplete(int state)
        {
            ChangeState(state);
        }
        public MoveUtilityArgs moveUtilityArgs;

        public void Initialize(int initialState)
        {
            State = initialState;
            currentState = states[initialState];
            currentState.EnterState();
            moveUtilityArgs = states[StateStruct.Move].GetArgs() as MoveUtilityArgs;
            moveState = states[StateStruct.Move] as MoveState;
        }

        public MoveState moveState;
        public virtual void ChangeState(int newState)
        {
            // lock us out of any accidental state changes in out of play
            if (State == StateStruct.OutOfPlay) return;
            if (State == newState) return;
            currentState.ExitState();
            currentState = states[newState];
            currentState.EnterState();
            State = newState;
        }

        public void FixedUpdate()
        {
            currentState.FixedUpdate();
        }

        public void UpdateState()
        {
            currentState.UpdateState();
        }
        
        public void OnTriggerEnter(Collider col)
        {
            currentState.OnTriggerEnter(col);
        }

        public void OnTriggerExit(Collision col)
        {
            currentState.OnTriggerExit(col);
        }

        public void AddState(int state, IDerivedAIState stateObject)
        {
            states.Add(state, stateObject);
        }

        public void SubscribeRolling()
        {
            _matrix.onCalculationComplete += OnCalculationComplete;
        }

        public void UnsubscribeRolling()
        {
            _matrix.onCalculationComplete -= OnCalculationComplete;
        }

        public DodgeballTargetModule targetModule;
        public void SetTargetModule(DodgeballTargetModule targeting)
        {
            targetModule = targeting;
        }

        public void Dispose()
        {
            UnsubscribeRolling();
            _matrix.StopCalculations();
            _matrix = null;
        }

        public string GetStateName()
        {
            _stateMap.TryGetValue(State, out string stateName);
            return stateName;
        }

        public void Rebind()
        {
            Initialize(StateStruct.Move);
        }
    }
}