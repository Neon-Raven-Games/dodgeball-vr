using System;
using Cysharp.Threading.Tasks;
using Hands.SinglePlayer.EnemyAI;
using UnityEngine;

public class ShadowStepUtility : Utility<ShadowStepUtilityArgs>, IUtility
{
    private readonly Animator _animator;
    private static DodgeballAI _ai;
    internal bool _shadowSteppingSequencePlaying;
    private bool _isShadowStepping;
    private bool _canShadowStep;
    private float lastShadowStepTime = -Mathf.Infinity;


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


    public override float Roll(DodgeballAI ai)
    {
        if (_shadowSteppingSequencePlaying || _ai.currentState != DodgeballAI.AIState.Throw) return 0;
        if (!_ai.hasBall) return 0;
        
        var timeSinceLastStep = Time.time - lastShadowStepTime;
        if (timeSinceLastStep < args.shadowStepCooldown) return 0;

        var roll = UnityEngine.Random.Range(1, 100);
        _canShadowStep = roll > args.rollChance;

        if (!_shadowSteppingSequencePlaying && _ai.hasBall && _canShadowStep) ShadowStepMove();
        _canShadowStep = false;
        return 0;
    }

    public void ShadowStepMove()
    {
        if (_shadowSteppingSequencePlaying) return;
        Debug.Log("executing shadow step");
        _shadowSteppingSequencePlaying = true;
        lastShadowStepTime = Time.time;
        _isShadowStepping = true;
        args.stepDirection = CalculateValidShadowStep();
        args.stepDirection.y = _ai.transform.position.y;

        ShadowStepEnter().Forget();
    }


    private Vector3 CalculateValidShadowStep()
    {
        // Define the preferred angles in degrees for left and right movements
        float[] preferredAngles = {-45f, 45f, -30f, 30f, -15f, 15f};

        Vector3 bestDirection = Vector3.zero;
        float maxDistance = 0f;

        foreach (float angle in preferredAngles)
        {
            // Calculate the direction based on the preferred angle
            Vector3 direction = Quaternion.Euler(0, angle, 0) * _ai.transform.forward;

            // Determine the target position in world space
            Vector3 targetPosition = _ai.transform.TransformPoint(direction * args.stepDistance);

            // Check if the target position is within the bounds of the play area
            if (IsWithinPlayArea(targetPosition, _ai.playArea.team2PlayArea))
            {
                // Calculate the distance to the target position from the AI's current position
                float distance = Vector3.Distance(_ai.transform.position, targetPosition);

                // Choose the direction that gives the AI the farthest valid movement
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    bestDirection = direction;
                }
            }
        }

        // If no valid direction is found (unlikely, but a safety check)
        if (bestDirection == Vector3.zero)
        {
            // Default to moving left if nothing else is valid
            bestDirection = _ai.transform.TransformDirection(Vector3.left);
        }

        return bestDirection;
    }

    private bool IsWithinPlayArea(Vector3 position, Transform playArea)
    {
        // Assume playArea has a method or property that gives us the bounds
        Bounds bounds = new Bounds(playArea.transform.position, playArea.localScale);

        return bounds.Contains(position);
    }


    private async UniTaskVoid ShadowStepExit()
    {
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
    }

    private async UniTaskVoid LerpColors(float fromSeconds, float toSeconds, AnimationClip clip, float fromValue,
        float toValue)
    {
        args.colorLerp.lerpValue = fromValue;
        await UniTask.Yield();
        var curTime = 0f;
        while (curTime < fromSeconds && fromSeconds > 0)
        {
            var animState = _ai.animator.GetCurrentAnimatorStateInfo(0);
            var normalizedTime = animState.normalizedTime % 1;
            curTime = normalizedTime * clip.length;

            fromSeconds /= _ai.animator.speed;
            toSeconds /= _ai.animator.speed;

            if (curTime >= fromSeconds) break;

            await UniTask.Yield();
        }

        var currentTime = 0f;
        const float tolerance = 0.01f;
        while (Mathf.Abs(currentTime - toSeconds) > tolerance)
        {
            var animState = _ai.animator.GetCurrentAnimatorStateInfo(0);
            var normalizedTime = animState.normalizedTime % 1;
            currentTime = normalizedTime * clip.length;

            var t = Mathf.InverseLerp(fromSeconds, toSeconds, currentTime);
            args.colorLerp.lerpValue = Mathf.Lerp(fromValue, toValue, t);

            await UniTask.Yield();
        }

        args.colorLerp.lerpValue = 0;
    }


    private async UniTaskVoid ShadowStepEnter()
    {
        if (!_ai) return;
        args.ik.solvers.leftHand.SetIKPositionWeight(0);
        args.ik.solvers.leftHand.SetIKRotationWeight(0);
        _animator.SetTrigger(AIAnimationHelper.SSpecialOne);
        
        LerpColors(0, args.introAnimationClip.length,
            args.introAnimationClip, 0, args.introColorLerpValue).Forget();

        args.floorSmoke.transform.position = _ai.transform.position + args.stepDirection * (args.stepDistance / 8);
        args.floorSmoke.SetActive(true);
        var entryPoint = _ai.transform.TransformPoint(args.stepDirection * (args.stepDistance / 4));
        entryPoint.y = _ai.transform.position.y;
        var start = _ai.transform.position;
        var entryTime = 0f;
        while (_isShadowStepping && entryTime < 1)
        {
            if (!_ai) break;
            _ai.transform.position = Vector3.Lerp(start, entryPoint, args.entryCurve.Evaluate(entryTime));
            entryTime += Time.deltaTime / args.entrySpeed;
            entryTime = Mathf.Clamp01(entryTime);
            await UniTask.Yield();
        }
    }

    public float FrameToSeconds(int frameNumber, AnimationClip clip)
    {
        return frameNumber / clip.frameRate;
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
        LerpColors(0, FrameToSeconds(args.outroColorFrame, args.outroAnimationClip),
            args.outroAnimationClip, args.outroColorLerpValue,
            0).Forget();

        ShadowStepExit().Forget();
        await UniTask.Yield();
        _shadowSteppingSequencePlaying = false;
    }
}