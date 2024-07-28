using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BallBoundsHelper : MonoBehaviour
{
    [SerializeField] private bool forceMode;
    public float backToBoundsForce = 3f;
    [SerializeField] private BallRespawnPoint[] ballRespawnPoints;
    private int _ballLayer;
    
    public static Dictionary<GameObject, bool> BallInPlay = new();
    [SerializeField] private DodgeballPlayArea dodgeballPlayArea;

    // Start is called before the first frame update
    void Start()
    {
        foreach (var ball in dodgeballPlayArea.dodgeBalls)
            BallInPlay.Add(ball, true);
        
        _ballLayer = LayerMask.NameToLayer("Ball");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != _ballLayer) return;
        if (forceMode) return;
        if (!BallInPlay.ContainsKey(other.gameObject)) return;
        if (!BallInPlay[other.gameObject]) return;
        BallInPlay[other.gameObject] = false;
        
        var ball = other.gameObject;
        var respawnPoint = ballRespawnPoints[Random.Range(0, ballRespawnPoints.Length)];

        if (respawnPoint.IsOccupied())
        { 
            for (var i = 0; i < ballRespawnPoints.Length; i++)
            {
                if (ballRespawnPoints[i].IsOccupied()) continue;
                respawnPoint = ballRespawnPoints[i];
                break;
            }
        }

        if (respawnPoint.IsOccupied())
        {
            Debug.LogError("No respawn points available. Ball will be destroyed.");
            return;
        }
        
        respawnPoint.SpawnBall(ball);
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.layer != _ballLayer) return;

        if (!forceMode) return;

        var rb = other.gameObject.GetComponent<Rigidbody>();
        var force = (transform.position - other.transform.position).normalized * backToBoundsForce;
        rb.AddForce(force);
    }
}