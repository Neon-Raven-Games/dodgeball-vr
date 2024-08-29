using System.Collections.Generic;
using Gameplay.Util;
using UnityEngine;

namespace Gameplay.InGameEvents
{
    [DisallowMultipleComponent]
    public class PhaseManager : MonoBehaviour
    {
        [SerializeField] protected List<Actor> playerTeamAi;
        protected readonly Dictionary<BattlePhase, PhaseEvent> phases = new();
        
        private BattlePhase _currentPhaseIndex;
        private PhaseEvent _currentPhase;
        private static PhaseManager _instance;
        public static bool phasing;

        private void OnDisable() => TimerManager.ClearTimers();
        
        public bool GameOver() => _currentPhase.GameOver();
        
        public static void DecreaseTeamLife(Team team)
        {
            _instance._currentPhase.DecreaseLife(team);
            if (_instance._currentPhase.IsComplete()) Debug.Log("Phase Complete");
        }

        private void Start()
        {
            _instance = this;
            Initialize();
            if (phases.Count <= 0)
            {
                Debug.LogError("Phase initialization has failed.");
                return;
            }
            
            phases[_currentPhaseIndex].StartPhase();
            _currentPhase = phases[_currentPhaseIndex];
            phasing = true;
        }
        
        public virtual void Initialize()
        {
            
        }
        
        private void Update()
        {
            if (_currentPhase == null) return;

            if (GameOver())
            {
                Debug.Log("Game Over");
                return;
            }
            TimerManager.Update();

            if (ShouldTransitionToNextPhase()) TransitionToNextPhase();
        }
        
        private bool ShouldTransitionToNextPhase()
        {
            
            return _currentPhase.IsComplete() && phases.Count > 1;
        }

        private void TransitionToNextPhase()
        {
            // todo, we should make the timer abstract from this system.
            // this will halt every timer in the game if we wanna reuse
            TimerManager.ClearTimers();

            
            if (_currentPhaseIndex == BattlePhase.Lackey)
            {
                _currentPhase.ExitPhase();
                _currentPhaseIndex = BattlePhase.Boss;
                _currentPhase = phases[_currentPhaseIndex];
                StartPhase();
            }
            else
            {
                _currentPhaseIndex = BattlePhase.Lackey;
                _currentPhase = phases[_currentPhaseIndex];
                StartPhase();
            }
        }

        private void StartPhase()
        {
            _currentPhase.StartPhase();
        }

        public string GetCurrentPhase()
        {
            if (_currentPhase == null) return "No phase initialized.";
            return _currentPhase.phase.ToString();
        }

        public string GetTeamTwoLives()
        {
            if (_currentPhase == null) return "N/A";
            return _currentPhase.teamTwoLives.ToString();
        }
        
        public string GetTeamOneLives()
        {
            if (_currentPhase == null) return "N\\A";
            return _currentPhase.teamOneLives.ToString();
        }

        // can we make a decent way for us to define phase lengths here, and then return them from the timer manager?
        public float GetRemainingTime()
        {
            return 0f;
        }

        public static bool CanSpawnTeam(Team team)
        {
            if (team == Team.TeamOne) return _instance._currentPhase.teamOneLives >= 3;
            return _instance._currentPhase.teamTwoLives >= 3;
        }
    }
}