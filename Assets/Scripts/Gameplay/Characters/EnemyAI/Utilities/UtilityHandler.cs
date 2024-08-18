using System.Collections.Generic;
using Hands.SinglePlayer.EnemyAI;
using UnityEngine;

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

        public DodgeballAI.AIState GetState()
        {
            return _currentUtility.State;
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
    DodgeballAI.AIState State { get; }
    float Execute(DodgeballAI ai);
    float Roll(DodgeballAI ai);
}