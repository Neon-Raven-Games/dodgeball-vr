using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class LerpColorOnEnable : MonoBehaviour
{
    private ColorLerp colorLerp;
    private bool _initialized;
    [SerializeField] private float lerpDuration = 1f;

    private void Awake()
    {
        colorLerp = GetComponent<ColorLerp>();
        colorLerp.onMaterialsLoaded += () => _initialized = true;
    }

    private void OnEnable()
    {
        LerpColor();
    }

    public void LerpColor()
    {
        if (!_initialized) return;
        colorLerp.lerpValue = 1;
        StartColorLerp().Forget();
    }
    
    
    private async UniTaskVoid StartColorLerp()
    {
        var curTime = 0f;
        while (curTime < 1)
        {
            curTime += Time.deltaTime / lerpDuration;
            colorLerp.lerpValue = 1 - curTime;
            await UniTask.Yield();
        }
    }
}