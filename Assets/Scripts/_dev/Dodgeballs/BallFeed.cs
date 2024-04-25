using System;
using System.Collections;
using UnityEngine;

namespace _dev.Dodgeballs
{
    public class BallFeed : BallSpawner
    {
        private bool _spawning;

        private void OnTriggerExit(Collider other)
        {
            if (_spawning) return;
            _spawning = true;
            StartCoroutine(CollisionBlockedSpawnBall());
        }

        private IEnumerator CollisionBlockedSpawnBall()
        {
            StartCoroutine(SpawnBall());
            
            yield return new WaitForSeconds(ballDelay + particleDelay);
            _spawning = false;
        }
    }
}