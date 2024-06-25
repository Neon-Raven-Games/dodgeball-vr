using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FMOD;
using FMODUnity;
using TMPro;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class MultiplayerManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public NetworkRunner networkRunner;
    public GameObject networkPlayerPrefab;
    [SerializeField] private TextMeshPro _statusText;
    private readonly Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new();

    public void StartLocalGame()
    {
        if (_statusText != null)
        {
            if (networkRunner == null) _statusText.text += "\nNetworkRunner is null when starting local game!";
            else _statusText.text += "\nStarting game";
        }

        InitializeNetworkRunner(networkRunner, GameMode.AutoHostOrClient);
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        if (networkRunner != null) return;
        InitializeNetworkRunner();
    }

    #region initialization

    private static StartGameArgs CreateRoom(Component runner, GameMode mode, string sessionName)
    {
        var sceneManager = runner.GetComponent<NetworkSceneManagerDefault>();
        if (sceneManager == null) sceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>();

        var startGameArgs = new StartGameArgs()
        {
            GameMode = mode,
            SessionName = sessionName,
            Scene = SceneRef.FromIndex(1),
            SceneManager = sceneManager
        };
        return startGameArgs;
    }

    private void HandlePlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        var spawnPosition = Utils.GetRandomSpawnPosition();
        var spawnRotation = Quaternion.identity;
        
        var networkPlayer = runner.Spawn(networkPlayerPrefab, spawnPosition, spawnRotation, player);
        runner.SetPlayerObject(player, networkPlayer);
        // GetComponent<VoiceClient>().
        
        if (networkPlayer != null) _spawnedPlayers[player] = networkPlayer;
    }

    private void HandlePlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        runner.Despawn(_spawnedPlayers[player]);
        _spawnedPlayers.Remove(player);
    }

    private void InitializeNetworkRunner()
    {
        networkRunner = gameObject.GetComponent<NetworkRunner>();
        networkRunner.ProvideInput = true;
        networkRunner.AddCallbacks(this);
        if (_statusText != null) _statusText.text = "NetworkRunner initialized";
    }

    private async void InitializeNetworkRunner(NetworkRunner runner, GameMode mode)
    {
        if (runner == null)
        {
            Debug.LogError("NetworkRunner is null!");
            return;
        }

        var startGameArgs = CreateRoom(runner, mode, "RandomRoom");
        var result = await runner.StartGame(startGameArgs);

        LogRoomConnectionStatus(result, startGameArgs);
    }

    #endregion

    // logging
    private void LogRoomConnectionStatus(StartGameResult result, StartGameArgs startGameArgs)
    {
        if (result.Ok)
        {
            Debug.Log("Connected to room: " + startGameArgs.SessionName);
        }
        else
        {
            if (_statusText != null) _statusText.text += $"\nFailed to connect to room: {result.ShutdownReason} - {result.ErrorMessage}";
            Debug.LogError($"Failed to connect to room: {result.ShutdownReason} - {result.ErrorMessage}");
        }
    }

    #region callbacks

    public void OnConnectedToServer(NetworkRunner runner)
    {
        if (runner.IsClient)
        {
            Debug.Log("Connected to Fusion server.");
            if (_statusText != null) _statusText.text += "\nConnected to Fusion server.";
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return;
        
        HandlePlayerJoined(runner, player);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return;
        if (!_spawnedPlayers.ContainsKey(player)) return;
        HandlePlayerLeft(runner, player);
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
    }


    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request,
        byte[] token)
    {
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key,
        ArraySegment<byte> data)
    {
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
    }

    #endregion
}


public static class Utils
{
    public static Vector3 GetRandomSpawnPosition()
    {
        // This is where we will implement our spawn logic
        // the team will provide default rotation for characters, based on their team.
        // we want to create a formation along the line of the team's side of the court
        return new Vector3(UnityEngine.Random.Range(-5f, 5f), 1f, UnityEngine.Random.Range(-5f, 5f));
    }
}