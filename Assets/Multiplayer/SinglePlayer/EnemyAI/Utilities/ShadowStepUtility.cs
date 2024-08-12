using System;
using Cysharp.Threading.Tasks;
using Hands.SinglePlayer.EnemyAI;
using UnityEngine;

public class ShadowStepUtility : Utility<ShadowStepUtilityArgs>, IUtility
{
    private readonly Animator _animator;
    private readonly DodgeballAI _ai;
    private bool _shadowSteppingSequencePlaying;
    private bool _isShadowStepping;
    public bool ballInTrigger;
    
    public ShadowStepUtility(ShadowStepUtilityArgs args, DodgeballAI.AIState state, DodgeballAI ai) : base(args, state)
    {
        _ai = ai;
        _animator = ai.animator;
    }

    public override float Execute(DodgeballAI ai)
    {
        ShadowStepMove();
        return 1f;
    }

    public override float Roll(DodgeballAI ai)
    {
        if (_shadowSteppingSequencePlaying) return float.MaxValue;
        
        if (ballInTrigger)
        {
            return float.MaxValue;
        }
        return 0f;
    }
    
    public void ShadowStepMove()
    {
        if (_isShadowStepping) return;
        _shadowSteppingSequencePlaying = true;
        _isShadowStepping = true;
        _animator.SetTrigger(AIAnimationHelper.SSpecialOne);
        ShadowStepEnter().Forget();
    }

    private async UniTaskVoid ShadowStepExit()
    {
        var playerPosition = _ai.transform.position;
        var exitPoint = _ai.transform.TransformPoint(-args.stepDirection * args.stepDistance / 2);
        
        var exitTime = 0f;
        while (exitTime < 1)
        {
            _ai.transform.position = Vector3.Lerp(playerPosition, exitPoint, args.exitCurve.Evaluate(exitTime));
            exitTime += Time.deltaTime / args.exitDuration;
            await UniTask.Yield();
        }
        _shadowSteppingSequencePlaying = false;
    }
    
    private async UniTaskVoid ShadowStepEnter()
    {
        args.floorSmoke.transform.position = _ai.transform.position + args.stepDirection * (args.stepDistance / 8);
        args.floorSmoke.SetActive(true);
        var entryPoint = _ai.transform.TransformPoint(args.stepDirection * (args.stepDistance / 4));
        var start = _ai.transform.position;
        var entryTime = 0f;
        while (_isShadowStepping)
        {
            _ai.transform.position = Vector3.Lerp(start, entryPoint, args.entryCurve.Evaluate(entryTime));
            entryTime += Time.deltaTime / args.entrySpeed;
            await UniTask.Yield();
        }
    }
    
    /// <summary>
    /// Animation event called from the the last frame of the shadow step exit animation
    /// </summary>
    public void InitialShadowStepFinished()
    {
        if (!_isShadowStepping) return;
        _isShadowStepping = false;
        Reappear().Forget();
    }

    // todo, validate game object disable and re-enable working
    private async UniTaskVoid Reappear()
    {
        await UniTask.Yield();
        _ai.gameObject.SetActive(false);
        var targetPosition = _ai.transform.TransformPoint(args.stepDirection * args.stepDistance);
        targetPosition = ClampPositionToPlayArea(targetPosition, _ai.playArea, _ai.team);
        _ai.transform.position = targetPosition;
        await UniTask.Delay(TimeSpan.FromSeconds(args.stepDuration));
        args.entryEffect.SetActive(false);
        args.exitEffect.SetActive(true);
        _ai.gameObject.SetActive(true);
        args.floorSmoke.SetActive(false);
        _animator.Play(AIAnimationHelper.SSpecialOneExit);
        ShadowStepExit().Forget();
    }
}
