using System;
using System.Collections;
using CloudFine.ThrowLab;
using Unity.Template.VR.Multiplayer;
using UnityEngine;

public class Pedastal : MonoBehaviour
{
    [SerializeField] private LabManager labManager;
    [SerializeField] private Transform spawnTransform;
    private int _ballCount;

    private void OnEnable()
    {
        StartCoroutine(SpawnNewBallRoutine());
    }

    private void OnDisable()
    {
        _ballCount = 0;
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ball"))
        {
            _ballCount++;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ball"))
        {
            _ballCount--;
            if (_ballCount <= 0)
            {
                _ballCount = 0; 
                StartCoroutine(SpawnNewBallRoutine());
            }
        }
    }

    private IEnumerator SpawnNewBallRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        if (_ballCount == 0)
        {
            var ball = BallPool.SetBall(spawnTransform.position);
            labManager.ResetBall(ball.GetComponent<ThrowHandle>());
            ball.SetActive(true);
        }
    }
}