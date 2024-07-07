using System.Collections.Generic;
using FishNet.Object;
using Unity.Template.VR.Multiplayer;
using UnityEngine;


public class NetBallController : MonoBehaviour
{
    // todo, redo this class to populate the player data
    // the _networkBalls should be fine to delete, but leaving here just to remind myself how it was originally
    
    // idk if we can use syncvars for dictionary
    // we need to update the player data for net avatar possession on the server side
    // private readonly SyncVar<Dictionary<int, NetPlayerData>> _playerData = new();
    
    // the dodgeballs in the scene, we need to populate these on the network manager
    private static readonly Dictionary<int, NetworkObject> _networkBalls = new();

    #region monobehaviour level

    // [SerializeField] private ThrowConfiguration throwConfig;
    
    
    // this is taken over by the multiplayer manager. We should probably drag all this logic to it, or
    // separate dodgeball from multiplayer manager.
    // to keep legacy, probably the latter
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private Transform firstBallSpawn;
    [SerializeField] private Transform secondBallSpawn;
    [SerializeField] private Transform thirdBallSpawn;
    
        
    
    // public static void SetThrowableConfig(ThrowHandle throwablePrefab)
    // {
    //     throwablePrefab.SetConfigSet(new ThrowConfigurationSet(1));
    //     throwablePrefab.SetConfigForDevice(Device.UNSPECIFIED, _instance.throwConfig);
    // }
    
    // we need to set the dodgeball lab
    private static NetBallController _instance;

    private void Start()
    {
        Debug.Log("Starting netball controller instance");
        Instance = this;
    }

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

    // public static NetBallPossession GetBallPossession(int id, HandSide hand)
    // {
    //     var success = _instance._playerData.Value.TryGetValue(id, out var playerData);
    //     if (!success) Debug.Log("Failed to get player data");
    //
    //
    //     // LogPlayerData(id);
    //     if (!success) return NetBallPossession.None;
    //
    //     if (hand == HandSide.LEFT)
    //         return playerData.leftBall == BallType.None ? NetBallPossession.None : NetBallPossession.LeftHand;
    //     if (hand == HandSide.RIGHT)
    //         return playerData.rightBall == BallType.None ? NetBallPossession.None : NetBallPossession.RightHand;
    //     return NetBallPossession.None;
    // }

    // internal static void UpdatePlayerPossessionData(int id, NetBallPossession possession, BallType type)
    // {
    //     var playerData = _instance._playerData.Value[id];
    //     if (possession == NetBallPossession.None)
    //     {
    //         playerData.leftBall = BallType.None;
    //         playerData.rightBall = BallType.None;
    //     }
    //     else
    //     {
    //         playerData.leftBall = possession == NetBallPossession.LeftHand ? type : playerData.leftBall;
    //         playerData.rightBall = possession == NetBallPossession.RightHand ? type : playerData.rightBall;
    //     }
    //
    //     _instance._playerData.Value[id] = playerData;
    // }

    #endregion


    public static NetDodgeball GetBall(int grabDataBallIndex)
    {
        return _networkBalls[grabDataBallIndex].GetComponent<NetDodgeball>();
    }
}