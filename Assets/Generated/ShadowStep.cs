using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class ShadowStep : MonoBehaviour
{
    [SerializeField] [Tooltip("The player object to shadow step towards.")]
    private GameObject player;
    [SerializeField] [Tooltip("The particle system to play during the shadow step.")]
    private GameObject shadowStepEffect;

    [SerializeField] [Tooltip("The distance to move forward during the shadow step.")]
    private float stepDistance = 5f;

    [SerializeField] [Tooltip("The duration for which the character will disappear.")]
    private float disappearDuration = 0.5f;

    [SerializeField] [Tooltip("The direction for the player to shadow step towards.")]
    private Vector3 stepDirection = Vector3.forward;

    [SerializeField][Tooltip("The offset to apply to the shadow step effect. Inward facing offsets, will appear first time slightly leading the player, and slightly trailing the player on the second appearance. Outward facing offsets will appear slightly trailing the player on the first appearance, and slightly leading the player on the second appearance.")]
    private float fxOffset;
    
    [SerializeField]
    private ParticleSystem spriteSystem;
    
    private Vector3 _originalPosition;
    private Vector3 _fxOriginalLocalPosition;

    private Dictionary<Material, Color> playerMaterials = new ();
    private void Start()
    {
        GetAllMaterials().Forget();
    }
    
    private async UniTaskVoid GetAllMaterials()
    {
        playerMaterials.Clear();
        foreach (var rend in player.GetComponentsInChildren<Renderer>())
        {
            if (rend.gameObject.activeInHierarchy)
            {
                Debug.Log($"getting color for: {rend.gameObject.name}");
                foreach (var material in rend.materials)
                {
                    playerMaterials.Add(material, material.color);
                }
            }
            await UniTask.Yield();
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
        for (float t = 0; t < disappearDuration; t += Time.deltaTime)
        {
            foreach (var material in playerMaterials.Keys)
            {
                material.color = Color.Lerp(material.color, Color.black, t / 0.15f);
            }
        }
        
        await UniTask.Yield();
    }

    
    private async UniTask LerpAllColorsToOriginal()
    {
        for (float t = 0; t < disappearDuration; t += Time.deltaTime)
        {
            foreach (var material in playerMaterials.Keys)
            {
                material.color = Color.Lerp(material.color, playerMaterials[material], t / 0.15f);
            }
        }
        
        await UniTask.Yield();
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
        Quaternion originalRotation = player.transform.rotation;
        RotateOverTime(player.transform.rotation, Quaternion.LookRotation(targetPosition - player.transform.position));
        await LerpAllColorsToBlack();

        PlayEffect(player.transform.position); // + stepDirection * fxOffset);
        await WaitForParticleSystemTime();
        
        player.SetActive(false);

        await UniTask.Delay(TimeSpan.FromSeconds(disappearDuration * 0.25f));
        player.transform.position = targetPosition;
        PlayEffect(player.transform.position);// - stepDirection * fxOffset);

        await UniTask.Delay(TimeSpan.FromSeconds(disappearDuration * 0.5f));
        player.SetActive(true);
        shadowStepEffect.SetActive(false);

        RotateOverTime(player.transform.rotation, originalRotation);
        LerpAllColorsToOriginal();
    }

    private void PlayEffect(Vector3 position)
    {
        shadowStepEffect.transform.position = position;
        shadowStepEffect.SetActive(true);
    }

    private async UniTask RotateOverTime(Quaternion fromRotation, Quaternion toRotation)
    {
        for (float t = 0; t < disappearDuration; t += Time.deltaTime)
        {
            player.transform.rotation = Quaternion.Slerp(fromRotation, toRotation, t / 0.15f);
            await UniTask.Yield();
        }
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