using System.Collections.Generic;

namespace Multiplayer.SinglePlayer.EnemyAI.Utilities
{
    public class UtilityHandler
    {
        private readonly List<IUtility> _utilities = new();
        private IUtility _currentUtility;
        
        public void AddUtility(IUtility utility) 
        {
            _utilities.Add(utility);
        }

        public AIState GetState()
        {
            return _currentUtility.State;
        }
        
        public IUtility GetCurrentUtility()
        {
            return _currentUtility;
        }

        public IUtility EvaluateUtilityWithoutSpecial(DodgeballAI ai, out float score)
        {
            score = 0f;
            foreach (var utility in _utilities)
            {
                if (utility.State == AIState.Special) continue;
                
                var utilityValue = utility.Roll(ai);
                if (utilityValue > score)
                {
                    score = utilityValue;
                    _currentUtility = utility;
                }
            }

            return _currentUtility; 
        }
        
        public IUtility EvaluateUtility(DodgeballAI ai, out float score)
        {
            score = 0f;
            foreach (var utility in _utilities)
            {
                var utilityValue = utility.Roll(ai);
                if (utilityValue > score)
                {
                    score = utilityValue;
                    _currentUtility = utility;
                }
            }

            return _currentUtility;
        }
    }
}

public interface IUtility
{
    AIState State { get; }
    float Execute(DodgeballAI ai);
    float Roll(DodgeballAI ai);
}