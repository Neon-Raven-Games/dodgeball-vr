using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Unity.Template.VR.Multiplayer;
using UnityEngine;

public class NetPlayerData
{
    public NetBallPossession leftBall;
    public BallType leftBallType;
    
    public NetBallPossession rightBall;
    public BallType rightBallType;
}
public class ServerOwnershipManager : NetworkBehaviour
{
    private readonly SyncDictionary<int, NetPlayerData> _playerBallPossessions = new();
    private static ServerOwnershipManager _instance;

    public static void AddPlayer(int id) =>
        _instance.AddPlayerWithNewData(id);

    [ServerRpc(RequireOwnership = false)]
    private void AddPlayerWithNewData(int id) =>
        _playerBallPossessions[id] = new NetPlayerData();
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public static void RequestOwnershipFromServer(NetworkBehaviour networkObject, NetworkConnection requestingPlayer, HandSide handSide)
    {
        _instance.RequestOwnershipServerRpc(networkObject, requestingPlayer, handSide);
    }
    
    public static void ReleaseOwnershipFromServer(NetworkBehaviour networkObject, Vector3 velocity, Vector3 position,
        HandSide handSide)
    {
        _instance.ReleaseOwnershipServerRpc(networkObject, velocity, position, handSide);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void RequestOwnershipServerRpc(NetworkBehaviour networkObject, NetworkConnection requestingPlayer, HandSide handSide)
    {
        if (!IsOwnershipRequestValid(networkObject, requestingPlayer)) return;
        
        Debug.Log("Requested ownership of ball, setting state to possessed");
        
        var netBall = networkObject.GetComponent<NetDodgeball>();
        netBall.state.Value = BallState.Possessed;
        if (handSide == HandSide.LEFT) _playerBallPossessions[requestingPlayer.ClientId].leftBall = NetBallPossession.LeftHand;
        else _playerBallPossessions[requestingPlayer.ClientId].rightBall = NetBallPossession.RightHand;
            
        // todo, handle power-up balls here
        _playerBallPossessions[requestingPlayer.ClientId].leftBallType = BallType.Dodgeball;
           
        networkObject.GiveOwnership(requestingPlayer);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReleaseOwnershipServerRpc(NetworkBehaviour networkObject, Vector3 velocity, Vector3 position,
        HandSide handSide)
    {
        var netDb = networkObject.GetComponent<NetDodgeball>();
        if (handSide == HandSide.RIGHT)
        {
            _playerBallPossessions[netDb.OwnerId].rightBall = NetBallPossession.None;
            _playerBallPossessions[netDb.OwnerId].rightBallType = BallType.None;
        }
        else
        {
            _playerBallPossessions[netDb.OwnerId].leftBall = NetBallPossession.None;
            _playerBallPossessions[netDb.OwnerId].leftBallType = BallType.None;
        }
        
        netDb.StartCoroutine(netDb.WaitForServerOwner(velocity, position));
        networkObject.RemoveOwnership();
    }

    private bool IsOwnershipRequestValid(NetworkBehaviour networkObject, NetworkConnection requestingPlayer)
    {
        return networkObject.HasAuthority && !networkObject.IsOwner && networkObject.OwnerId != requestingPlayer.ClientId;
    }

    public static NetBallPossession GetBallPossession(int id, HandSide handSide)
    {
        _instance._playerBallPossessions.TryGetValue(id, out var playerData);
        if (playerData == null) return NetBallPossession.None;
        
        if (handSide == HandSide.LEFT) return playerData.leftBall;
        if (handSide == HandSide.RIGHT) return playerData.rightBall;
        return NetBallPossession.None;
    }

    public static BallType GetBallType(int id, HandSide handSide)
    {
        _instance._playerBallPossessions.TryGetValue(id, out var playerData);
        if (playerData == null) return BallType.None;
        
        if (handSide == HandSide.LEFT) return playerData.leftBallType;
        if (handSide == HandSide.RIGHT) return playerData.rightBallType;
        return BallType.None;
    }
}