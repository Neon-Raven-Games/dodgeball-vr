using System.Collections.Generic;
using CloudFine.ThrowLab;
using Fusion;
using Unity.Template.VR.Multiplayer;
using Unity.Template.VR.Multiplayer.Players;
using UnityEngine;


public class NetBallController : NetworkBehaviour
{
    [Networked, Capacity(10)] private NetworkDictionary<NetworkId, NetPlayerData> playerData => default;
    [Networked, Capacity(3)] private NetworkDictionary<int, BallData> ballMap => default;

    #region monobehaviour level

    [SerializeField] private DodgeballLab lab;
    [SerializeField] private GameObject ballPrefab;

    [SerializeField] private Transform firstBallSpawn;
    [SerializeField] private Transform secondBallSpawn;
    [SerializeField] private Transform thirdBallSpawn;
    
    private static readonly Dictionary<int, NetworkObject> _networkBalls = new();

    private static NetBallController _instance;

    private void Start() => Instance = this;

    private static NetBallController Instance
    {
        set
        {
            if (_instance == null)
            {
                _instance = value;
            }
        }
    }

    #endregion

    #region rpc methods

    public static void SetDeadBall(int ballIndex)
    {
        _instance.ballMap.TryGet(ballIndex, out var ballData);
        ballData.team = Team.None;
        _instance.ballMap.Set(ballIndex, ballData);
    }

    public static NetBallPossession GetBallPossession(NetworkId id, HandSide hand)
    {
        var success = _instance.playerData.TryGet(id, out var playerData);
        if (!success) Debug.Log("Failed to get player data");


        // LogPlayerData(id);
        if (!success) return NetBallPossession.None;

        if (hand == HandSide.LEFT)
            return playerData.leftBall == BallType.None ? NetBallPossession.None : NetBallPossession.LeftHand;
        if (hand == HandSide.RIGHT)
            return playerData.rightBall == BallType.None ? NetBallPossession.None : NetBallPossession.RightHand;
        return NetBallPossession.None;
    }

    public static void PossessBall(NetworkRunner runner, NetworkId id, NetBallPossession possession, int ballIndex)
    {
        var ball = _instance.ballMap.Get(ballIndex);
        UpdatePlayerPossessionData(id, possession, ball.ballType);
        ball.owner = id;
        ball.team = _instance.playerData.Get(id).team;
        _instance.ballMap.Set(ballIndex, ball);

        // LogBallData(ballIndex);

        var destroyedBall = _networkBalls[ballIndex];
        runner.Despawn(destroyedBall);
    }

    public static void ThrowBall(NetworkRunner runner, NetBallPossession possession, Vector3 position, Vector3 velocity,
        NetworkId id, int index)
    {
        UpdatePlayerThrowData(id, possession);
        _instance.ballMap.TryGet(index, out var ballData);
        ballData.team = _instance.playerData.Get(id).team;
        _instance.ballMap.Set(index, ballData);

        var thrownBall = runner.Spawn(_instance.ballPrefab, position, Quaternion.identity, null,
            (networkRunner, o) =>
            {
                var db = o.GetComponent<NetDodgeball>();
                db.Initialize(_instance.ballMap[index].ballType, velocity, index, _instance.ballMap[index].team);
            });
        // LogBallData(index);
        _networkBalls[index] = thrownBall;
    }

    private static void UpdatePlayerThrowData(NetworkId id, NetBallPossession possession)
    {
        var playerData = _instance.playerData[id];
        if (possession == NetBallPossession.None)
        {
            playerData.leftBall = BallType.None;
            playerData.rightBall = BallType.None;
        }
        else
        {
            playerData.leftBall = possession == NetBallPossession.LeftHand ? BallType.None : playerData.leftBall;
            playerData.rightBall = possession == NetBallPossession.RightHand ? BallType.None : playerData.rightBall;
        }

        _instance.playerData.Set(id, playerData);
        // LogPlayerData(id);
    }

    private static void UpdatePlayerPossessionData(NetworkId id, NetBallPossession possession, BallType type)
    {
        var playerData = _instance.playerData[id];
        if (possession == NetBallPossession.None)
        {
            playerData.leftBall = BallType.None;
            playerData.rightBall = BallType.None;
        }
        else
        {
            playerData.leftBall = possession == NetBallPossession.LeftHand ? type : playerData.leftBall;
            playerData.rightBall = possession == NetBallPossession.RightHand ? type : playerData.rightBall;
        }

        _instance.playerData.Set(id, playerData);
        // LogPlayerData(id);
    }

    private static NetDodgeball SpawnNetBall(Vector3 position, NetworkRunner networkRunner)
    {
        var netBehavior = networkRunner.Spawn(_instance.ballPrefab, position, Quaternion.identity);
        var go = netBehavior.gameObject;
        return go.GetComponent<NetDodgeball>();
    }

    #endregion

    #region initialization

    public static NetDodgeball SpawnInitialBall(int number, NetworkRunner networkRunner)
    {
        // spawn ball
        NetDodgeball dodgeball;

        if (number == 0) dodgeball = SpawnNetBall(_instance.firstBallSpawn.position, networkRunner);
        else if (number == 1) dodgeball = SpawnNetBall(_instance.secondBallSpawn.position, networkRunner);
        else dodgeball = SpawnNetBall(_instance.thirdBallSpawn.position, networkRunner);

        // update maps
        _instance.ballMap.Add(number, new BallData {ballType = BallType.Dodgeball});
        _networkBalls[number] = dodgeball.GetComponent<NetworkObject>();

        return dodgeball;
    }

    public static NetDodgeball SpawnLocalBall(Vector3 position, int ballIndex)
    {
        var go = Instantiate(_instance.ballPrefab, position, Quaternion.identity);

        // local ball
        var throwHandle = go.GetComponent<ThrowHandle>();
        SetBallConfig(throwHandle);

        var dodgeBall = go.GetComponent<NetDodgeball>();
        dodgeBall.index = ballIndex;
        return dodgeBall;
    }

    public static void SetBallConfig(ThrowHandle handle) =>
        _instance.lab.SetThrowableConfig(handle);

    public static void AddPlayerData(NetworkId objectId, Team teamOne) =>
        _instance.playerData.Add(objectId, new NetPlayerData {team = teamOne});

    #endregion

    public static BallType GetBallType(NetworkId id)
    {
        _instance.playerData.TryGet(id, out var playerData);
        return playerData.leftBall == BallType.None ? playerData.rightBall : playerData.leftBall;
    }

    public static Team GetBallTeam(int index) =>
        _instance.ballMap.Get(index).team;

    public static Team GetPlayerTeam(NetworkId id) =>
        _instance.playerData.Get(id).team;

    #region logging

    private static void LogPlayerData(NetworkId id)
    {
        Debug.Log($"===Player Data===\n{id}\n player left possession: {_instance.playerData[id].leftBall}\n" +
                  $"player right possession: {_instance.playerData[id].rightBall}\n" +
                  $"Player Team {_instance.playerData[id].team}");
    }

    private static void LogBallData(int index)
    {
        Debug.Log($"===BallData===\n[Ball Index {index}]\nBallType : {_instance.ballMap[index].ballType}\n" +
                  $"Ball possession: {_instance.ballMap[index].ballType}\n" +
                  $"Ball Team {_instance.ballMap[index].team}");
    }

    #endregion
}