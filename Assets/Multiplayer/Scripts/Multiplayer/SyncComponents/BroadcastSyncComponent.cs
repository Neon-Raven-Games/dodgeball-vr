using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Serializing;
using Multiplayer.Scripts.Multiplayer.Util;
using UnityEngine;

namespace Multiplayer.Scripts.Multiplayer.SyncComponents
{
    public class BroadcastSyncComponent : NetworkBehaviour, INeonBroadcastReceiver
    {
        // todo, we can set these as serialized for now, and play with them in the inspector
        // but we should get the ball resetting first for quicker iteration
        private const uint _TICK_UPDATE_RATE = 10;
        private readonly Writer _writer = new();
        public readonly SyncVar<int> index = new();
        private readonly SortedDictionary<uint, Vector3> _syncPositions = new();

        private Rigidbody _rb;
        private ServerMessage _serverMessage;
        private bool _inThrow;
        private void Start() => TimeManager.OnTick += OnFramedTick;

        private void OnDisable() => TimeManager.OnTick -= OnFramedTick;

        public void InitializeServerObject(int newIndex)
        {
            index.Value = newIndex;
            index.DirtyAll();
            AddReceiver();
        }

        private const float MAX_PASSED_TIME = 0.3f;

        public void AddPositionForCurrentTick(uint tick, Vector3 position, Vector3 throwVelocity)
        {
            Debug.Log("Receiving throw data from broadcast");
            transform.position = position;
            _syncPositions.Clear();
            var passedTime = (float) TimeManager.TimePassed(tick);
            passedTime = Mathf.Min(MAX_PASSED_TIME / 2f, passedTime);

            Debug.Log(
                $"Time passed since throw: {passedTime}. " +
                $"[{tick}] {position}" +
                $"[{TimeManager.Tick}] {position + throwVelocity * passedTime}");

            var futurePosition = position + throwVelocity * passedTime;
            _syncPositions[tick] = position;
            _syncPositions[TimeManager.Tick] = futurePosition;
            Debug.Log(
                $"Initial Sync Positions: {_syncPositions.Select(kvp => kvp.ToString()).Aggregate((a, b) => $"{a}, {b}")}");
        }

        public void AddReceiver()
        {
            _rb = GetComponent<Rigidbody>();
            _serverMessage = ServerMessage.Create(index.Value, RavenDataIndex.BallState, null, index.Value);
            Debug.Log($"Adding receiver with index {index.Value}");
            NeonRavenBroadcast.AddReceiver(this, index.Value);
        }

        public void RemoveReceiver()
        {
            Debug.Log($"Removing receiver with index {index.Value}");
            NeonRavenBroadcast.RemoveReceiver(this, index.Value);
        }

        public float smoothFactor = 1f;

        private void FixedUpdate()
        {
            if (!IsServerInitialized && !IsOwner)
                InterpolationHelper.InterpolateSyncCollection(_syncPositions, transform, smoothFactor);
        }

        public void ThrowBallOnTick(uint tick, Vector3 position, Vector3 throwVelocity)
        {
            throwTickDelay = 2;
            Debug.Log("Shipping throw data to all via broadcast");
            transform.position = position;
            _rb.velocity = throwVelocity;
            var data = WriteThrowData(position, throwVelocity, tick);
            NeonRavenBroadcast.QueueSendBytes(index.Value, RavenDataIndex.BallThrow, data, false, index.Value);
        }

        private uint throwTickDelay;
        private void OnFramedTick()
        {
            if (throwTickDelay > 0)
            {
                throwTickDelay--;
                Debug.Log("Delayed from throw tick");
                return;
            }
            
            if (HasAuthority && !IsOwner)
            {
                // server is owner, send actual position
                if (TimeManager.Tick % _TICK_UPDATE_RATE == 0) SendPositionData();
            }
            else if (IsOwner)
            {
                // player is owner, send actual position
                if (TimeManager.Tick % _TICK_UPDATE_RATE == 0) SendPositionData();
            }
        }

        private void SendPositionData()
        {
            WritePositionData();
            Debug.Log(
                $"[BALL:{index.Value}|OUT:{OwnerId}] Shipping tick: {ServerManager.NetworkManager.TimeManager.Tick}, position: {transform.position}, velocity: {_rb.velocity}");

            if (OwnerId != -1) SendServerData();
            else
            {
                NeonRavenBroadcast.QueueSendBytes(index.Value, RavenDataIndex.BallState, _writer.GetBuffer(), false,
                    index.Value);
            }
        }

        private void SendServerData()
        {
            _serverMessage.data = _writer.GetBuffer();
            NeonRavenBroadcast.QueueSendBytes(_serverMessage);
        }

        // make abstract
        private void WritePositionData()
        {
            _writer.Reset();

            Vector3 position = transform.position;
            _writer.WriteVector3(position);

            Vector3 velocity = _rb.velocity;
            _writer.WriteVector3(velocity);

            uint ticks = TimeManager.Tick;
            _writer.WriteUInt32(ticks);
        }

        private static byte[] WriteThrowData(Vector3 position, Vector3 velocity, uint ticks)
        {
            var writer = new Writer();
            writer.Reset();

            writer.WriteVector3(position);
            writer.WriteVector3(velocity);
            writer.WriteUInt32(ticks);
            return writer.GetBuffer();
        }
        

        public void ReceiveLazyLoadedMessage(byte[] data, int senderId, RavenDataIndex dataIndex)
        {
            switch (dataIndex)
            {
                case RavenDataIndex.BallThrow:
                    _syncPositions.Clear();
                    var throwReader = new Reader(data, NetworkManager);
                    var pos = throwReader.ReadVector3();
                    transform.position = pos;
                    
                    var vel = throwReader.ReadVector3();
                    var tick = throwReader.ReadUInt32();
                    AddPositionForCurrentTick(tick, pos, vel);
                    throwTickDelay = 0;
                    Debug.Log($"resetting throw delay in broadcast receiver {NetworkObject.LocalConnection.ClientId}");
                    break;
                case RavenDataIndex.BallState:
                    if (senderId != index.Value)
                    {
                        Debug.Log($"[IN:{index.Value} OUT:{senderId}] Received data from wrong ball index ");
                        return;
                    }

                    if (IsOwner)
                    {
                        _syncPositions.Clear();
                        break;
                    }

                    var reader = new Reader(data, NetworkManager);
                    var position = reader.ReadVector3();
                    var velocity = reader.ReadVector3();
                    var ticks = reader.ReadUInt32();

                    if (!_rb.isKinematic) _rb.velocity = velocity;
                    _syncPositions[ticks] = position;

                    if (HasAuthority && !IsOwner)
                    {
                        transform.position = position;
                    }

                    Debug.Log(
                        $"[BALL:{index.Value}|IN:{OwnerId}] Received tick: {ticks}, position: {position}, velocity: {velocity}, actual position: {transform.position}");
                    break;
            }
        }
    }
}