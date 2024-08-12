using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Serializing;
using Multiplayer.Scripts.Multiplayer.SyncComponents;
using UnityEngine;


namespace Unity.Template.VR.Multiplayer
{
    public enum MessageType : byte
    {
        DodgeballUpdate = 100,
    }

    [Serializable]
    public class DodgeballIndex
    {
        public BallType type;
        public GameObject dodgeball;
    }

    public class NetDodgeball : NetworkBehaviour
    {
        [SerializeField] private GameObject visualBall;
        private BroadcastSyncComponent _broadcastSyncComponent;
        #region syncvar

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
        private readonly Writer _writer = new();

        private void Start()
        {
            rb = GetComponent<Rigidbody>();

            _deadBallLayer = 1 << LayerMask.NameToLayer("Ground");

            var teamOneLayer = LayerMask.NameToLayer("TeamOne");
            var teamTwoLayer = LayerMask.NameToLayer("TeamTwo");
            _layerMask = (1 << teamOneLayer) | (1 << teamTwoLayer);
            _radius = GetComponent<SphereCollider>().radius;

            _broadcastSyncComponent = GetComponent<BroadcastSyncComponent>();
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
            visualBall.SetActive(IsOwner || state.Value != BallState.Possessed);
        }

        internal void WaitForServerOwner() => state.Value = BallState.Live;

        public void ApplyThrowVelocityServerRpc(Vector3 throwVelocity, Vector3 position, HandSide handSide)
        {
            ServerOwnershipManager.ReleaseOwnershipFromServer(this, throwVelocity, position, handSide,
                TimeManager.Tick);
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
            RegisterHit(hit.collider);
            state.Value = BallState.Dead;
        }

        private static void RegisterHit(Collider hitCollider)
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