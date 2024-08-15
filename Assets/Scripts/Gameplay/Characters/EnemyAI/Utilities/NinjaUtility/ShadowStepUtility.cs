using System;
using Cysharp.Threading.Tasks;
using Hands.SinglePlayer.EnemyAI;
using UnityEngine;

public class ShadowStepUtility : Utility<ShadowStepUtilityArgs>, IUtility
{
    private readonly Animator _animator;
    private readonly DodgeballAI _ai;
    internal bool _shadowSteppingSequencePlaying;
    private bool _isShadowStepping;
    private bool _canShadowStep;

    public ShadowStepUtility(ShadowStepUtilityArgs args, DodgeballAI.AIState state, DodgeballAI ai) : base(args, state)
    {
        _ai = ai;
        _animator = ai.animator;
    }

    public override float Execute(DodgeballAI ai)
    {
        if (_shadowSteppingSequencePlaying) return 0;
        ShadowStepMove();
        return 1f;
    }

    private float lastShadowStepTime = -Mathf.Infinity;
    private float shadowStepCooldown = 10f; // 10 seconds cooldown

    public override float Roll(DodgeballAI ai)
    {

        if (_shadowSteppingSequencePlaying || _ai.currentState != DodgeballAI.AIState.Throw) return 0;

        float timeSinceLastStep = Time.time - lastShadowStepTime;
        if (timeSinceLastStep < shadowStepCooldown) return 0;
        
        var roll = UnityEngine.Random.Range(1, 100);
        _canShadowStep = roll > args.rollChance;

        if (!_shadowSteppingSequencePlaying && _ai.hasBall && _canShadowStep)
            ShadowStepMove();
        _canShadowStep = false;
        return 0;
    }

    public void ShadowStepMove()
    {
        if (_shadowSteppingSequencePlaying) return;
        _shadowSteppingSequencePlaying = true;
        lastShadowStepTime = Time.time;
        _isShadowStepping = true;
        _animator.SetTrigger(AIAnimationHelper.SSpecialOne);
        args.stepDirection = CalculateValidShadowStep();
        args.stepDirection.y = _ai.transform.position.y;
        ShadowStepEnter().Forget();
    }

    private Vector3 CalculateValidShadowStep()
    {
        Vector3[] possibleDirections =
        {
            _ai.transform.right,
            -_ai.transform.right,
            _ai.transform.forward + _ai.transform.right * 0.3f,
            _ai.transform.forward - _ai.transform.right * 0.3f,
            -_ai.transform.forward - _ai.transform.right * 0.3f
        };

        for (int i = 0; i < 5; i++) // Attempt a limited number of times
        {
            Vector3 direction = possibleDirections[UnityEngine.Random.Range(0, possibleDirections.Length)].normalized;
            Vector3 targetPosition = _ai.transform.TransformPoint(direction * args.stepDistance);
            targetPosition = ClampPositionToPlayArea(targetPosition, _ai.playArea, _ai.team);

            float distance = Vector3.Distance(targetPosition, _ai.transform.position);
            if (distance >= args.stepDistance * 0.9f)
            {
                return direction;
            }
        }

        return Vector3.left;
    }


    private async UniTaskVoid ShadowStepExit()
    {
        Debug.Log("Exit ShadowStep");
        var playerPosition = _ai.transform.position;
        var exitPoint = _ai.transform.TransformPoint(args.stepDirection * args.stepDistance);
        exitPoint.y = playerPosition.y;

        var exitTime = 0f;
        while (exitTime < 1)
        {
            _ai.transform.position = Vector3.Lerp(exitPoint, playerPosition, args.exitCurve.Evaluate(exitTime));
            exitTime += Time.deltaTime / args.exitDuration;
            await UniTask.Yield();
        }

        await UniTask.Yield();

        _shadowSteppingSequencePlaying = false;
        _ai.currentState = DodgeballAI.AIState.Move;
    }

    private async UniTaskVoid ShadowStepEnter()
    {
        if (!_ai) return;
        args.floorSmoke.transform.position = _ai.transform.position + args.stepDirection * (args.stepDistance / 8);
        args.floorSmoke.SetActive(true);
        var entryPoint = _ai.transform.TransformPoint(args.stepDirection * (args.stepDistance / 4));
        entryPoint.y = _ai.transform.position.y;
        var start = _ai.transform.position;
        var entryTime = 0f;
        while (_isShadowStepping)
        {
            if (!_ai) break;
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
        _ai.SetOutOfPlay(false);
        
        args.ik.solvers.leftHand.SetIKPositionWeight(0);
        args.ik.solvers.leftHand.SetIKRotationWeight(0);
    }

    private async UniTaskVoid Reappear()
    {
        await UniTask.Yield();
        _ai.SwitchBallSideToLeft();
        args.aiAvatar.SetActive(false);
        var targetPosition = _ai.transform.TransformPoint(args.stepDirection * args.stepDistance);
        targetPosition = ClampPositionToPlayArea(targetPosition, _ai.playArea, _ai.team);
        targetPosition.y = _ai.transform.position.y;
        _ai.transform.position = targetPosition;
        await UniTask.Delay(TimeSpan.FromSeconds(args.stepDuration));
        args.entryEffect.SetActive(false);
        args.exitEffect.SetActive(true);
        args.aiAvatar.SetActive(true);
        args.floorSmoke.SetActive(false);
        _animator.Play(AIAnimationHelper.SSpecialOneExit);
        ShadowStepExit().Forget();
    }
}