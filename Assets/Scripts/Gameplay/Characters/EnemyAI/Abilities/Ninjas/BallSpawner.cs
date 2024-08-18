using Hands.SinglePlayer.EnemyAI.Abilities;
using UnityEngine;
using Random = UnityEngine.Random;

public class BallSpawner : MonoBehaviour
{
    [SerializeField] private int numberOfBalls = 10;
    [SerializeField] private float spawnRadius = 5f;
    [SerializeField] private float travelTime = 2f;
    [SerializeField] private Transform planeTransform;
    [SerializeField] private GameObject ballPrefab;

    private void Start()
    {
        SpawnBalls();
    }

    private void SpawnBalls()
    {
        for (int i = 0; i < numberOfBalls; i++)
        {
            Vector3 randomDirection = Random.insideUnitSphere;
            
            randomDirection.Normalize();

            Vector3 spawnPosition = planeTransform.position + randomDirection * Random.Range(0, spawnRadius);
            var ball = Instantiate(ballPrefab, spawnPosition, Quaternion.identity);
            ball.AddComponent<BallMovement>().Initialize(planeTransform, travelTime);
        }
    }

    private void OnDrawGizmos()
    {
        if (!planeTransform) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(planeTransform.position, spawnRadius);
    }
}