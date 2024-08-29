using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Hands.SinglePlayer.EnemyAI.Abilities;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class BallSpawner : MonoBehaviour
{
    [SerializeField] private int numberOfBalls = 10;
    [SerializeField] private float spawnRadius = 5f;
    [SerializeField] private float travelTime = 2f;
    [SerializeField] private float centerInfluence = 5f;
    [SerializeField] private float distance = 15f;
    [SerializeField] internal Transform planeTransform;
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private Vector2 ballLaunchStep;
    private List<BallMovement> balls = new();

    public void SpawnBalls()
    {
        if (balls.Count > 0)
        {
            Debug.LogError("Balls already spawned");
            return;
        }
        Debug.Log("Spawning balls...");
        for (var i = 0; i < numberOfBalls; i++)
        {
            var ball = Instantiate(ballPrefab, Vector3.down, Quaternion.identity);
            ball.gameObject.SetActive(false);
            var movement = ball.AddComponent<BallMovement>();
            movement.Initialize(planeTransform, travelTime, centerInfluence, distance);
            ball.GetComponent<Rigidbody>().isKinematic = true;

            balls.Add(movement);
        }
        LaunchBalls().Forget();
    }

    internal async UniTaskVoid LaunchBalls()
    {
        for (int i = 0; i < balls.Count; i++)
        {
            balls[i].gameObject.SetActive(false);
            Vector3 randomDirection = Random.insideUnitSphere;

            randomDirection.Normalize();

            Vector3 spawnPosition = planeTransform.position + randomDirection * Random.Range(0, spawnRadius);
            balls[i].transform.position = spawnPosition;
            balls[i].gameObject.SetActive(true);
            balls[i].StartBallRoutine();
            await UniTask.WaitForSeconds(Random.Range( ballLaunchStep.x, ballLaunchStep.y));
        }
        balls.Clear();
    }

    private void OnDrawGizmos()
    {
        if (!planeTransform) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(planeTransform.position, spawnRadius);
    }
}