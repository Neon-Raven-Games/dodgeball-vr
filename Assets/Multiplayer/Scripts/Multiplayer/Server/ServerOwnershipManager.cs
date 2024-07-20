using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Multiplayer.Scripts.Multiplayer.SyncComponents;
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

    // keep collection of balls in dictionary,
    // create an index for the ball numbers and populate the dictionary for a lookup map
    // when we throw the ball, we take the team from the player and set the ball to the team
    // whenever the ball is deemed dead, we set the ball team to none.
    // We have invocation for ball thrown and the ownership lifecycle.
    // We need invocation for RegisteringHit, RegisteringCatch, RegisteringDead

    // we need to out the player who does get hit, so we do need to keep reference to the id who is out
    public void RegisterHit(int ballIndex, int hitPlayerId)
    {
        // score the point
        // out the hit player -> hitPlayerId
        NetBallController.SetBallData(ballIndex, -1);
    }
    
    // this needs to out the throwing player, so we do need to keep reference to the id who threw the ball
    public void RegisterCatch(int ballIndex, int catcherId)
    {
        // score the point
        // out the current owner
        NetBallController.SetBallData(ballIndex, catcherId);
    }
    
    // no client events, just set team to none, ball to dead
    public void RegisterDeadBall(int ballIndex)
    {
        NetBallController.SetBallData(ballIndex, -1);
    }
    
    
    
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
        HandSide handSide, uint tick)
    {
        _instance.ReleaseOwnershipServerRpc(networkObject, velocity, position, handSide, tick);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void RequestOwnershipServerRpc(NetworkBehaviour networkObject, NetworkConnection requestingPlayer, HandSide handSide)
    {
        if (!IsOwnershipRequestValid(networkObject, requestingPlayer)) return;
        
        var netBall = networkObject.GetComponent<NetDodgeball>();
        netBall.state.Value = BallState.Possessed;
        if (handSide == HandSide.LEFT) _playerBallPossessions[requestingPlayer.ClientId].leftBall = NetBallPossession.LeftHand;
        else _playerBallPossessions[requestingPlayer.ClientId].rightBall = NetBallPossession.RightHand;
        _playerBallPossessions[requestingPlayer.ClientId].leftBallType = BallType.Dodgeball;
        
        _playerBallPossessions.Dirty(requestingPlayer.ClientId);
        
        var sync = networkObject.GetComponent<BroadcastSyncComponent>();
        sync.GiveOwnership(requestingPlayer);
        NetBallController.SetBallData(sync.index.Value, requestingPlayer.ClientId);
    }

    [ServerRpc(RequireOwnership = false, OrderType = DataOrderType.Last)]
    public void ReleaseOwnershipServerRpc(NetworkBehaviour networkObject, Vector3 velocity, Vector3 position,
        HandSide handSide, uint tick)
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
        
        _playerBallPossessions.Dirty(netDb.OwnerId);
        
        var sync = networkObject.GetComponent<BroadcastSyncComponent>();
        sync.ThrowBallOnTick(tick, position, velocity);
        netDb.WaitForServerOwner();
        sync.RemoveOwnership();
        
        NetBallController.SetBallData(sync.index.Value, -1);
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