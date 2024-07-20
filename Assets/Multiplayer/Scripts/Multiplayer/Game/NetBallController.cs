using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Multiplayer.Scripts.Multiplayer.SyncComponents;
using Unity.Template.VR.Multiplayer;
using UnityEngine;


public struct BallData
{
    public int ballIndex;

    public int ownerConnectionId;
    // public BallType type;
}

// these may be spawned after the player, if so, we need to update the player another way.
public class NetBallController : NetworkBehaviour
{
    private readonly SyncDictionary<int, BallData> _ballDataDictionary = new();

    private GameObject _ballOne;
    private GameObject _ballTwo;
    private GameObject _ballThree;
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(firstBallSpawn, 0.5f);
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(secondBallSpawn, 0.5f);
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(thirdBallSpawn, 0.5f);
    }
#endif

    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private Vector3 firstBallSpawn;
    [SerializeField] private Vector3 secondBallSpawn;
    [SerializeField] private Vector3 thirdBallSpawn;

    private static NetBallController _instance;

    // todo, testing client side
    public static int GetCurrentBallCount() => _instance._ballDataDictionary.Count;

    public static void SetBallData(int index, int ownerId)
    {
        var ballData = new BallData
        {
            ballIndex = index,
            ownerConnectionId = ownerId
        };

        Debug.Log($"setting ball data from server, [{index}] {ownerId}.");
        _instance._ballDataDictionary[index] = ballData;
        _instance._ballDataDictionary.Dirty(index);
    }

    public static void ResetBalls()
    {
        
        _instance.ResetBallsInstanced();
        _instance.SubscribeNewReceiver();
       
    }
    
    private void SubscribeNewReceiver()
    {
        // todo, method sub and invocation instead of direct call from client player
        Debug.Log("this shit don't work.");
        var collection = FindFirstObjectByType<BroadcastCollection>();
        collection.SubscribeNewReceiver();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ResetBallsInstanced()
    {
        var collection = FindFirstObjectByType<BroadcastCollection>();
        for (var i = -1; i > -4; i--)
        {
            var ball = collection.GetComponentByIndex(i);
            ball.RemoveReceiver();
            var netBall = ball.GetComponent<NetworkObject>();
            InstanceFinder.NetworkManager.ServerManager.Despawn(netBall);
        }

        collection.InitializeServerObjects();
        Debug.Log("Reset all the balls mane");
    }

    public static void ResetBall(int index)
    {
        if (index == -1) _instance._ballOne.transform.position = _instance.firstBallSpawn;
        if (index == 1) _instance._ballTwo.transform.position = _instance.secondBallSpawn;
        if (index == 2) _instance._ballThree.transform.position = _instance.thirdBallSpawn;
    }

    private void Awake()
    {
        Debug.Log("Starting netball controller instance");
        _instance = this;
    }
}