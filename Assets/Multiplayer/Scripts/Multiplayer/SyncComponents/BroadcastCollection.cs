using System;
using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Object;
using UnityEngine;


namespace Multiplayer.Scripts.Multiplayer.SyncComponents
{
    [Serializable]
    public class BroadcastServerIndex
    {
        public int index;
        public Vector3 initialPosition;
        public BroadcastSyncComponent syncComponent;
    }

    // as of now, broadcast positive values are coupled on the 
    // network connection for players (>=0)
    // -1 is reserved for the server. 
    // Populate the collection in any way with negative indicies below -1
    public class BroadcastCollection : MonoBehaviour
    {
        public static event Action BroadcastsInitialized;
        [SerializeField] private bool debugOneObject;
        [SerializeField] private int debugElementIndex;

        public List<BroadcastServerIndex> broadcastSyncComponents = new();
        private readonly Dictionary<int, BroadcastSyncComponent> _broadcastSyncComponentDictionary = new();

        private void Awake()
        {
            BroadcastsInitialized += SubscribeNewReceiver;
        }

        public void ReInitializeServerObjects()
        {
            var components = _broadcastSyncComponentDictionary.Values.ToList();
            foreach (var component in components)
            {
                component.RemoveReceiver();
                var netBall = component.GetComponent<NetworkObject>();
                InstanceFinder.NetworkManager.ServerManager.Despawn(netBall);
            }
            _broadcastSyncComponentDictionary.Clear();
            InitializeServerObjects();
            foreach (var component in InstanceFinder.ServerManager.Clients.Values)
            {
                Debug.Log($"Shipping ball reset message to client {component.ClientId}");
                NeonRavenBroadcast.SendEmptyMessage(-1, RavenDataIndex.BallReset, false, component.ClientId);
            }
        }
        
        public void InitializeServerObjects()
        {
            if (debugOneObject) InitializeBroadcastComponent(broadcastSyncComponents[debugElementIndex]);
            else
                foreach (var broadcastSyncComponent in broadcastSyncComponents)
                    InitializeBroadcastComponent(broadcastSyncComponent);

            Debug.Log("Broadcasts initialized on server.");
        }
        
        public static void OnBroadcastsInitialized() =>
            BroadcastsInitialized?.Invoke();

        public void RemoveReceivers()
        {
            foreach (var component in _broadcastSyncComponentDictionary.Values) component.RemoveReceiver();
        }

        public Vector3 GetSpawnPosition(int index)
        {
            var obj = broadcastSyncComponents.Find(x => x.index == index);
            if (obj == null)
            {
                Debug.LogWarning($"No spawn position found for index: {index}");
                return Vector3.zero;
            }

            return obj.initialPosition;
        }

        private void InitializeBroadcastComponent(BroadcastServerIndex broadcastSyncComponent)
        {
            var nob = InstanceFinder.NetworkManager.GetPooledInstantiated(
                broadcastSyncComponent.syncComponent.gameObject,
                broadcastSyncComponent.initialPosition, Quaternion.identity, true);
            InstanceFinder.NetworkManager.ServerManager.Spawn(nob);

            var syncComponent = nob.GetComponent<BroadcastSyncComponent>();
            syncComponent.InitializeServerObject(broadcastSyncComponent.index);

            if (_broadcastSyncComponentDictionary.ContainsKey(broadcastSyncComponent.index))
            {
                Debug.LogWarning(
                    $"Duplicate index found: {broadcastSyncComponent.index}. Not adding it to collection. Expect broadcasts to not be working properly.");
                return;
            }

            _broadcastSyncComponentDictionary.Add(broadcastSyncComponent.index, syncComponent);
        }

        public BroadcastSyncComponent GetComponentByIndex(int index) =>
            _broadcastSyncComponentDictionary.ContainsKey(index) ? _broadcastSyncComponentDictionary[index] : null;

        public void SubscribeNewReceiver()
        {
            Debug.Log("New receiver subscribed to broadcasts.");
            var components = FindObjectsByType<BroadcastSyncComponent>(FindObjectsSortMode.None);

            foreach (var component in components)
            {
                if (_broadcastSyncComponentDictionary.ContainsKey(component.index.Value))
                {
                    component.AddReceiver();
                    // if this works, safe to remove logs
                    Debug.LogWarning($"Duplicate index found: {component.index.Value}. Was this reinitialized?");
                    continue;
                }
                component.AddReceiver();
                _broadcastSyncComponentDictionary.Add(component.index.Value, component);
            }

            Debug.Log($"New receiver subscribed to {_broadcastSyncComponentDictionary.Count} components.");
        }
    }
}