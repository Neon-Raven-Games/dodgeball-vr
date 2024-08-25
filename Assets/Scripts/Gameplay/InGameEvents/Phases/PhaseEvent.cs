using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.InGameEvents
{
    public abstract class PhaseEvent : InGameEvent
    {
        public abstract BattlePhase phase { get; }
        public PhaseCurve phaseCurve;
        public List<InGameEvent> events;
        public List<Actor> phaseActors;

        public int maxLivesCount = 100;

        public int teamOneLives;
        public int teamTwoLives;
        
        public void DecreaseLife(Team team)
        {
            if (team == Team.TeamOne) teamOneLives--;
            else teamTwoLives--;
        }

        public override void InitializeEvent(InGameEvent newEvent, EventBalanceData balanceData)
        {
            eventLevel = balanceData.eventLevel;
            UpdatePhaseData();
        }

        public void UpdatePhaseData()
        {
            // teamOneLives = GetCurveValue(PhaseCurveType.TeamOneLives);
            // teamTwoLives = GetCurveValue(PhaseCurveType.TeamTwoLives);
            teamOneLives = 100;
            teamTwoLives = 100;
            Debug.Log($"Team one lives{teamOneLives}, TeamTwo Lives{teamTwoLives}");
            eventLevel++;
        }
       
        private int GetCurveValue(PhaseCurveType curveType)
        {
            var normalizedLevel = Mathf.Clamp01((eventLevel - 1f) / maxLivesCount - 1); 
            Debug.Log("Phase Level: " + this);
            switch (curveType)
            {
                case PhaseCurveType.TeamOneLives:
                    return (int) phaseCurve.teamOneLives.Evaluate(normalizedLevel);
                case PhaseCurveType.TeamTwoLives:
                    return (int) phaseCurve.teamTwoLives.Evaluate(normalizedLevel);
                default:
                    return 0;
            }
        }

        public abstract void StartPhase();

        public virtual bool GameOver() => teamOneLives <= 0;

        public virtual bool IsComplete()
        {
            // Debug.Log("team two lives: " + teamTwoLives);
            return teamTwoLives <= 0;
        }

        public override void InvokeEvent()
        {
        }

        public override void SimulateEvent()
        {
            Debug.Log("Simulation of phase event");
        }

        public virtual void ExitPhase()
        {
            
        }
    }
}