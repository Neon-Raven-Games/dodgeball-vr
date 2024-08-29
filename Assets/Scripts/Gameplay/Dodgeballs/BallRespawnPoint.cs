using System;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

public class BallRespawnPoint : MonoBehaviour
{
    [SerializeField] private GameObject particleEffect;
    [SerializeField] private float spawnInForce = 5f;
    [SerializeField] private float effectDelay = 1f;
    [SerializeField] float effectDespawnDelay = 0.3f;
    [SerializeField] private float delayTime = 2f;
    private bool _occupuied;

    public bool IsOccupied() => _occupuied;

    public void SpawnBall(GameObject ball)
    {
        _occupuied = true;
        DelayInThreadPool(ball).Forget();
    }

    public void DespawnBall(GameObject ball)
    {
        WaitToDespawnBall(ball).Forget();
    }

    private async UniTaskVoid WaitToDespawnBall(GameObject ball)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(delayTime));
        ball.SetActive(false);
        _occupuied = false;
    }

    private async UniTaskVoid DelayInThreadPool(GameObject oldBall)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(delayTime));
        oldBall.SetActive(false);

        particleEffect.gameObject.SetActive(true);
        await UniTask.Delay(TimeSpan.FromSeconds(effectDelay * 2));

        var ball = BallPool.SetBall(transform.position);

        ball.transform.rotation = quaternion.identity;
        ball.transform.position = transform.position;
        var rb = ball.GetComponent<Rigidbody>();
        if (!rb.isKinematic) rb.isKinematic = false;
        rb.angularVelocity = Vector3.zero;
        rb.velocity = Vector3.up * spawnInForce;
        ball.SetActive(true);

        await UniTask.Delay(TimeSpan.FromSeconds(effectDespawnDelay));

        particleEffect.gameObject.SetActive(false);
        _occupuied = false;
    }
}