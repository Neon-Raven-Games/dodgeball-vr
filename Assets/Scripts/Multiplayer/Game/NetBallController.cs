using CloudFine.ThrowLab;
using Fusion;
using Unity.Template.VR.Multiplayer;
using UnityEngine;

// public struct NetPlayerData : INetworkStruct
// {
//     public BallType leftBall;
//     public BallType rightBall;
//     public Team team;
// }
public class NetBallController : NetworkBehaviour
{
    [SerializeField] private DodgeballLab lab;
    [SerializeField] private GameObject ballPrefab;
    
    [SerializeField] private Transform firstBallSpawn;
    [SerializeField] private Transform secondBallSpawn;
    [SerializeField] private Transform thirdBallSpawn;
    
    // [Networked, Capacity(10)] 
    // private NetworkDictionary<NetworkId, NetPlayerData> playerData { get; set; }
    
    
    private ThrowHandle _ballHandle;

    // if the ball is picked up, we need to despawn the ball
    public static void Despawn(NetworkRunner runner, NetworkObject ball)
    {
        runner.Despawn(ball);
    }

    public static NetDodgeball SpawnBall(NetworkRunner runner, NetworkObject ball)
    {
        // spawn the ball
        // we can handle initialization here since we are state authority
        // and the dodgeball holds networked values
        return null;
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
