using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Hands.SinglePlayer.EnemyAI.Abilities;
using UnityEngine;
using Random = UnityEngine.Random;

// todo, the initialization logic of these makes me feel like we should name this
// BallRespawnManager
public class BallRespawnManager : MonoBehaviour
{
    [SerializeField] private bool forceMode;
    [SerializeField] private GameObject respawnPrefab;

    [SerializeField] private List<BallRespawnPoint> ballRespawnPoints;
    private int _ballLayer;

    [SerializeField] private DodgeballPlayArea dodgeballPlayArea;

    private int _currentIndex;
    private bool _teamOneSpawnSide;

    private void Start() => SetPlayBalls().Forget();

    public void SetNewNumberBalls(int dodgeballs)
    {
        dodgeballPlayArea.dodgeballCount = dodgeballs;
        SetPlayBalls();
    }
    
    private async UniTaskVoid SetPlayBalls()
    {
        await UniTask.DelayFrame(2);
        for (var i = 0; i < dodgeballPlayArea.dodgeballCount; i++)
        {
            Debug.Log("Spawning Ball");
            await UniTask.Yield();
            CreateRandomSpawnPoint();
            var ball = BallPool.SetBall(ballRespawnPoints[_currentIndex].transform.position);
            ball.SetActive(true);
            _currentIndex = (_currentIndex + 1) % ballRespawnPoints.Count;
        }
        _ballLayer = LayerMask.NameToLayer("Ball");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != _ballLayer) return;
        if (forceMode) return;
        if (dodgeballPlayArea.dodgeBalls.Count > dodgeballPlayArea.dodgeballCount)
        {
            ballRespawnPoints[_currentIndex].DespawnBall(other.gameObject);
            _currentIndex = (_currentIndex + 1) % ballRespawnPoints.Count;
            return;
        }
        CreateRandomSpawnPoint();
        ballRespawnPoints[_currentIndex].SpawnBall(other.gameObject);
        _currentIndex = (_currentIndex + 1) % ballRespawnPoints.Count;
    }

    private void CreateRandomSpawnPoint() =>
        MoveToCourtBounds(_teamOneSpawnSide ? dodgeballPlayArea.team1PlayArea : dodgeballPlayArea.team2PlayArea);

    private void MoveToCourtBounds(Transform courtBounds)
    {
        var moveObject = ballRespawnPoints[_currentIndex];

        var xDistToCenter = courtBounds.localScale.x / 2;
        var zDistToCenter = courtBounds.localScale.z / 2;

        var moveObjectPos = moveObject.transform.position;
        moveObjectPos.x = courtBounds.position.x + Random.Range(-xDistToCenter, xDistToCenter);
        moveObjectPos.y = transform.position.y + 0.33f;
        moveObjectPos.z = courtBounds.position.z + Random.Range(-zDistToCenter, zDistToCenter);
        moveObject.transform.position = moveObjectPos;

        if (ballRespawnPoints[_currentIndex].IsOccupied())
        {
            ballRespawnPoints.Add(Instantiate(respawnPrefab, moveObject.transform.position, Quaternion.identity)
                .GetComponent<BallRespawnPoint>());
            _currentIndex = ballRespawnPoints.Count - 1;
        }

        _teamOneSpawnSide = !_teamOneSpawnSide;
    }
}