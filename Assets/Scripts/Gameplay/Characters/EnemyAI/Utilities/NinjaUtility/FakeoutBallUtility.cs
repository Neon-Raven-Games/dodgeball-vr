using Cysharp.Threading.Tasks;
using Hands.SinglePlayer.EnemyAI;
using UnityEngine;

public class FakeoutBallUtility : Utility<FakeoutUtilityArgs>, IUtility
{
    private DodgeballAI _ai;
    public bool active;
    private float _nextRollTime;
    
    public FakeoutBallUtility(FakeoutUtilityArgs args, AIState state, DodgeballAI ai) : base(args, state)
    {
        _ai = ai;
    }
    
    
    public override float Execute(DodgeballAI ai)
    {
        if (active) return 0f;
        active = true;
        args.entryEffect.SetActive(true);
        RunFakeoutAppearEffect().Forget();
        _ai._possessedBall = args.fakeoutBall;
        return 0f;
    }

    private async UniTaskVoid RunFakeoutAppearEffect()
    {
        args.leftHandIndex.SetBallType(BallType.Dodgeball);
        var dodgeball = args.leftHandIndex._currentDodgeball;
        dodgeball.transform.localScale = Vector3.zero;
        var time = 0f;

        while (time < 1)
        {
            _ai.currentState = AIState.Special;
            dodgeball.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, time);
            time += Time.deltaTime / args.entryDuration;
            await UniTask.Yield();
        }
        dodgeball.transform.localScale = Vector3.one;
        args.fakeoutBall.gameObject.SetActive(true);
        _ai.PickUpBall(args.fakeoutBall);
        args.fakeoutBall.gameObject.SetActive(false);
        args.entryEffect.SetActive(false);
        await UniTask.Yield();
        active = false;
        _ai.currentState = AIState.Throw;
    }

    public override float Roll(DodgeballAI ai)
    {
        if (active) return 1f;
        if (_ai.hasBall) return 0f;
        if (_ai.currentState == AIState.Move && Time.time > _nextRollTime + 5)
        {
            _nextRollTime = Time.time + Random.Range(args.rollIntervalMin, args.rollIntervalMax);
            return float.MaxValue;
        }
        
        return 0f;
    }
}
