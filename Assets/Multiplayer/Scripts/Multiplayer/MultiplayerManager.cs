using FishNet.Managing;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;

public class MultiplayerManager : MonoBehaviour
{
    private NetworkManager _networkManager;
    [SerializeField] private NetworkObject serverOwnershipManager;
    [SerializeField] private NetworkObject dodgeballPrefab;
    [SerializeField] private Transform dodgeballPosition;
    public void StartServerGameAsHost()
    {
        _networkManager.ServerManager.StartConnection();
        _networkManager.ServerManager.OnServerConnectionState += ConnectOnServerStart;
    }

    private void ConnectOnServerStart(ServerConnectionStateArgs args)
    {
        Debug.Log(args.ConnectionState);
        if (args.ConnectionState != LocalConnectionState.Started) return;
        
        // _networkManager.ClientManager.StartConnection();
        _networkManager.ServerManager.OnServerConnectionState -= ConnectOnServerStart;
        
        // can the server not spawn our dodgeball? We are getting an object reference error?
        NetworkObject nob = _networkManager.GetPooledInstantiated(dodgeballPrefab, dodgeballPosition.position,Quaternion.identity, true);
        _networkManager.ServerManager.Spawn(nob);
        NetworkObject ownership = _networkManager.GetPooledInstantiated(serverOwnershipManager, Vector3.zero, Quaternion.identity, true);
        _networkManager.ServerManager.Spawn(ownership);
    }

    public void StartClientGame() =>
        _networkManager.ClientManager.StartConnection();

    private void Start() =>
        _networkManager = GetComponent<NetworkManager>();
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