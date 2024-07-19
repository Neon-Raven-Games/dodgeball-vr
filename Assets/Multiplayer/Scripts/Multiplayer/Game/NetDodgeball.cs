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

    public class NetDodgeball : NetworkBehaviour, INeonBroadcastReceiver
    {
        [SerializeField] private GameObject visualBall;
        private BroadcastSyncComponent _broadcastSyncComponent;
        #region syncvar

        public readonly SyncVar<BallState> state = new();
        public readonly SyncVar<int> ballIndex = new();

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
            // TimeManager.OnTick += OnFramedTick;
        }

        public void AddReceiver()
        {
            Debug.Log($"Adding receiver with index {ballIndex.Value}");
            NeonRavenBroadcast.AddReceiver(this, ballIndex.Value);
        }

        private void OnDestroy()
        {
            // TimeManager.OnTick -= OnFramedTick;
        }

        public void SetBallPosition(Vector3 position)
        {
            transform.position = position;
            rb.velocity = Vector3.zero;
            _syncVelocities.Clear();
            _syncPositions.Clear();
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

        internal void WaitForServerOwner(Vector3 throwVelocity, Vector3 position, uint tick)
        {
            Debug.Log($"Calling ball thrown rpc: Position: {position}, Velocity: {throwVelocity}.");

            _broadcastSyncComponent.CleanServerPositionData(tick, position, throwVelocity);
            // _syncPositions.Clear();
            // _syncPositions[tick] = position;
            // transform.position = position;
            // rb.velocity = throwVelocity;
            // SendPositionData();

            state.Value = BallState.Live;
        }

        public void ApplyThrowVelocityServerRpc(Vector3 throwVelocity, Vector3 position, HandSide handSide)
        {
            ServerOwnershipManager.ReleaseOwnershipFromServer(this, throwVelocity, position, handSide,
                TimeManager.Tick);
            _broadcastSyncComponent.AddPositionForCurrentTick(position);
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

        #region broadcast

        private const int _MAX_SYNC_DATA_COUNT = 4;
        private const uint _TICK_UPDATE_RATE = 10;
        private readonly SortedDictionary<uint, Vector3> _syncPositions = new();
        private readonly SortedDictionary<uint, Vector3> _syncVelocities = new();

        private void OnFramedTick()
        {
            if (HasAuthority && !IsOwner)
            {
                // server is owner, send actual position
                if (TimeManager.Tick % _TICK_UPDATE_RATE == 0) SendPositionData();
            }
            else if (IsOwner)
            {
                // player is owner, send actual position
                if (IsOwner && TimeManager.Tick % _TICK_UPDATE_RATE == 0) SendPositionData();
            }
        }

        // private void FixedUpdate()
        // {
        //     if (!HasAuthority && !IsOwner) InterpolateCollection(_syncPositions, true);
        //     ClearOldSyncData(_syncPositions);
        // }

        private static void ClearOldSyncData(SortedDictionary<uint, Vector3> syncCollection)
        {
            while (syncCollection.Count > _MAX_SYNC_DATA_COUNT)
                syncCollection.Remove(syncCollection.Keys.First());
        }

        private const float SMOOTHING_FACTOR = 0.05f;

        private void InterpolateCollection(SortedDictionary<uint, Vector3> syncCollection, bool isPosition)
        {
            if (syncCollection.Count < 2) return;

            var ticks = new List<uint>(syncCollection.Keys);
            uint previousTick = ticks[^2]; // Second last element
            uint nextTick = ticks[^1]; // Last element

            // todo, see if the smooth damn will work better
            // we can keep up with the velocity in a collection too, and clear
            // the collection on the server throw?
            // var velocity = rb.velocity;
            // Vector3.SmoothDamp(transform.position, syncCollection[nextTick], ref velocity, SMOOTHING_FACTOR);
            
            var previousValue = syncCollection[previousTick];
            var nextValue = syncCollection[nextTick];

            uint currentTick = ServerManager.NetworkManager.TimeManager.Tick;
            Debug.Log($"CurrentTick: {currentTick}, PreviousTick: {previousTick}, NextTick: {nextTick}");

            if (currentTick > nextTick)
            {
                // Extrapolate if currentTick is beyond nextTick
                float deltaTime = (currentTick - nextTick) / (float) _TICK_UPDATE_RATE;
                Vector3 velocity = (nextValue - previousValue) / (nextTick - previousTick);
                Vector3 extrapolatedValue = nextValue + velocity * deltaTime;

                if (isPosition)
                {
                    transform.position = Vector3.Lerp(transform.position, extrapolatedValue, SMOOTHING_FACTOR);
                }
                else if (!rb.isKinematic)
                {
                    rb.velocity = Vector3.Lerp(rb.velocity, velocity, SMOOTHING_FACTOR);
                }

                Debug.Log($"Extrapolated Value: {extrapolatedValue}, Velocity: {velocity}, DeltaTime: {deltaTime}");
            }
            else
            {
                // Interpolate if currentTick is between previousTick and nextTick
                float interpolationFactor = (float) (currentTick - previousTick) / (nextTick - previousTick);
                interpolationFactor = Mathf.Clamp(interpolationFactor, 0f, 1f); // Ensure factor is within range
                Debug.Log($"Interpolation Factor: {interpolationFactor}");

                if (isPosition)
                {
                    Vector3 interpolatedPosition = Vector3.Lerp(previousValue, nextValue, interpolationFactor);
                    transform.position = Vector3.Lerp(transform.position, interpolatedPosition, SMOOTHING_FACTOR);
                }
                else if (!rb.isKinematic)
                {
                    Vector3 interpolatedVelocity = Vector3.Lerp(previousValue, nextValue, interpolationFactor);
                    rb.velocity = Vector3.Lerp(rb.velocity, interpolatedVelocity, SMOOTHING_FACTOR);
                }
            }
        }

        private void SendPositionData()
        {
            WritePositionData();
            Debug.Log(
                $"[BALL:{ballIndex.Value}|OUT:{OwnerId}] Shipping tick: {ServerManager.NetworkManager.TimeManager.Tick}, position: {transform.position}, velocity: {rb.velocity}");

            var index = ballIndex.Value;
            if (IsOwner) NeonRavenBroadcast.QueueSendBytes(index, RavenDataIndex.BallState, _writer.GetBuffer(), true);
            else NeonRavenBroadcast.QueueSendBytes(index, RavenDataIndex.BallState, _writer.GetBuffer());
        }

        private void WritePositionData()
        {
            _writer.Reset();

            Vector3 position = transform.position;
            _writer.WriteVector3(position);

            Vector3 velocity = rb.velocity;
            _writer.WriteVector3(velocity);

            uint ticks = TimeManager.Tick;
            _writer.WriteUInt32(ticks);
        }

        public void ReceiveLazyLoadedMessage(byte[] data, int senderId, RavenDataIndex dataIndex)
        {
            Debug.Log("Wrong lazy loaded message being called");
            switch (dataIndex)
            {
                case RavenDataIndex.BallState:
                    // if the owner, we want to toss the server data
                    // we can optimize this by not sending one back to the owner
                    // would require refactor on the broadcast side, but worth it for the 
                    // player sync
                    if (senderId != ballIndex.Value) break;

                    if (IsOwner)
                    {
                        _syncPositions.Clear();
                        break;
                    }
                    var reader = new Reader(data, NetworkManager);
                    var position = reader.ReadVector3();
                    var velocity = reader.ReadVector3();
                    var ticks = reader.ReadUInt32();

                    if (IsServerInitialized)
                    {
                        Debug.Log("Received ball state data from server, updating client positions.");
                        _syncPositions.Clear();
                        transform.position = position;
                        _syncPositions[ticks] = position;
                        NeonRavenBroadcast.QueueSendBytes(ballIndex.Value, RavenDataIndex.BallState, data);
                    }

                    _syncPositions[ticks] = position;
                    if (!rb.isKinematic) rb.velocity = velocity;
                    Debug.Log(
                        $"[BALL:{ballIndex.Value}|IN:{OwnerId}] Received tick: {ticks}, position: {position}, velocity: {velocity}, actual position: {transform.position}");
                    break;
            }
        }

        #endregion
    }
}