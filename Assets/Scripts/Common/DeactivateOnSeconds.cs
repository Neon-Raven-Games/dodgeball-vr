using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class DeactivateOnSeconds : MonoBehaviour
{
    [SerializeField] private float seconds;
    
    private void OnEnable()
    {
        Deactivate().Forget();
    }
    
    private async UniTaskVoid Deactivate()
    {
        await UniTask.Delay(TimeSpan.FromSeconds(seconds));
        gameObject.SetActive(false);
    }
}
