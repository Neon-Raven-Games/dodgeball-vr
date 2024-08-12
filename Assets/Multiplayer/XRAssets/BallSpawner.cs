using System.Collections;
using Unity.Template.VR.Multiplayer;
using UnityEngine;

namespace _dev.Dodgeballs
{
    public class BallSpawner : MonoBehaviour
    {
        [SerializeField] private bool despawnAfterSeconds;
        [SerializeField] protected float particleDelay = 1f;
        [SerializeField] protected float ballDelay = 1f;
        [SerializeField] private DodgeballLab lab;
        [SerializeField] private GameObject ballPrefab;
        private ThrowHandle _ballHandle;
        private ParticleSystem _particleSystem;

        private void Start()
        {
            _particleSystem = GetComponentInChildren<ParticleSystem>();
            lab.ballSpawners.Add(this);
            StartCoroutine(SpawnBall());
        }

        protected IEnumerator SpawnBall()
        {
            var go = Instantiate(ballPrefab, transform.position, transform.rotation);
            _ballHandle = go.GetComponent<ThrowHandle>();

            if (despawnAfterSeconds) _ballHandle.onDetachFromHand += () => AttachDeactivateScript(go);

            lab.SetThrowableConfig(_ballHandle);
            _ballHandle.gameObject.SetActive(false);
            _ballHandle.transform.position = transform.position;
            _ballHandle.transform.rotation = transform.rotation;

            yield return new WaitForSeconds(ballDelay);
            _particleSystem.Play();
            yield return new WaitForSeconds(particleDelay);
            _ballHandle.gameObject.SetActive(true);
        }

        private void AttachDeactivateScript(GameObject go)
        {
            var delay = go.AddComponent<SetDeactiveAfterSeconds>();
            delay.delaySeconds = 8f;
            delay.StartCoroutine(delay.DeactivateAfterSeconds());
        }
    }
}