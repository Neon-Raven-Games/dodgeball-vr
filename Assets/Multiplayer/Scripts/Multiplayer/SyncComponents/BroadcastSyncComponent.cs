using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Serializing;
using UnityEngine;

namespace Multiplayer.Scripts.Multiplayer.SyncComponents
{
    public class BroadcastSyncComponent : NetworkBehaviour, INeonBroadcastReceiver
    {
        private const int _MAX_SYNC_DATA_COUNT = 4;
        private const uint _TICK_UPDATE_RATE = 10;
        private readonly Writer _writer = new();
        
        public readonly SyncVar<int> index = new();
        [SerializeField] private int initializingIndex;
        private Rigidbody rb;
        private readonly SortedDictionary<uint, Vector3> _syncPositions = new();
        
        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            TimeManager.OnTick += OnFramedTick;
        }

        public void InitializeServerObject()
        {
            index.Value = initializingIndex;
            index.DirtyAll();
            AddReceiver();
        }

        public void AddReceiver()
        {
            NeonRavenBroadcast.AddReceiver(this, index.Value);
        }
        
        public void CleanServerPositionData(uint tick, Vector3 position, Vector3 throwVelocity)
        {
            _syncPositions.Clear();
            _syncPositions[tick] = position;
            transform.position = position;
            rb.velocity = throwVelocity;
            SendPositionData(); 
        }
        
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
        private void SendPositionData()
        {
            WritePositionData();
            Debug.Log(
                $"[BALL:{this.index.Value}|OUT:{OwnerId}] Shipping tick: {ServerManager.NetworkManager.TimeManager.Tick}, position: {transform.position}, velocity: {rb.velocity}");

            var index = this.index.Value;
            if (IsOwner) NeonRavenBroadcast.QueueSendBytes(index, RavenDataIndex.BallState, _writer.GetBuffer(), true);
            else NeonRavenBroadcast.QueueSendBytes(index, RavenDataIndex.BallState, _writer.GetBuffer());
        }
        
        // make abstract
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
        
        // make abstract
        public void ReceiveLazyLoadedMessage(byte[] data, int senderId, RavenDataIndex dataIndex)
        {
            switch (dataIndex)
            {
                case RavenDataIndex.BallState:
                    // if the owner, we want to toss the server data
                    // we can optimize this by not sending one back to the owner
                    // would require refactor on the broadcast side, but worth it for the 
                    // player sync
                    if (senderId != index.Value) break;

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
                        NeonRavenBroadcast.QueueSendBytes(index.Value, RavenDataIndex.BallState, data);
                    }

                    _syncPositions[ticks] = position;
                    if (!rb.isKinematic) rb.velocity = velocity;
                    
                    Debug.Log(
                        $"[BALL:{index.Value}|IN:{OwnerId}] Received tick: {ticks}, position: {position}, velocity: {velocity}, actual position: {transform.position}");
                    break;
            }
        }
    }
}