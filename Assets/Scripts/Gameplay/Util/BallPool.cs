using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BallPool : MonoBehaviour
{
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private int poolSize = 8;
    
    private List<DodgeBall> _pool = new();
    private int _currentIndex;
   private static BallPool _instance;
    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        Initialize();
    }
    public static void Initialize()
    {
        // _instance._pool.ForEach(Destroy);
        // _instance._pool.Clear();
        for (var i = 0; i < _instance.poolSize; i++)
        {
            var ball = Instantiate(_instance.ballPrefab, _instance.transform);
            ball.SetActive(false);
            _instance._pool.Add(ball.GetComponent<DodgeBall>());
        }
    }

    public static GameObject SetBall(Vector3 position)
    {
        var ball = _instance._pool[_instance._currentIndex];
        if (ball.gameObject.activeInHierarchy)
        {
            ball = _instance._pool.FirstOrDefault(x => !x.gameObject.activeInHierarchy);
            if (!ball) 
            {
                _instance._currentIndex = 0;
                ball = Instantiate(_instance.ballPrefab, _instance.transform).GetComponent<DodgeBall>();
                ball._ballState = BallState.Dead;
                ball.gameObject.SetActive(false);
                _instance._pool.Add(ball.GetComponent<DodgeBall>()); 
            }
        }
        
        ball._ballState = BallState.Dead;
        ball.GetComponent<Rigidbody>().velocity = Vector3.zero;
        ball.transform.position = position;
        _instance._currentIndex = (_instance._currentIndex + 1) % _instance.poolSize;
        return ball.gameObject;
    }

    public static void Sleep()
    {
        _instance._pool.ForEach(ball => ball.gameObject.SetActive(false));
    }

    public static DodgeBall GetBall(Vector3 position)
    {
        var ball = _instance._pool[_instance._currentIndex];
        if (ball.gameObject.activeInHierarchy)
        {
            ball = _instance._pool.FirstOrDefault(x => !x.gameObject.activeInHierarchy);
            if (!ball) 
            {
                _instance._currentIndex = 0;
                ball = Instantiate(_instance.ballPrefab, _instance.transform).GetComponent<DodgeBall>();
                ball._ballState = BallState.Dead;
                ball.gameObject.SetActive(false);
                _instance._pool.Add(ball.GetComponent<DodgeBall>()); 
            }
        }
        
        ball.GetComponent<Rigidbody>().velocity = Vector3.zero;
        ball.transform.position = position;
        _instance._currentIndex = (_instance._currentIndex + 1) % _instance.poolSize;
        return ball;
    }
}
