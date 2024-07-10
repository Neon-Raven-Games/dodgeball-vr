using System;
using System.Collections;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;


namespace Unity.Template.VR.Multiplayer
{
    [Serializable]
    public class DodgeballIndex
    {
        public BallType type;
        public GameObject dodgeball;
    }

    public class NetDodgeball : NetworkBehaviour
    {
        [SerializeField] private GameObject visualBall;
        
        #region syncvar

        private readonly SyncVar<Vector3> _syncPosition = new();
        private readonly SyncVar<Vector3> _syncVelocity = new();

        public readonly SyncVar<BallState> state = new();

        #endregion

        #region unsynced vars, need to sync

        public Team team { get; set; }
        private BallType type { get; set; }

        #endregion

        private Rigidbody rb;
        private int _layerMask;
        private int _deadBallLayer;
        private float _radius;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();

            _deadBallLayer = 1 << LayerMask.NameToLayer("Ground");

            var teamOneLayer = LayerMask.NameToLayer("TeamOne");
            var teamTwoLayer = LayerMask.NameToLayer("TeamTwo");
            _layerMask = (1 << teamOneLayer) | (1 << teamTwoLayer);
            _radius = GetComponent<SphereCollider>().radius;
        }

        internal void SetBallType(BallType ballType)
        {
            if (ballType == BallType.None)
            {
                visualBall.SetActive(false);
                return;
            }

            type = ballType;
            visualBall.SetActive(true);
        }

        private void FixedUpdate()
        {
            if (HasAuthority && !IsOwner)
            {
                _syncPosition.Value = transform.position;
                _syncVelocity.Value = rb.velocity;

                if (state.Value == BallState.Live) PerformHitDetection();
            }
        }

        private void Update()
        {
            if (!HasAuthority) SmoothSync();
        }

        private void SmoothSync()
        {
            var interpolationFactor = Time.deltaTime / (NetworkManager.TimeManager.RoundTripTime / 2f);

            transform.position = Vector3.Lerp(transform.position, _syncPosition.Value, interpolationFactor);
            if (!rb.isKinematic)
                rb.velocity = Vector3.Lerp(rb.velocity, _syncVelocity.Value, interpolationFactor);

            visualBall.SetActive(state.Value != BallState.Possessed);
        }


        internal IEnumerator WaitForServerOwner(Vector3 throwVelocity, Vector3 position)
        {
            yield return new WaitUntil(() => HasAuthority);

            _syncPosition.Value = position;
            _syncVelocity.Value = throwVelocity;

            transform.position = position;
            rb.velocity = throwVelocity;

            // set the ball to a live state
            state.Value = BallState.Live;

            // update the visuals to net clients
            visualBall.SetActive(true);
        }

        public void ApplyThrowVelocityServerRpc(Vector3 throwVelocity, Vector3 position, HandSide handSide)
        {
            ServerOwnershipManager.ReleaseOwnershipFromServer(this, throwVelocity, position, handSide);
        }

        private void PerformHitDetection()
        {
            var latency = NetworkManager.TimeManager.RoundTripTime / 2f;
            var futurePosition = transform.position + rb.velocity * latency;

            if (!Physics.SphereCast(futurePosition, _radius, rb.velocity.normalized,
                    out var deadHit, rb.velocity.magnitude * Time.deltaTime, _deadBallLayer))
            {
                Debug.Log("Dead ball");
                state.Value = BallState.Dead;
                return;
            }

            if (!Physics.SphereCast(futurePosition, _radius, rb.velocity.normalized,
                    out var hit, rb.velocity.magnitude * Time.deltaTime, _layerMask)) return;

            Debug.Log($"Hit detected: {hit.collider.name}");
            UpdateGameState(hit.collider);
            state.Value = BallState.Dead;
        }

        private void UpdateGameState(Collider hitCollider)
        {
            var hitPlayer = hitCollider.GetComponent<DevController>();
            if (hitPlayer != null)
            {
                Debug.Log("Hit player! Player team: " + hitPlayer.team);
                // todo, hoist dodgeball hit logic to the player
                // hitPlayer.MarkAsOut();
            }
        }
    }
}