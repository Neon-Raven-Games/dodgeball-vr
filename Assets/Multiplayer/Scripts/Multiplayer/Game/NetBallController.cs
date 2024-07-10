using UnityEngine;


public class NetBallController : MonoBehaviour
{
    // todo, we need to use the spawn points on match init
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private Transform firstBallSpawn;
    [SerializeField] private Transform secondBallSpawn;
    [SerializeField] private Transform thirdBallSpawn;
    
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
}