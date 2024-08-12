using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

public class ShadowStep : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private float stepDistance = 5f;
    [SerializeField] private float stepDuration;
    [SerializeField] private Vector3 stepDirection = Vector3.forward;

    [SerializeField] private AnimationCurve entryCurve;
    [SerializeField] private AnimationCurve exitCurve;
    [SerializeField] private GameObject exitEffect;
    [SerializeField] private GameObject entryEffect;
    [SerializeField] private GameObject floorSmoke;
    [SerializeField] private float entrySpeed;
    [SerializeField] private float exitSpeed;
    [SerializeField] private float exitDuration;
    
    private Vector3 _originalEffectLocalPosition;
    private Animator _anim;

    private bool _isShadowStepping;
    private void Start()
    {
        _anim = player.GetComponentInChildren<Animator>();
    }

    public void ShadowStepMove()
    {
        if (_isShadowStepping) return;
        _isShadowStepping = true;
        _anim.SetTrigger(AIAnimationHelper.SSpecialOne);
        ShadowStepEnter().Forget();
    }

    private async UniTaskVoid ShadowStepExit()
    {
        var playerPosition = player.transform.position;
        var exitPoint = player.transform.TransformPoint(-stepDirection * stepDistance / 2);
        
        var exitTime = 0f;
        while (exitTime < 1)
        {
            player.transform.position = Vector3.Lerp(playerPosition, exitPoint, exitCurve.Evaluate(exitTime));
            exitTime += Time.deltaTime / exitDuration;
            await UniTask.Yield();
        }
    }
    
    private async UniTaskVoid ShadowStepEnter()
    {
        floorSmoke.transform.position = player.transform.position + stepDirection * (stepDistance / 8);
        floorSmoke.SetActive(true);
        // entryEffect.SetActive(true);
        var entryPoint = player.transform.TransformPoint(stepDirection * (stepDistance / 4));
        var start = player.transform.position;
        var entryTime = 0f;
        while (_isShadowStepping)
        {
            player.transform.position = Vector3.Lerp(start, entryPoint, entryCurve.Evaluate(entryTime));
            entryTime += Time.deltaTime / entrySpeed;
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

    private async UniTaskVoid Reappear()
    {
        await UniTask.Yield();
        player.SetActive(false);
        var targetPosition = player.transform.TransformPoint(stepDirection * stepDistance);
        targetPosition = ClampPositionWithinBounds(targetPosition);
        player.transform.position = targetPosition;
        await UniTask.Delay(TimeSpan.FromSeconds(stepDuration));
        entryEffect.SetActive(false);
        exitEffect.SetActive(true);
        player.SetActive(true);
        floorSmoke.SetActive(false);
        _anim.Play(AIAnimationHelper.SSpecialOneExit);
        ShadowStepExit().Forget();
    }

    private Vector3 ClampPositionWithinBounds(Vector3 targetPosition)
    {
        Vector3 minBounds = _originalEffectLocalPosition - transform.localScale / 2;
        Vector3 maxBounds = _originalEffectLocalPosition + transform.localScale / 2;

        targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
        targetPosition.y = Mathf.Clamp(targetPosition.y, minBounds.y, maxBounds.y);
        targetPosition.z = Mathf.Clamp(targetPosition.z, minBounds.z, maxBounds.z);

        return targetPosition;
    }
}
