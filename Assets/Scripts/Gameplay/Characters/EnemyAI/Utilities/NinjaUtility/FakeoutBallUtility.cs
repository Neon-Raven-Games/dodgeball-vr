using Hands.SinglePlayer.EnemyAI;
using Hands.SinglePlayer.EnemyAI.StatefulRefactor;
using UnityEngine;

public class FakeoutBallUtility : Utility<FakeoutUtilityArgs>, IUtility
{
    private DodgeballAI _ai;
    private NinjaAgent _ninja;
    public bool active;
    private float _nextRollTime;
    
    public FakeoutBallUtility(FakeoutUtilityArgs args, AIState state, DodgeballAI ai) : base(args, state)
    {
        _ai = ai;
        _ninja = ai as NinjaAgent;
    }


    public int State => NinjaStruct.FakeOut;

    public override float Execute(DodgeballAI ai)
    {
        return -1f;
    }

    public override float Roll(DodgeballAI ai)
    {
        if (_ai.IsOutOfPlay())
        {
            return -1f;
        }

        if (_ai.hasBall ||
            _ai.currentState == "PickUp" ||
            _ai.currentState == "Throw" ||
            _ai.currentState == "OutOfPlay")
        {
            
            return -1f;
        }
        
        if (Time.time > args.nextRollTime + 5)
        {
            args.nextRollTime = Time.time + Random.Range(args.rollIntervalMin, args.rollIntervalMax);
            return float.MaxValue;
        }
        return 0f;
    }
}
