using FishNet.Object;
using FishNet.Object.Synchronizing;
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
    // todo, make a new serialized object that will hold: ball index and ball spawn position
    [SerializeField] private Vector3 firstBallSpawn;
    [SerializeField] private Vector3 secondBallSpawn;
    [SerializeField] private Vector3 thirdBallSpawn;

    private static NetBallController _instance;
    
    // todo, testing client side
    public static int GetCurrentBallCount() => _instance._ballDataDictionary.Count;
    
    public static void SpawnBallWithIndex(int index)
    {
        var position = index switch
        {
            -1 => _instance.firstBallSpawn,
            1 => _instance.secondBallSpawn,
            2 => _instance.thirdBallSpawn,
            _ => Vector3.zero
        };
        var nob = _instance.NetworkManager.GetPooledInstantiated(_instance.ballPrefab, position, Quaternion.identity, true);
        _instance.NetworkManager.ServerManager.Spawn(nob);
        
        Debug.Log($"Spawning ball! [{index}] position: {position}. Setting ball index to {index}.");
        
        var db = nob.GetComponent<NetDodgeball>();
        db.ballIndex.Value = index;
        db.ballIndex.DirtyAll();
        
        SetBallData(index, -1);

        if (index == -1) _instance._ballOne = nob.gameObject;
        if (index == 1) _instance._ballTwo = nob.gameObject;
        if (index == 2) _instance._ballThree = nob.gameObject;
        db.AddReceiver();
    }

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

    public static void ResetBalls() => _instance.ResetBallsInstanced();

    [ServerRpc(RequireOwnership = false)]
    private void ResetBallsInstanced()
    {
        _ballOne.GetComponent<NetDodgeball>().SetBallPosition(_instance.firstBallSpawn);
        _ballTwo.GetComponent<NetDodgeball>().SetBallPosition(_instance.secondBallSpawn);
        _ballThree.GetComponent<NetDodgeball>().SetBallPosition(_instance.thirdBallSpawn);
    }
    
    public static void ResetBall(int index)
    {
        if (index == -1) _instance._ballOne.transform.position = _instance.firstBallSpawn;
        if (index == 1) _instance._ballTwo.transform.position = _instance.secondBallSpawn;
        if (index == 2) _instance._ballThree.transform.position = _instance.thirdBallSpawn;
    }
    
    public static void SetBalls()
    {
        var foundObjects = FindObjectsByType<NetDodgeball>(FindObjectsSortMode.None);
        foreach (var db in foundObjects)
        {
            if (db.ballIndex.Value == -1)
            {
                _instance._ballOne = db.gameObject;
                db.AddReceiver();
            }
            else if (db.ballIndex.Value == 1)
            {
                _instance._ballTwo = db.gameObject;
                db.AddReceiver();
            }
            else if (db.ballIndex.Value == 2)
            {
                _instance._ballThree = db.gameObject;
                db.AddReceiver();
            }
        }
        if (_instance._ballOne == null || _instance._ballTwo == null || _instance._ballThree == null)
        {
            Debug.LogError("Could not find all balls!");
        }
    }
    private void Awake()
    {
        Debug.Log("Starting netball controller instance");
        _instance = this;
    }
}