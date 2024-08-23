using System;
using System.Collections.Generic;
using Hands.SinglePlayer.EnemyAI.StatefulRefactor.BaseState;
using JetBrains.Annotations;
using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI.StatefulRefactor
{
    public class DerivedAIStateController<TEnum> where TEnum : Enum
    {
        private TEnum defaultState;
        public IDerivedAIState currentState;
        public TEnum State;
        public Dictionary<TEnum, IDerivedAIState> states = new Dictionary<TEnum, IDerivedAIState>();
        public Action<TEnum> onDefaultRestored;
        public DerivedAIStateController()
        {
        }
        
        public void Initialize(TEnum initialState)
        {
            defaultState = initialState;
            State = initialState;
            currentState = states[initialState];
            currentState.EnterState();
            onDefaultRestored?.Invoke(initialState);
        }
        
        public void ChangeState(TEnum newState)
        {
            if (EqualityComparer<TEnum>.Default.Equals(newState, State)) 
                return;
            
            currentState.ExitState();
            currentState = states[newState];
            currentState.EnterState();
            State = newState;
            
            if (EqualityComparer<TEnum>.Default.Equals(newState, defaultState))
                onDefaultRestored?.Invoke(newState);
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
        
        public void AddState(TEnum state, IDerivedAIState stateObject)
        {
            states.Add(state, stateObject);
        }

        public void CleanUp()
        {
            foreach(var state in states)
            {
                state.Value.CleanUp();
            }
        }
    }
}