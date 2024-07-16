using System;
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
        public float interpolateFactor = 0.1f;

        private readonly SyncVar<Vector3> _syncPosition = new();
        private readonly SyncVar<Vector3> _syncVelocity = new();

        public readonly SyncVar<BallState> state = new();
        private const float MAX_PASSED_TIME = 0.3f;
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
            TimeManager.OnTick += TickUpdate;
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

        private void TickUpdate()
        {
            if (HasAuthority && !IsOwner && state.Value != BallState.Possessed)
            {
                _syncPosition.Value = transform.position;
                _syncVelocity.Value = rb.velocity;

                if (state.Value == BallState.Live) PerformHitDetection();
            }

            if (HasAuthority && !IsOwner && state.Value == BallState.Possessed)
            {
                Debug.Log($"Setting ballstate live. Sync var position: {_syncPosition.Value}, Sync var velocity: {_syncVelocity.Value}");
                transform.position = _syncPosition.Value;
                rb.velocity = _syncVelocity.Value;
                state.Value = BallState.Live;
            }
        }

        
        private void Update()
        {
            // if (!IsOwner && state.Value != BallState.Possessed) SmoothSync();
            
            // if it's possessed, set the visuals inactive
            visualBall.SetActive(IsOwner || state.Value != BallState.Possessed);
        }

        private void SmoothSync()
        {
            // transform.position =  Vector3.Lerp(transform.position, _syncPosition.Value, interpolateFactor);
            // if (!rb.isKinematic)
                // rb.velocity = Vector3.Lerp(rb.velocity, _syncVelocity.Value, interpolateFactor);
        }
        
        internal void WaitForServerOwner(Vector3 throwVelocity, Vector3 position, uint tick)
        {
            var passedTime = (float) TimeManager.TimePassed(tick);
            passedTime = Mathf.Min(MAX_PASSED_TIME / 2f, passedTime);
            
            Debug.Log($"Calling ball thrown rpc: Position: {position}, Velocity: {throwVelocity}. Time passed since throw: {passedTime}");
         
            var futurePosition = position + throwVelocity * passedTime;
            _syncPosition.Value = futurePosition;
            _syncVelocity.Value = throwVelocity;
            rb.velocity = throwVelocity;
        }


        public void ApplyThrowVelocityServerRpc(Vector3 throwVelocity, Vector3 position, HandSide handSide)
        {
            ServerOwnershipManager.ReleaseOwnershipFromServer(this, throwVelocity, position, handSide, TimeManager.Tick);
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
            var hitPlayer = hitCollider.GetComponent<NetIKTargetHelper>();
            if (hitPlayer != null)
            {
                Debug.Log("Hit player! Need to get team from layer mask :3 ");
                // todo, hoist dodgeball hit logic to the player
                // hitPlayer.MarkAsOut();
            }
        }
    }
}