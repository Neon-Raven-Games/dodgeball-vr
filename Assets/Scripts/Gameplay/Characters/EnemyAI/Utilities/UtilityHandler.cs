using System;
using System.Collections.Generic;
using Hands.SinglePlayer.EnemyAI;
using Hands.SinglePlayer.EnemyAI.StatefulRefactor;
using Hands.SinglePlayer.EnemyAI.Utilities;

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

        public int GetState()
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
                if (utility.State > 11) continue;

                var utilityValue = utility.Roll(ai);
                if (utilityValue > score)
                {
                    score = utilityValue;
                    _currentUtility = utility;
                }
            }

            return _currentUtility;
        }

        public IUtility GetUtility(int state)
        {
            return _utilities.Find(utility => utility.State == state);
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

        public static UtilityHandler Create(DodgeballAI ai, params UtilityArgs[] utilities)
        {
            var handler = new UtilityHandler();
            foreach (var util in utilities)
            {
                var iutil = handler.CreateUtility(util, ai);
                handler.AddUtility(iutil);
            }

            return handler;
        }

        private IUtility CreateUtility(UtilityArgs args, DodgeballAI ai)
        {
            switch (args)
            {
                case MoveUtilityArgs utilityArgs:
                    var move = new MoveUtility(utilityArgs);
                    move.Initialize(ai.friendlyTeam.playArea, ai.team);
                    return move;
                case PickUpUtilityArgs utilityArgs:
                    var pickup = new PickUpUtility(utilityArgs, ai);
                    pickup.Initialize(ai.friendlyTeam.playArea, ai.team);
                    return pickup;
                case ThrowUtilityArgs utilityArgs:
                    var thrw = new ThrowUtility(utilityArgs);
                    thrw.Initialize(ai.friendlyTeam.playArea, ai.team);
                    return thrw;
                case OutOfPlayUtilityArgs utilityArgs:
                    var oop = new OutOfPlayUtility(utilityArgs);
                    oop.Initialize(ai.friendlyTeam.playArea, ai.team);
                    return oop;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            // todo implement factory helper
            // if we populate a map of the utilities, or have a ninja state on them, we can pass the utility
        }
    }
}

public interface IUtility
{
    int State { get; }
    float Execute(DodgeballAI ai);
    float Roll(DodgeballAI ai);
}