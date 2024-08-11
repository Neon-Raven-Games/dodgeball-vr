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
    [SerializeField] private Transform shadowStepToUnparent;
    [SerializeField] private Transform shadowStepParent;

    private Vector3 _originalEffectLocalPosition;
    private Animator _anim;

    private void Start()
    {
        _anim = player.GetComponentInChildren<Animator>();
    }

    public void ShadowStepMove()
    {
        _originalEffectLocalPosition = shadowStepToUnparent.transform.localPosition;
        shadowStepToUnparent.transform.parent = null;
        _anim.SetTrigger(AIAnimationHelper.SSpecialOne);
    }

    /// <summary>
    /// Animation event called from the the last frame of the shadow step exit animation
    /// </summary>
    public void InitialShadowStepFinished()
    {
        player.SetActive(false);
        var targetPosition = player.transform.TransformPoint(stepDirection * stepDistance);
        targetPosition = ClampPositionWithinBounds(targetPosition);
        ShadowStepEntryFinished(targetPosition);
    }

    /// <summary>
    /// Resumes shadowstep animation in the entry position
    /// </summary>
    /// <param name="targetPosition">Place where the character landed in their shadow step.</param>
    private void ShadowStepEntryFinished(Vector3 targetPosition)
    {
        player.transform.position = targetPosition;
        shadowStepToUnparent.transform.parent = shadowStepParent;
        shadowStepToUnparent.transform.localPosition = _originalEffectLocalPosition;
        Reappear().Forget();
    }
    
    private async UniTaskVoid Reappear()
    {
        await UniTask.Delay(TimeSpan.FromSeconds(stepDuration));
        player.SetActive(true);
        _anim.Play(AIAnimationHelper.SSpecialOneExit);
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
