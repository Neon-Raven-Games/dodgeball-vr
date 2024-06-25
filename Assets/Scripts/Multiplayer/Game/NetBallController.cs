using CloudFine.ThrowLab;
using Fusion;
using Unity.Template.VR.Multiplayer;
using Unity.Template.VR.Multiplayer.Players;
using UnityEngine;


public class NetBallController : NetworkBehaviour
{
    [SerializeField] private DodgeballLab lab;
    [SerializeField] private GameObject ballPrefab;

    [SerializeField] private Transform firstBallSpawn;
    [SerializeField] private Transform secondBallSpawn;
    [SerializeField] private Transform thirdBallSpawn;

    // player picks up a net dodgeball, we need to delete it
    // player throws a dodgeball, we need to spawn a new one and return to delete it
    // we should map the index to an int, 0, 1, 2. we can set the dodgeball live or dead with a balltype
    // if we sync the index of the ball, we can update the ball map to net possession, and a player id.

    // if the ball is picked up, set


    // if ball is thrown, set


    [Networked, Capacity(10)] private NetworkDictionary<NetworkId, NetPlayerData> playerData => default;

    [Networked, Capacity(3)] private NetworkDictionary<int, BallData> ballMap => default;

    private ThrowHandle _ballHandle;

    // if the ball is picked up, we need to despawn the ball
    public static void Despawn(NetworkRunner runner, NetworkObject ball)
    {
        // ballMap[ball.index].ballType = ballType;
        // ballMap[ball.index].playerId = player.id;
        // ballMap[ball.index].netPossession = handside.
        // destroy(ball)
        runner.Despawn(ball);
    }

    public static NetDodgeball SpawnBall(NetworkRunner runner, NetDodgeball ball, NetBallPossession possession)
    {
        var ballData = new BallData
        {
            possession = possession,
            ballType = ball.type,
            team = ball.team
        };
        _instance.ballMap.Set(ball.index, ballData);
        var index = ball.index;
        Destroy(ball);
        
        var thrownBall = runner.Spawn(_instance.ballPrefab, ball.transform.position, Quaternion.identity, null,
            (networkRunner, o) =>
            {
                var db = o.GetComponent<NetDodgeball>();
                db.Initialize(_instance.ballMap[index].ballType, Vector3.zero, index, _instance.ballMap[index].team);
            });

        return thrownBall.GetComponent<NetDodgeball>();
    }

    private static NetBallController _instance;

    public NetBallController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = this;
            }

            return _instance;
        }
        set
        {
            if (_instance == null)
            {
                _instance = value;
            }
        }
    }

    public static NetDodgeball SpawnInitialBall(int number, NetworkRunner networkRunner)
    {
        if (number == 0) return SpawnNewBall(_instance.firstBallSpawn.position, networkRunner);
        if (number == 1) return SpawnNewBall(_instance.secondBallSpawn.position, networkRunner);
        return SpawnNewBall(_instance.thirdBallSpawn.position, networkRunner);
    }

    public static NetDodgeball SpawnNewBall(Vector3 position, NetworkRunner networkRunner)
    {
        var netBehavior = networkRunner.Spawn(_instance.ballPrefab, position, Quaternion.identity);
        var go = netBehavior.gameObject;
        return go.GetComponent<NetDodgeball>();
    }

    public static NetDodgeball SpawnNewBall(Vector3 position)
    {
        var go = Instantiate(_instance.ballPrefab, position, Quaternion.identity);
        var throwHandle = go.GetComponent<ThrowHandle>();
        SetBallConfig(throwHandle);
        return go.GetComponent<NetDodgeball>();
    }

    public static void SetBallConfig(ThrowHandle handle)
    {
        _instance.lab.SetThrowableConfig(handle);
    }

    void Start()
    {
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
    }
}