using CloudFine.ThrowLab;
using Fusion;
using Unity.Template.VR.Multiplayer;
using UnityEngine;

public class NetBallController : MonoBehaviour
{
    [SerializeField] private DodgeballLab lab;
    [SerializeField] private GameObject ballPrefab;
    
    [SerializeField] private Transform firstBallSpawn;
    [SerializeField] private Transform secondBallSpawn;
    [SerializeField] private Transform thirdBallSpawn;
    
    private ThrowHandle _ballHandle;
    

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
        var throwHandle = go.GetComponent<ThrowHandle>();
        SetBallConfig(throwHandle);
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
