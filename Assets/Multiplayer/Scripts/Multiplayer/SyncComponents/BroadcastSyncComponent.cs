using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Serializing;
using UnityEngine;

namespace Multiplayer.Scripts.Multiplayer.SyncComponents
{
    public class BroadcastSyncComponent : NetworkBehaviour, INeonBroadcastReceiver
    {
        // todo, we can set these as serialized for now, and play with them in the inspector
        // but we should get the ball resetting first for quicker iteration
        private const int _MAX_SYNC_DATA_COUNT = 4;
        private const uint _TICK_UPDATE_RATE = 10;
        private const float SMOOTHING_FACTOR = 0.05f;
        private readonly Writer _writer = new();
        public readonly SyncVar<int> index = new();
        private Rigidbody rb;
        private readonly SortedDictionary<uint, Vector3> _syncPositions = new();

        private void Start()
        {
            Debug.Log("Assigning rigid body");
            rb = GetComponent<Rigidbody>();
            Debug.Log($"RigidBody: {rb}");
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
            Debug.Log($"Adding receiver with index {index.Value}");
            NeonRavenBroadcast.AddReceiver(this, index.Value);
        }

        private void FixedUpdate()
        {
            if (!HasAuthority && !IsOwner) InterpolateCollection(_syncPositions, true);
            ClearOldSyncData(_syncPositions);
        }

        private static void ClearOldSyncData(SortedDictionary<uint, Vector3> syncCollection)
        {
            while (syncCollection.Count > _MAX_SYNC_DATA_COUNT)
                syncCollection.Remove(syncCollection.Keys.First());
        }

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
                $"[BALL:{index.Value}|OUT:{OwnerId}] Shipping tick: {ServerManager.NetworkManager.TimeManager.Tick}, position: {transform.position}, velocity: {rb.velocity}");

            if (IsOwner) NeonRavenBroadcast.QueueSendBytes(index.Value, RavenDataIndex.BallState, _writer.GetBuffer(), true);
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