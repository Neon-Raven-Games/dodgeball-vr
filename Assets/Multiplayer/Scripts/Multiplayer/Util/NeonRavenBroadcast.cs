using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Connection;
using FishNet.Managing.Client;
using FishNet.Managing.Server;
using FishNet.Transporting;
using UnityEngine;


public class NeonRavenBroadcast : MonoBehaviour
{
    public const int MAX_SIZE = 32000;
    private const int _MAX_SEND_PER_ATTEMPT = 1;
    private const float _SEND_INTERVAL = 0.1f;

    public bool debug;
    private static RavenMessage _currentlyProcessing;
    private static bool _hasCurrentlyProcessing;

    /// <summary>
    /// The message pool for handling data effeciently.
    /// </summary>
    private static readonly RavenMessagePool _SPool = new();

    /// <summary>
    /// Subscribers to the messages, set in <see cref="AddReceiver"/> and <see cref="RemoveReceiver"/>.
    /// </summary>
    private static readonly Dictionary<int, INeonBroadcastReceiver> _SReceivers = new();

    /// <summary>
    /// The message queue to enqueue messages to send.
    /// </summary>
    private static readonly Queue<RavenMessage> _SSendQueue = new();
    private readonly Dictionary<int, RavenMessage> _receiverPoolMap = new();

    private float _lastTimeCheckedSendQueue;
    private byte[] _sendBuffer;
    
    #region Singleton - Possible Refactor Needed

    // todo, we probably don't need this to be singleton, and can make this a static class.
    private static bool DoDebug => _instanced && _instance.debug;
    private static NeonRavenBroadcast _instance;
    private static bool _instanced;

    // todo, if we do refactor singleton out of the equation and make static class, we can remove this, and pass in the
    // respective managers in the constructor overload
    private static ServerManager ServerManager => InstanceFinder.ServerManager;
    private static ClientManager ClientManager => InstanceFinder.ClientManager;

    // todo, validate this is the correct way to check if it is the server
    private static bool IsServer => InstanceFinder.IsServerStarted;

    #endregion

    #region public api. todo - bring in the byte array helper class

    /// <summary>
    /// Queue bytes to send to clients or server. This is helpful for sending large data packets that need to be broken up.
    /// This is the main way to send data to clients or server.
    /// </summary>
    /// <param name="senderID">Client Id of who is sending/queuing the data.</param>
    /// <param name="dataIndex">The type of data you defined this message as.</param>
    /// <param name="data">The data to send to the server.</param>
    /// <param name="sendToServer">If true, sends only to the server.</param>
    /// <param name="toSpecificID">If -1, send to all clients, otherwise the client who is the target.</param>
    public static void QueueSendBytes(int senderID, RavenDataIndex dataIndex, byte[] data, bool sendToServer = false,
        int toSpecificID = -1)
    {
        if (DoDebug)
            Debug.Log(string.Format(
                $"Send {data.Length} to {(sendToServer ? "server" : (toSpecificID != -1 ? $"client {toSpecificID}" : "all clients"))}. It will take {data.Length / MAX_SIZE} messages. Should be done in {(data.Length / MAX_SIZE) / _MAX_SEND_PER_ATTEMPT} frames, sending {MAX_SIZE * _MAX_SEND_PER_ATTEMPT} per frame"));

        var slowSender = _SPool.Allocate();
        slowSender.Initialize(data, MAX_SIZE);
        slowSender.dataIndex = dataIndex;
        slowSender.totalSize = data.Length;
        slowSender.senderID = senderID;
        slowSender.sendingToServer = sendToServer;
        slowSender.targetClientId = toSpecificID;
        _SSendQueue.Enqueue(slowSender);
    }

    /// <summary>
    /// Add a receiver to the messages sent.
    /// </summary>
    /// <param name="receiver">Interface's concrete type to remove to messaging.</param>
    /// <param name="receiverId">The receiver Id</param>
    public static void AddReceiver(INeonBroadcastReceiver receiver, int receiverId)
    {
        if (!_SReceivers.ContainsKey(receiverId)) _SReceivers.Add(receiverId, receiver);
    }

    /// <summary>
    /// Remove a receiver from the messages sent.
    /// </summary>
    /// <param name="receiver">Interface's concrete type to remove from messaging.</param>
    /// <param name="receiverId">the receive id</param>
    public static void RemoveReceiver(INeonBroadcastReceiver receiver, int receiverId)
    {
        if (_SReceivers.ContainsKey(receiverId)) _SReceivers.Remove(receiverId);
    }

    #endregion

    #region MonoBehavior Callbacks

    private void Awake()
    {
        _SReceivers.Clear();
        _instanced = true;
        if (_instance == null) _instance = this;
        else
        {
            Debug.LogError(
                $"More than one message handler is in the scene. Deleting {gameObject.name}. " +
                $"Ensure that only one handler is in the scene at a time.");

            Destroy(gameObject);
        }
    }

    public static void Initialize()
    {
        ClientManager.RegisterBroadcast<RavenMessageSegment>(_instance.ReceiveLocalClientBroadcast);
        ServerManager.RegisterBroadcast<RavenMessageSegment>(_instance.ReceiveLocalServerBroadcast);
    }

    private void OnDisable()
    {
        if (ClientManager != null) ClientManager.UnregisterBroadcast<RavenMessageSegment>(ReceiveLocalClientBroadcast);
        if (ServerManager != null) ServerManager.UnregisterBroadcast<RavenMessageSegment>(ReceiveLocalServerBroadcast);
    }

    private void OnDestroy()
    {
        _hasCurrentlyProcessing = false;
        _currentlyProcessing = null;
    }

    // todo, to keep consistent with the network, we could probably move this over to OnTick callbacks.
    private void Update()
    {
        // if (WaitForFrame()) return;

        if (_hasCurrentlyProcessing)
        {
            var sentThisFrame = 0;
            while (sentThisFrame < _MAX_SEND_PER_ATTEMPT && _currentlyProcessing != null &&
                   _currentlyProcessing.ReadChunk(out _sendBuffer))
            {
                var completed = !_currentlyProcessing.HasBytesLeft();
                var newBroadcast = CreateBroadcastMessage(completed);

                if (_currentlyProcessing.sendingToServer) BroadcastMessageToServer(newBroadcast);
                else BroadcastMessageToClients(newBroadcast);

                if (completed) Complete();

                if (_hasCurrentlyProcessing && _currentlyProcessing != null)
                    _currentlyProcessing.currentSlowSenderIndex++;

                sentThisFrame++;
            }

            if (DoDebug && _hasCurrentlyProcessing)
                Debug.Log($"Done round of sending still has to send {_currentlyProcessing.BytesLeft}");
        }
        else if (_SSendQueue.Count > 0 && !_hasCurrentlyProcessing)
        {
            _currentlyProcessing = _SSendQueue.Dequeue();
            _hasCurrentlyProcessing = _currentlyProcessing != null;
        }
    }

    #endregion

    #region Control Flow

    /// <summary>
    /// Timer function to wait for the next frame to send more data. todo, change this to tick based logic
    /// </summary>
    /// <returns></returns>
    private bool WaitForFrame()
    {
        if (_lastTimeCheckedSendQueue + _SEND_INTERVAL > Time.realtimeSinceStartup) return true;
        _lastTimeCheckedSendQueue = Time.realtimeSinceStartup;
        return false;
    }

    /// <summary>
    /// Complete the sending of a message and release the currently processing message.
    /// </summary>
    private static void Complete()
    {
        if (DoDebug) Debug.Log($"Completed sending! Sent {_currentlyProcessing.totalSize} total");
        _SPool.Release(_currentlyProcessing);
        _currentlyProcessing = null;
        _hasCurrentlyProcessing = false;
    }

    /// <summary>
    /// If the client is not found in the current connected clients, log a warning and release the packet.
    /// </summary>
    private static void ClientNotFound()
    {
        Debug.LogWarning($"Did not find target client I should be sending to! {_currentlyProcessing.targetClientId}");
        _SPool.Release(_currentlyProcessing);
        _currentlyProcessing = null;
        _hasCurrentlyProcessing = false;
    }

    #endregion

    #region Sending

    /// <summary>
    /// Ships the message from the server to send to the message to the target <see cref="INeonBroadcastReceiver"/>s.
    /// if no target ID, the server broadcasts the segment to all clients.
    /// Default messages are sent to all clients, ID = -1.
    /// </summary>
    /// <param name="newBroadcast">The broadcast to send to the target clients.</param>
    private static void BroadcastMessageToClients(RavenMessageSegment newBroadcast)
    {
        if (_currentlyProcessing.targetClientId != -1) RelayClientMessage(newBroadcast);
        else ServerManager.Broadcast(newBroadcast, true, Channel.Unreliable);
    }

    /// <summary>
    /// If sent from the server, send message to the server's target <see cref="INeonBroadcastReceiver"/>(s). If called
    /// from the client, it will broadcast to the server.
    /// </summary>
    /// <param name="newBroadcast">The broadcast to send to the server.</param>
    private void BroadcastMessageToServer(RavenMessageSegment newBroadcast)
    {
        if (IsServer) InvokeServerBroadcast(newBroadcast);
        else ClientManager.Broadcast(newBroadcast, Channel.Unreliable);
    }

    /// <summary>
    /// Receive the message from the server and ship it to the target client. Will call broadcast to the client if the target
    /// is found. Otherwise, logs warning and releases the packet. (Player not found).
    /// </summary>
    /// <param name="newBroadcast"></param>
    private static void RelayClientMessage(RavenMessageSegment newBroadcast)
    {
        if (ServerManager.Clients.TryGetValue(_currentlyProcessing.targetClientId, out var conn))
            ServerManager.Broadcast(conn, newBroadcast);
        else ClientNotFound();
    }

    #endregion

    #region Receiving

    /// <summary>
    /// Called whenever a message is fully received and reassembled on the server side. Calls the respective client's sender Id
    /// <see cref="INeonBroadcastReceiver.ReceiveLazyLoadedMessage"/> when the <see cref="RavenMessage.senderID"/> is
    /// equal to their own. The default message is sent to all clients. 
    /// </summary>
    private static void ReceiveTargetClientMessage(RavenMessage dataPart) =>
        _SReceivers[dataPart.senderID]
            .ReceiveLazyLoadedMessage(dataPart.FullArray, dataPart.senderID, dataPart.dataIndex);

    /// <summary>
    /// Called whenever a message is fully received and reassembled on the server side. Called on all clients when
    /// the <see cref="RavenMessage.senderID"/> is -1. The default message is sent to all clients.
    /// </summary>
    /// <param name="data">The full message sent from the server.</param>
    /// <param name="senderId">The client Id of whoever sent the current message.</param>
    /// <param name="dataIndex">The data index to gain context to what is being communicated.</param>
    private static void ReceiveCompletedMessage(byte[] data, int senderId, RavenDataIndex dataIndex)
    {
        if (DoDebug) Debug.Log($"Received message {dataIndex} with length {data.Length} from sender {senderId}");

        foreach (var receiver in _SReceivers)
            receiver.Value.ReceiveLazyLoadedMessage(data, senderId, dataIndex);
    }

    /// <summary>
    /// Receives the message from the server relayed from a client. Will reassemble the message and send to associated receivers.
    /// </summary>
    /// <param name="msg">The message to receive and process.</param>
    private void ReceiveRavenSegment(RavenMessageSegment msg)
    {
        var connectionID = msg.senderID;
        if (!_receiverPoolMap.ContainsKey(connectionID))
        {
            _receiverPoolMap[connectionID] = _SPool.Allocate();
            _receiverPoolMap[connectionID].Initialize(msg.totalSize, MAX_SIZE);
        }

        if (!_receiverPoolMap.TryGetValue(connectionID, out var dataPart)) return;

        dataPart.dataIndex = msg.dataIndex;
        dataPart.senderID = msg.senderID;
        dataPart.WriteChunk(msg.data, msg.slowSenderIndex, msg.totalSize);

        if (!msg.complete) return;

        var data = dataPart.FullArray;

        if (dataPart.targetClientId == -1) ReceiveCompletedMessage(data, dataPart.senderID, dataPart.dataIndex);
        else ReceiveTargetClientMessage(dataPart);

        _SPool.Release(dataPart);
        _receiverPoolMap.Remove(connectionID);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Calls the server's local broadcast to receive the message to the server's <see cref="INeonBroadcastReceiver"/>s.
    /// </summary>
    /// <param name="conn">Unused: Broadcast callback requirements.</param>
    /// <param name="msg">Unused: Broadcast callback requirements.</param>
    /// <param name="channel">Unused: Broadcast callback requirements.</param>
    private void ReceiveLocalServerBroadcast(NetworkConnection conn, RavenMessageSegment msg, Channel channel) =>
        ReceiveRavenSegment(msg);

    /// <summary>
    /// Calls the local client(s) to receive the message to the client owned <see cref="INeonBroadcastReceiver"/>s.
    /// </summary>
    /// <param name="msg">Unused: Broadcast callback requirements.</param>
    /// <param name="channel">Unused: Broadcast callback requirements.</param>
    private void ReceiveLocalClientBroadcast(RavenMessageSegment msg, Channel channel) =>
        ReceiveRavenSegment(msg);

    private RavenMessageSegment CreateBroadcastMessage(bool completed) =>
        new(_currentlyProcessing.senderID,
            _currentlyProcessing.dataIndex,
            _currentlyProcessing.currentSlowSenderIndex,
            _currentlyProcessing.FullArray.Length,
            _sendBuffer,
            completed);

    /// <summary>
    /// Helper that Calls local broadcast to send the message to the local <see cref="INeonBroadcastReceiver"/>s.
    /// </summary>
    /// <param name="newBroadcast"></param>
    private void InvokeServerBroadcast(RavenMessageSegment newBroadcast) =>
        ReceiveLocalServerBroadcast(InstanceFinder.IsClientStarted ? ClientManager.Connection : default,
            newBroadcast, Channel.Reliable);

    #endregion

    public static int GetReceiverNumber(INeonBroadcastReceiver receiver)
    {
        foreach (var subbed in _SReceivers)
        {
            if (subbed.Value != receiver) continue;
            return subbed.Key;
        }
        return -1;
    }
}