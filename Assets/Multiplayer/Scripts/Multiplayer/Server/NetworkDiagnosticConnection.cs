using FishNet.Managing;
using FishNet.Managing.Statistic;
using FishNet.Transporting;
using Newtonsoft.Json;
using UnityEngine;
using WebSocketSharp;

public class NetworkDiagnosticConnection
{
    private readonly WebSocket _webSocket;
    private readonly NetworkManager _networkManager;

    private ClientDiagnostics _clientDiagnostic;

    public NetworkDiagnosticConnection(string webSocketUrl, NetworkManager networkManager)
    {
        _networkManager = networkManager;
        _webSocket = new WebSocket(webSocketUrl);
        _webSocket.OnOpen += OnWebSocketOpen;
        _webSocket.OnError += OnWebSocketError;
        _webSocket.OnClose += OnWebSocketClose;
        _networkManager.ClientManager.OnAuthenticated += Open;
        _networkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
    }

    private void OnClientConnectionState(ClientConnectionStateArgs obj)
    {
        _networkManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;
        // todo, this is not working as expected, we have to call close on the class we create this object on
        // if (obj.ConnectionState == LocalConnectionState.Stopping) Close();
    }

    private void Open()
    {
        _networkManager.ClientManager.OnAuthenticated -= Open;
        var localClient = _networkManager.ClientManager.Connection.ClientId;
        Debug.Log($"[Network Diagnostics] Subscribing Client {localClient} to network traffic diagnostics.");
        _clientDiagnostic = new ClientDiagnostics(localClient);
        _webSocket.Connect();

        _networkManager.TimeManager.OnTick += CollectData;
        _networkManager.StatisticsManager.NetworkTraffic.OnClientNetworkTraffic += OnClientNetworkTraffic;
    }

    public void Close()
    {
        _webSocket.OnOpen -= OnWebSocketOpen;
        _webSocket.OnError -= OnWebSocketError;
        _webSocket.OnClose -= OnWebSocketClose;
        _networkManager.TimeManager.OnTick -= CollectData;
        _networkManager.StatisticsManager.NetworkTraffic.OnClientNetworkTraffic -= OnClientNetworkTraffic;
        _webSocket.Close();
    }

    private void OnClientNetworkTraffic(NetworkTrafficArgs obj)
    {
        _clientDiagnostic.ReceivedBytes = obj.FromServerBytes;
        _clientDiagnostic.SentBytes = obj.ToServerBytes;
        if (_webSocket.ReadyState == WebSocketState.Open) SendData();
    }

    private void CollectData()
    {
        var conn = _networkManager.ClientManager.Connection;
        _clientDiagnostic.Ping = conn.NetworkManager.TimeManager.PingInterval;
        _clientDiagnostic.RoundTripTime = conn.NetworkManager.TimeManager.RoundTripTime;
        _clientDiagnostic.Time = conn.NetworkManager.TimeManager.Tick;
    }

    private void SendData()
    {
        var jsonData = JsonConvert.SerializeObject(_clientDiagnostic);
        _webSocket.Send(jsonData);
    }

    private void OnWebSocketOpen(object sender, System.EventArgs e) =>
        Debug.Log("[Network Diagnostics] WebSocket connection established with server for client: " +
                  _networkManager.ClientManager.Connection.ClientId);

    private void OnWebSocketError(object sender, ErrorEventArgs e) =>
        Debug.LogError($"[Network Diagnostics] WebSocket error: {e.Message}");

    private void OnWebSocketClose(object sender, CloseEventArgs e) =>
        Debug.Log($"[Network Diagnostics] {e.Reason} WebSocket connection closed for client: " +
                  _networkManager.ClientManager.Connection.ClientId);
}

public class ClientDiagnostics
{
    public ClientDiagnostics(int clientId) => ClientId = clientId;

    public int ClientId { get; set; }
    public long Ping { get; set; }
    public long RoundTripTime { get; set; }
    public ulong ReceivedBytes { get; set; }
    public ulong SentBytes { get; set; }
    public ulong Time { get; set; }
}