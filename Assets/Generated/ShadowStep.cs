using System;
using System.Collections.Concurrent;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class ShadowStep : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject shadowStepEffect;
    [SerializeField] private float stepDistance = 5f;
    [SerializeField] private float disappearDuration = 0.5f;
    [SerializeField] private Vector3 stepDirection = Vector3.forward;
    [SerializeField] private float fxOffset;
    [SerializeField] private ParticleSystem spriteSystem;

    private Vector3 _originalPosition;
    private ConcurrentDictionary<Material, Color> playerMaterials = new();
    private Animator _anim;

    private void Start()
    {
        _anim = player.GetComponentInChildren<Animator>();
        CacheAllMaterials();
    }

    private void CacheAllMaterials()
    {
        playerMaterials.Clear();
        foreach (var rend in player.GetComponentsInChildren<Renderer>())
        {
            foreach (var material in rend.materials)
            {
                if (!playerMaterials.ContainsKey(material))
                {
                    playerMaterials.TryAdd(material, material.color);
                }
            }
        }
    }

    public void ShadowStepMove()
    {
        _originalPosition = player.transform.position;

        var targetPosition = player.transform.TransformPoint(stepDirection * stepDistance);
        targetPosition = ClampPositionWithinBounds(targetPosition);

        PerformShadowStep(targetPosition).Forget();
    }

    private async UniTask LerpAllColorsToBlack()
    {
        var time = disappearDuration / 4;
        for (float t = 0; t < time; t += Time.deltaTime)
        {
            foreach (var material in playerMaterials.Keys)
            {
                material.color = Color.Lerp(material.color, Color.black, t / time);
            }
            await UniTask.Yield(); 
        }
    }

    private async UniTask LerpAllColorsToOriginal()
    {
        for (float t = 0; t < disappearDuration; t += Time.deltaTime)
        {
            foreach (var material in playerMaterials.Keys)
            {
                material.color = Color.Lerp(material.color, playerMaterials[material], t / disappearDuration);
            }
            await UniTask.Yield(); // Ensure smooth transition over time
        }
    }

    private async UniTask WaitForParticleSystemTime()
    {
        
        while (spriteSystem.time < 0.2f)
        {
            await UniTask.Yield();
        }
    }

    private async UniTaskVoid PerformShadowStep(Vector3 targetPosition)
    {
        _anim.Play("ShadowStep");
        
        PlayEffect(player.transform.position);
        await WaitForParticleSystemTime();
        await LerpAllColorsToBlack();
        
        var animStateInfo = _anim.GetCurrentAnimatorStateInfo(0);
        float normalizedTime = animStateInfo.normalizedTime % 1;

        player.SetActive(false);

        await UniTask.Delay(TimeSpan.FromSeconds(disappearDuration * 0.25f));
        player.transform.position = targetPosition;
        
        PlayEffect(player.transform.position);
        player.SetActive(true);
        _anim.Play("ShadowStep", 0, normalizedTime);

        await LerpAllColorsToOriginal();
    }

    private void PlayEffect(Vector3 position)
    {
        shadowStepEffect.SetActive(false);
        shadowStepEffect.transform.position = position;
        shadowStepEffect.SetActive(true);
    }

    private Vector3 ClampPositionWithinBounds(Vector3 targetPosition)
    {
        Vector3 minBounds = _originalPosition - transform.localScale / 2;
        Vector3 maxBounds = _originalPosition + transform.localScale / 2;

        targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
        targetPosition.y = Mathf.Clamp(targetPosition.y, minBounds.y, maxBounds.y);
        targetPosition.z = Mathf.Clamp(targetPosition.z, minBounds.z, maxBounds.z);

        return targetPosition;
    }
}
