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
        private const int _MAX_SYNC_DATA_COUNT = 5;
        private const uint _TICK_UPDATE_RATE = 10;
        private const float SMOOTHING_FACTOR = 0.05f;
        private readonly Writer _writer = new();
        public readonly SyncVar<int> index = new();
        private Rigidbody rb;
        private readonly SortedDictionary<uint, Vector3> _syncPositions = new();

        private void Start()
        {
            TimeManager.OnTick += OnFramedTick;
        }

        private void OnDestroy()
        {
            TimeManager.OnTick -= OnFramedTick;
        }

        public void InitializeServerObject(int newIndex)
        {
            index.Value = newIndex;
            index.DirtyAll();
            AddReceiver();
        }

        public void AddPositionForCurrentTick(Vector3 position)
        {
            _syncPositions[TimeManager.Tick] = position;
        }

        public void AddReceiver()
        {
            rb = GetComponent<Rigidbody>();
            Debug.Log($"Adding receiver with index {index.Value}");
            NeonRavenBroadcast.AddReceiver(this, index.Value);
        }

        private void FixedUpdate()
        {
            if (!IsServerInitialized && !IsOwner)
            {
                InterpolationHelper.InterpolateSyncCollection(_syncPositions, transform);
                // InterpolateCollection(_syncPositions);
                // ClearOldSyncData(_syncPositions);
            }
        }

        private static void ClearOldSyncData(SortedDictionary<uint, Vector3> syncCollection)
        {
            while (syncCollection.Count > _MAX_SYNC_DATA_COUNT)
                syncCollection.Remove(syncCollection.Keys.First());
        }

        private void InterpolateCollection(SortedDictionary<uint, Vector3> syncCollection)
        {
            if (syncCollection.Count < 2) return;

            var ticks = new List<uint>(syncCollection.Keys);
            uint previousTick = ticks[^2];
            uint nextTick = ticks[^1];

            var previousValue = syncCollection[previousTick];
            var nextValue = syncCollection[nextTick];

            uint currentTick = ServerManager.NetworkManager.TimeManager.Tick;

            if (currentTick > nextTick)
            {
                // Extrapolate if currentTick is beyond nextTick
                float deltaTime = (currentTick - nextTick) / (float) _TICK_UPDATE_RATE;
                Vector3 velocity = (nextValue - previousValue) / (nextTick - previousTick);
                Vector3 extrapolatedValue = nextValue + velocity * deltaTime;

                transform.position = Vector3.Lerp(transform.position, extrapolatedValue, SMOOTHING_FACTOR);
                Debug.Log($"Extrapolated Value: {extrapolatedValue}, Velocity: {velocity}, DeltaTime: {deltaTime}");
                _syncPositions[nextTick] = transform.position;
            }
            else
            {
                // Interpolate if currentTick is between previousTick and nextTick
                float interpolationFactor = (float) (currentTick - previousTick) / (nextTick - previousTick);
                interpolationFactor = Mathf.Clamp(interpolationFactor, 0f, 1f); // Ensure factor is within range
                Debug.Log($"Interpolation Factor: {interpolationFactor}");

                Vector3 interpolatedPosition = Vector3.Lerp(previousValue, nextValue, interpolationFactor);
                transform.position = Vector3.Lerp(transform.position, interpolatedPosition, SMOOTHING_FACTOR);
                _syncPositions[nextTick] = transform.position;
            }
        }

        public void CleanServerPositionData(uint tick, Vector3 position, Vector3 throwVelocity)
        {
            Debug.Log("Shipping throw data to all");
            _syncPositions.Clear();
            _syncPositions[tick] = position;
            transform.position = position;
            rb.velocity = throwVelocity;
            SendPositionData();
        }

        private void OnFramedTick()
        {
            if (IsServerInitialized)
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
                $"[BALL:{index.Value}|OUT:{OwnerId}] Shipping tick: {ServerManager.NetworkManager.TimeManager.Tick}, position: {transform.position}, velocity: {rb.velocity}");

            // the IsOwner bool marks true when transfering ownership
            // we send back to our server, from the server... for some reason?
            if (OwnerId != -1) NeonRavenBroadcast.QueueSendBytes(index.Value, RavenDataIndex.BallState, _writer.GetBuffer(), true);
            else NeonRavenBroadcast.QueueSendBytes(index.Value, RavenDataIndex.BallState, _writer.GetBuffer());
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

                    if (!rb.isKinematic) rb.velocity = velocity;
                    _syncPositions[ticks] = position;
                    
                    if (OwnerId == -1) transform.position = position;
                    
                    Debug.Log(
                        $"[BALL:{index.Value}|IN:{OwnerId}] Received tick: {ticks}, position: {position}, velocity: {velocity}, actual position: {transform.position}");
                    break;
            }
        }
    }
}