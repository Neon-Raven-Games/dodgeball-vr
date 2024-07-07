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
        private readonly SyncVar<Vector3> _syncPosition = new();
        private readonly SyncVar<Vector3> _syncVelocity = new();
        private Rigidbody rb;
        // do we need this anymore?
        public int index { get; set; }
        
        // we should use a sync var for this and set it before transferring ownership
        public Team team { get; set; }
        public BallType type { get; set; }

        public readonly SyncVar<BallState> state = new();

        [SerializeField] private GameObject visualBall;
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

        private void Update()
        {
            // this !IsOwner is breaking the ball
            if (HasAuthority && !IsOwner)
            {
                _syncPosition.Value = transform.position;
                _syncVelocity.Value = rb.velocity;

                if (state.Value == BallState.Live) PerformHitDetection();
            }
            else if (!IsOwner)
            {
                // todo, this was 10. Using prediction seemed more fitting here to keep the ball in sync and smooth
                transform.position = Vector3.Lerp(transform.position, _syncPosition.Value, Time.deltaTime * (NetworkManager.TimeManager.RoundTripTime / 2f));
                if (!rb.isKinematic)
                    rb.velocity = Vector3.Lerp(rb.velocity, _syncVelocity.Value, Time.deltaTime * (NetworkManager.TimeManager.RoundTripTime / 2f));

                if (state.Value == BallState.Possessed) 
                    visualBall.SetActive(false);
            }
        }

        public void ApplyThrowVelocityServerRpc(Vector3 throwVelocity, Vector3 position)
        {
            ServerOwnershipManager.ReleaseOwnershipFromServer(this, throwVelocity, position);
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