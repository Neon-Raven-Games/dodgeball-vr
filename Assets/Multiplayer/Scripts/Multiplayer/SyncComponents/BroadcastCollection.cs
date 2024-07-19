using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
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
    public class BroadcastCollection : MonoBehaviour
    {
        [SerializeField] private bool debugOneObject;
        [SerializeField] private int debugElementIndex;
        
        public List<BroadcastServerIndex> broadcastSyncComponents = new();
        private readonly Dictionary<int, BroadcastSyncComponent> _broadcastSyncComponentDictionary = new();
        
        public void InitializeServerObjects()
        {
            if (debugOneObject) InitializeBroadcastComponent(broadcastSyncComponents[debugElementIndex]);
            else foreach (var broadcastSyncComponent in broadcastSyncComponents) InitializeBroadcastComponent(broadcastSyncComponent);
            
            Debug.Log("Broadcasts initialized on server.");
        }

        private void InitializeBroadcastComponent(BroadcastServerIndex broadcastSyncComponent)
        {
            var nob = InstanceFinder.NetworkManager.GetPooledInstantiated(broadcastSyncComponent.syncComponent.gameObject,
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
            var components = FindObjectsByType<BroadcastSyncComponent>(FindObjectsSortMode.None);

            foreach (var component in components)
            {
                if (_broadcastSyncComponentDictionary.ContainsKey(component.index.Value))
                {
                    Debug.LogWarning($"Duplicate index found: {component.index.Value}. Not adding it to collection. Expect broadcasts to not be working properly.");
                    continue;
                }
                
                _broadcastSyncComponentDictionary.Add(component.index.Value, component);
                component.AddReceiver();
            }
            Debug.Log($"New receiver subscribed to {_broadcastSyncComponentDictionary.Count} components.");
        }
    }
}