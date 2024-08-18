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
    private float lastShadowStepTime = -Mathf.Infinity;
    private TeleportationPathHandler _teleportationPathHandler;

    private readonly float[] _preferredAngles = {-45f, 45f, -30f, 30f, -85f, 85f};

    public ShadowStepUtility(ShadowStepUtilityArgs args, DodgeballAI.AIState state, DodgeballAI ai) : base(args, state)
    {
        _ai = ai;
        _teleportationPathHandler = ai.GetComponent<TeleportationPathHandler>();
        _animator = ai.animator;
    }

    public override float Execute(DodgeballAI ai)
    {
        if (_shadowSteppingSequencePlaying || _ai.transform.position.y < 0.1f) return 0;
        ShadowStepMove();
        return 1f;
    }


    public override float Roll(DodgeballAI ai)
    {
        if (_shadowSteppingSequencePlaying || _ai.currentState != DodgeballAI.AIState.Throw) return 0;
        if (!_ai.hasBall) return 0;

        var timeSinceLastStep = Time.time - lastShadowStepTime;
        if (timeSinceLastStep < args.shadowStepCooldown) return 0;
        lastShadowStepTime = Time.time;

        var roll = UnityEngine.Random.Range(1, 100);
        var canShadowStep = roll > args.rollChance;

        if (!_shadowSteppingSequencePlaying && _ai.hasBall && canShadowStep) return float.MaxValue;
        return 0;
    }

    #region refactor methods

    private void OnIntroPointReached()
    {
        if (!_isShadowStepping) return;
        _isShadowStepping = false;

        args.ik.solvers.leftHand.SetIKPositionWeight(0);
        args.ik.solvers.leftHand.SetIKRotationWeight(0);
        args.ik.solvers.rightHand.SetIKPositionWeight(0);
        args.ik.solvers.rightHand.SetIKRotationWeight(0);
        args.ik.solvers.spine.SetIKPositionWeight(0);
        args.ik.solvers.lookAt.SetIKPositionWeight(0);

        args.aiAvatar.SetActive(false);
        _ai.SwitchBallSideToLeft();

        _ai.SetOutOfPlay(false);
    }

    private void OnFinishTeleport()
    {
        ExitTeleport().Forget();
    }

    private async UniTask ExitTeleport()
    {
        await UniTask.Yield();

        if (switchBackToSigning)
        {
            var t = 0f;
            while (t < 1)
            {
                t += Time.deltaTime / 0.2f;
                args.ik.solvers.leftHand.SetIKPositionWeight(t);
                args.ik.solvers.leftHand.SetIKRotationWeight(t);
                await UniTask.Yield();
            }

            switchBackToSigning = false;
        }
        else
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.2f));
        }

        _ai.currentState = DodgeballAI.AIState.Move;
    }

    private async UniTask EnterOutro()
    {
        args.entryEffect.SetActive(false);
        args.exitEffect.SetActive(true);
        args.aiAvatar.SetActive(true);
        args.floorSmoke.SetActive(false);
        _animator.Play(AIAnimationHelper.SSpecialOneExit);
        LerpColors(0, FrameToSeconds(args.outroColorFrame, args.outroAnimationClip),
            args.outroAnimationClip, args.outroColorLerpValue,
            0).Forget();
        InvokeAnimationEvent(args.outroAnimationClip, args.outroThrowFrame, _ai.ThrowBall).Forget();
    }

    private bool switchBackToSigning;

    private void OnMovedToOutroPoint()
    {
        switchBackToSigning = isSigning;
        isSigning = false;

        args.ik.solvers.leftHand.SetIKPositionWeight(0);
        args.ik.solvers.leftHand.SetIKRotationWeight(0);

        EnterOutro().Forget();
        _shadowSteppingSequencePlaying = false;
    }

    public void ShadowStepMove()
    {
        if (_shadowSteppingSequencePlaying) return;
        _shadowSteppingSequencePlaying = true;
        lastShadowStepTime = Time.time;
        _isShadowStepping = true;
        InitializeTeleport();
        _teleportationPathHandler.Teleport(TeleportationType.ShadowStep, args.stepDirection, OnIntroPointReached,
            OnMovedToOutroPoint,
            OnFinishTeleport).Forget();

        // ShadowStepEnter().Forget();
    }

    private void InitializeTeleport()
    {
        args.stepDirection = CalculateValidShadowStep();
        isSigning = args.ik.solvers.leftHand.IKPositionWeight > 0;

        _animator.SetTrigger(AIAnimationHelper.SSpecialOne);

        LerpColors(0, args.introAnimationClip.length,
            args.introAnimationClip, 0, args.introColorLerpValue).Forget();

        args.floorSmoke.transform.position = _ai.transform.position + args.stepDirection * (args.stepDistance / 8);
        args.floorSmoke.SetActive(true);
    }

    #endregion

    private void RandomizeAngles()
    {
        for (var i = _preferredAngles.Length - 1; i > 0; i--)
        {
            var j = UnityEngine.Random.Range(0, i + 1);
            (_preferredAngles[i], _preferredAngles[j]) = (_preferredAngles[j], _preferredAngles[i]);
        }
    }

    private Vector3 CalculateValidShadowStep()
    {
        Vector3 bestDirection = Vector3.zero;
        float maxDistance = 0f;
        RandomizeAngles();

        for (var i = 0; i < _preferredAngles.Length; i++)
        {
            Vector3 direction = Quaternion.Euler(0, _preferredAngles[i], 0) * _ai.transform.forward;
            Debug.DrawRay(_ai.transform.position, direction * args.stepDistance, Color.red, 2.0f);

            Vector3 targetPosition = _ai.transform.position + direction * args.stepDistance;

            if (!IsWithinPlayArea(targetPosition, _ai.friendlyTeam.playArea)) continue;

            var distance = Vector3.Distance(_ai.transform.position, targetPosition);
            if (distance < maxDistance) continue;
            
            maxDistance = distance;
            bestDirection = direction;
        }

        if (bestDirection == Vector3.zero)
        {
            Debug.Log("no best dir");
            bestDirection = _ai.transform.TransformDirection(Vector3.left);
        }
        
        return bestDirection;
    }

    private readonly Vector3[] _bestPoints = new Vector3[4];

    private bool IsWithinPlayArea(Vector3 position, Transform playArea)
    {
        var size = playArea.localScale;
        size.y = 5;
        Bounds bounds = new Bounds(playArea.position, size);
        Debug.DrawRay(position, Vector3.up, Color.cyan, 2.0f);

        return bounds.Contains(position);
    }


    #region custom animation events to refactor

    // Invokes throwing animation event
    private async UniTaskVoid InvokeAnimationEvent(AnimationClip clip, int frame, Action action)
    {
        await UniTask.Yield();
        const float tolerance = 0.01f;
        var executeDelay = FrameToSeconds(frame, clip);
        var curTime = 0f;

        while (curTime < executeDelay - tolerance)
        {
            _ai.transform.LookAt(_ai.targetUtility.CurrentTarget.transform);
            var animState = _ai.animator.GetCurrentAnimatorStateInfo(0);
            var normalizedTime = animState.normalizedTime % 1;
            curTime = normalizedTime * clip.length;
            await UniTask.Yield();
        }

        action.Invoke();
    }

    private bool isSigning;

    // lerps our character's color
    private async UniTaskVoid LerpColors(float fromSeconds, float toSeconds, AnimationClip clip, float fromValue,
        float toValue)
    {
        const float tolerance = 0.01f;
        args.colorLerp.lerpValue = fromValue;
        await UniTask.Yield();
        var curTime = 0f;
        while (curTime < fromSeconds && fromSeconds > 0 && args.aiAvatar.activeInHierarchy)
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
        while (Mathf.Abs(currentTime - toSeconds) > tolerance && args.aiAvatar.activeInHierarchy)
        {
            var animState = _ai.animator.GetCurrentAnimatorStateInfo(0);
            var normalizedTime = animState.normalizedTime % 1;
            currentTime = normalizedTime * clip.length;

            var t = Mathf.InverseLerp(fromSeconds, toSeconds, currentTime);
            args.colorLerp.lerpValue = Mathf.Lerp(fromValue, toValue, t);

            if (isSigning)
            {
                // todo, this needs to be decoupled, but setting this for the sake of keeping less routines
                args.ik.solvers.leftHand.SetIKPositionWeight(1 - args.colorLerp.lerpValue);
                args.ik.solvers.leftHand.SetIKRotationWeight(1 - args.colorLerp.lerpValue);
            }

            await UniTask.Yield();
        }

        args.colorLerp.lerpValue = 0;
    }

    #endregion

    private async UniTaskVoid ShadowStepExit()
    {
        var exitPoint = _bestPoints[^1];
        var playerPosition = _bestPoints[^2];
        _ai.transform.position = playerPosition;
        Debug.DrawRay(exitPoint, Vector3.up, Color.magenta, 2.0f);
        var targetTransform = _ai.targetUtility.CurrentTarget;
        var exitTime = 0f;
        while (exitTime < 1)
        {
            _ai.transform.LookAt(targetTransform.transform);
            _ai.transform.position = Vector3.Lerp(playerPosition, exitPoint, args.exitCurve.Evaluate(exitTime));
            exitTime += Time.deltaTime / args.exitDuration;
            await UniTask.Yield();
        }

        _ai.transform.position = exitPoint;

        await UniTask.Yield();
        _shadowSteppingSequencePlaying = false;
        await UniTask.Delay(TimeSpan.FromSeconds(0.2f));
    }

    private async UniTaskVoid ShadowStepEnter()
    {
        if (!_ai) return;

        await UniTask.Yield();

        var start = _bestPoints[0];
        var entryPoint = _bestPoints[1];
        Debug.DrawRay(entryPoint, Vector3.up, Color.yellow, 2.0f);
        var introDuration = args.introAnimationClip.length; // Total duration of the intro animation in seconds
        var entryTime = 0f;

        while (entryTime < 1)
        {
            if (!_ai) break;

            var t = args.entryCurve.Evaluate(entryTime);
            _ai.transform.position = Vector3.Lerp(start, entryPoint, t);

            entryTime += Time.deltaTime / introDuration;

            await UniTask.Yield();
        }

        // InitialShadowStepFinished();
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

        args.ik.solvers.leftHand.SetIKPositionWeight(0);
        args.ik.solvers.leftHand.SetIKRotationWeight(0);
        args.ik.solvers.rightHand.SetIKPositionWeight(0);
        args.ik.solvers.rightHand.SetIKRotationWeight(0);
        args.ik.solvers.spine.SetIKPositionWeight(0);
        args.ik.solvers.lookAt.SetIKPositionWeight(0);

        args.aiAvatar.SetActive(false);
        _ai.SwitchBallSideToLeft();

        // Reappear().Forget();
        _ai.SetOutOfPlay(false);
    }

    private async UniTaskVoid Reappear()
    {
        await UniTask.Delay(TimeSpan.FromSeconds(args.stepDuration));
        args.entryEffect.SetActive(false);
        args.exitEffect.SetActive(true);
        args.aiAvatar.SetActive(true);
        args.floorSmoke.SetActive(false);
        _animator.Play(AIAnimationHelper.SSpecialOneExit);
        LerpColors(0, FrameToSeconds(args.outroColorFrame, args.outroAnimationClip),
            args.outroAnimationClip, args.outroColorLerpValue,
            0).Forget();
        InvokeAnimationEvent(args.outroAnimationClip, args.outroThrowFrame, _ai.ThrowBall).Forget();
        // ShadowStepExit().Forget();
        await UniTask.Yield();
    }
}