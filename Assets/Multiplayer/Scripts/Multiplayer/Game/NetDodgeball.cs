using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Serializing;
using FishNet.Transporting;
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

        #region syncvar

        public float interpolateFactor = 0.1f;

        // public readonly SyncVar<Vector3> _syncPosition = new();
        // public readonly SyncVar<Vector3> _syncVelocity = new();
        public readonly SyncVar<BallState> state = new();
        public readonly SyncVar<int> ballIndex = new();

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

            // TimeManager.OnTick += TickUpdate;
            TimeManager.OnTick += OnFramedTick;
        }

        private void OnDestroy()
        {
            TimeManager.OnTick -= OnFramedTick;
            // NetworkManager.TransportManager.Transport.OnServerReceivedData -= TugboatOnOnServerReceivedData;
        }

        private const int _MAX_SYNC_DATA_COUNT = 100;
        private const uint _TICK_UPDATE_RATE = 10;
        private uint _lastTick;
        private readonly SortedDictionary<uint, Vector3> _syncPositions = new();
        private readonly SortedDictionary<uint, Vector3> _syncVelocities = new();

        // tick count on client is huge compared to server
        // timestamp?
        private void OnFramedTick()
        {
            if (HasAuthority && !IsOwner)
            {
                if (TimeManager.Tick % _TICK_UPDATE_RATE == 0) SendPositionData();
                InterpolateCollection(_syncPositions, true);
                InterpolateCollection(_syncVelocities, false);
            }
            else if (NetworkManager.ClientManager.Clients.ContainsKey(OwnerId))
            {
                InterpolateCollection(_syncPositions, true);
                InterpolateCollection(_syncVelocities, false);
                if (IsOwner && TimeManager.Tick % _TICK_UPDATE_RATE == 0) SendPositionData();
            }

            // Clear outdated data
            ClearOldSyncData(_syncPositions);
            ClearOldSyncData(_syncVelocities);
        }

        private static void ClearOldSyncData(SortedDictionary<uint, Vector3> syncCollection)
        {
            while (syncCollection.Count > _MAX_SYNC_DATA_COUNT)
                syncCollection.Remove(syncCollection.Keys.First());
        }

        private void InterpolateCollection(SortedDictionary<uint, Vector3> syncCollection, bool position)
        {
            if (syncCollection.Count == 0) return;

            var ticks = new List<uint>(syncCollection.Keys);
            var previousTick = ticks[0];
            var nextTick = ticks[0];

            foreach (var tick in ticks)
            {
                if (tick <= _lastTick)
                {
                    previousTick = tick;
                }
                else
                {
                    nextTick = tick;
                    break;
                }
            }

            if (previousTick == nextTick) return;

            var previousPosition = syncCollection[previousTick];
            var nextPosition = syncCollection[nextTick];

            // Interpolate based on the time passed between ticks
            var interpolationFactor = (float) (ServerManager.NetworkManager.TimeManager.Tick - previousTick) /
                                      (nextTick - previousTick);

            if (position)
                transform.position = Vector3.Lerp(previousPosition, nextPosition, interpolationFactor);
            else if (!rb.isKinematic)
                rb.velocity = Vector3.Lerp(previousPosition, nextPosition, interpolationFactor);
        }

        private void SendPositionData()
        {
            var writer = WriterPool.Retrieve(NetworkManager);
            writer.Reset();
            // writer.WritePacketId(PacketId.DodgeBallUpdate);
            writer.WriteUInt32((uint)ballIndex.Value);
            writer.WriteVector3(transform.position);
            writer.WriteVector3(rb.velocity);
            writer.WriteUInt32(1);
            writer.Store();
            Debug.Log("Writer Position: " + writer.Position);
            Debug.Log(
                $"[BALL:{ballIndex.Value}|OUT:{OwnerId}] Shipping tick: {ServerManager.NetworkManager.TimeManager.Tick}, position: {transform.position}, velocity: {rb.velocity}");

            if (IsOwner)
            {
                Debug.Log($"Sending to server from {OwnerId}.");
                NetworkManager.TransportManager.Transport.SendToServer((byte) Channel.Unreliable,
                    writer.GetArraySegment());
            }
            
            foreach (var conn in NetworkManager.ServerManager.Clients.Values)
            {
                NetworkManager.TransportManager.Transport.SendToClient((byte) Channel.Unreliable,
                    writer.GetArraySegment(), conn.ClientId);
            }
        }

        #region server

        public override void OnStartServer()
        {
            base.OnStartServer();
            // NetworkManager.TransportManager.Transport.OnServerReceivedData += TugboatOnOnServerReceivedData;
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            Debug.Log("Starting network");
        }

        // private void TugboatOnOnServerReceivedData(ServerReceivedDataArgs obj)
        // {
        //     var reader = ReaderPool.Retrieve(obj.Data, NetworkManager);
        //     var messageType = reader.PeekPacketId();
        //     switch (messageType)
        //     {
        //         case PacketId.DodgeBallUpdate:
        //             Debug.Log("ReadPacketId: DodgeBallUpdate");
        //
        //             int currentPosition = reader.Position;
        //             PacketId result = reader.ReadPacketId();
        //             if (reader.ReadUInt32() != ballIndex.Value)
        //             {
        //                 reader.Position = currentPosition;
        //                 break;
        //             }
        //             
        //             var position = reader.ReadVector3();
        //             var velocity = reader.ReadVector3();
        //             var ticks = reader.ReadUInt32();
        //             _syncPositions[ticks] = position;
        //             _syncVelocities[ticks] = velocity;
        //             _lastTick = ticks;
        //             reader.Store();
        //             Debug.Log(
        //                 $"[BALL:{ballIndex.Value}|IN:{OwnerId}] Received tick: {ticks}, position: {position}, velocity: {velocity}, actual position: {transform.position}");
        //             break;
        //     }
        // }

        #endregion

        #region client

        // public void SubscribeClientToData() =>
            // NetworkManager.TransportManager.Transport.OnClientReceivedData += HandleReceivedData;

        // private void HandleReceivedData(ClientReceivedDataArgs clientReceivedDataArgs)
        // {
        //     var reader = ReaderPool.Retrieve(clientReceivedDataArgs.Data, NetworkManager);
        //     var messageType = reader.PeekPacketId();
        //     switch (messageType)
        //     {
        //         case PacketId.DodgeBallUpdate:
        //             int currentPosition = reader.Position;
        //             PacketId result = reader.ReadPacketId();
        //             if (reader.ReadUInt32() != ballIndex.Value)
        //             {
        //                 reader.Position = currentPosition;
        //                 break;
        //             }
        //
        //             var position = reader.ReadVector3();
        //             var velocity = reader.ReadVector3();
        //             var ticks = reader.ReadUInt32();
        //             _syncPositions[ticks] = position;
        //             _syncVelocities[ticks] = velocity;
        //             _lastTick = ticks;
        //             reader.Store();
        //             Debug.Log(
        //                 $"[BALL:{ballIndex.Value}|IN:{OwnerId}] Received tick: {ticks}, position: {position}, velocity: {velocity}, actual position: {transform.position}");
        //             break;
        //     }
        // }

        #endregion

        public void SetBallPosition(Vector3 position)
        {
            // _syncPosition.Value = position;
            // _syncVelocity.Value = Vector3.zero;

            transform.position = position;
            rb.velocity = Vector3.zero;
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

        private bool _updatedForLive = false;
/*
        private void TickUpdate()
        {
            if (HasAuthority && !IsOwner && state.Value == BallState.Possessed)
            {
                if (!_updatedForLive)
                {
                    _updatedForLive = true;
                    transform.position = _syncPosition.Value;
                    rb.velocity = _syncVelocity.Value;
                    Debug.Log(
                        $"[Server Tick] Updating position and velocity before switch state to live. If called more than once, set bool flag." +
                        $"Setting position to Sync: {_syncPosition.Value}, " +
                        $"Setting Velocity to Sync: {_syncVelocity.Value}");
                }
                else
                {
                    _updatedForLive = false;
                    state.Value = BallState.Live;
                    Debug.Log(
                        $"[Server Tick] Setting Ball Live, updating velocity and position closer to client received. " +
                        $"Sync Position: {_syncPosition.Value}, Ball Position: {transform.position}" +
                        $"Sync Velocity: {_syncVelocity.Value}, Ball Velocity: {rb.velocity}");
                }
            }
            else if (HasAuthority && !IsOwner && state.Value != BallState.Possessed)
            {
                var refreshPosition = _syncPosition.Value != transform.position;
                if (refreshPosition)
                {
                    Debug.Log($"[Server Tick] Before Overwriting to refresh positions: " +
                              $"Sync Position: {_syncPosition.Value}, Actual Position: {transform.position}" +
                              $"Sync Velocity: {_syncVelocity.Value}, Actual Velocity: {rb.velocity}");
                    _syncPosition.Value = transform.position;
                    _syncVelocity.Value = rb.velocity;
                    Debug.Log($"[Server Tick] After Overwriting to refresh positions: " +
                              $"Sync Position: {_syncPosition.Value}, Actual Position: {transform.position}" +
                              $"Sync Velocity: {_syncVelocity.Value}, Actual Velocity: {rb.velocity}");
                }

                if (state.Value == BallState.Live) PerformHitDetection();
            }
            else if (!HasAuthority && !IsOwner && state.Value == BallState.Possessed)
            {
                Debug.Log($"[Networked Client Tick] Sync positions while ball is possessed." +
                          $"Position is: {_syncPosition.Value} " +
                          $"Velocity is: {_syncVelocity.Value}");
            }
            else if (!HasAuthority && !IsOwner)
            {
                SmoothSync();
                Debug.Log("[Networked Client Tick] Smooth Syncing position across clients." +
                          $"Sync Position: {_syncPosition.Value}, Regular Position: {transform.position}" +
                          $"Sync Velocity: {_syncVelocity.Value}, Regular Velocity: {rb.velocity}");
            }
        }
*/

        private void TestingData()
        {
            var writer = new PooledWriter();
            // writer.Write(_syncPosition.Value);
            // writer.Write(_syncVelocity.Value);
            writer.Write(TimeManager.Tick);

            NetworkManager.TransportManager.Transport.SendToClient((byte) Channel.Unreliable, writer.GetArraySegment(),
                OwnerId);
        }


        private void Update()
        {
            // if (!IsOwner && state.Value != BallState.Possessed) SmoothSync();
            // if it's possessed, set the visuals inactive
            visualBall.SetActive(IsOwner || state.Value != BallState.Possessed);
        }

        private void SmoothSync()
        {
            // transform.position = Vector3.Lerp(transform.position, _syncPosition.Value, interpolateFactor);
            // if (!rb.isKinematic)
            // rb.velocity = Vector3.Lerp(rb.velocity, _syncVelocity.Value, interpolateFactor);
        }

        [Rpc]
        internal void WaitForServerOwner(Vector3 throwVelocity, Vector3 position, uint tick)
        {
            var passedTime = (float) TimeManager.TimePassed(tick);
            passedTime = Mathf.Min(MAX_PASSED_TIME / 2f, passedTime);

            Debug.Log($"Calling ball thrown rpc: Position: {position}, Velocity: " +
                      $"{throwVelocity}. Time passed since throw: {passedTime}");
            _syncPositions.Clear();
            _syncVelocities.Clear();

            var futurePosition = position + throwVelocity * passedTime;
            transform.position = futurePosition;
            rb.velocity = throwVelocity;

            _syncPositions[tick] = position;
            _syncPositions[tick + 1] = futurePosition;

            _syncVelocities[tick] = throwVelocity;
            _syncVelocities[tick + 1] = throwVelocity;

            state.Value = BallState.Live;
            BroadcastBallState(futurePosition, throwVelocity, tick);
        }

        private void BroadcastBallState(Vector3 position, Vector3 velocity, uint tick)
        {
            var writer = new PooledWriter();
            // writer.WritePacketId(PacketId.DodgeBallUpdate);
            writer.Write(position);
            writer.Write(velocity);
            writer.Write(tick);

            foreach (var conn in NetworkManager.ServerManager.Clients.Values)
                NetworkManager.TransportManager.Transport.SendToClient((byte) Channel.Reliable,
                    writer.GetArraySegment(), conn.ClientId);
        }

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