using System.Collections;
using Unity.Mathematics;
using UnityEngine;

public class BallRespawnPoint : MonoBehaviour
{
    private int _ballLayer;
    [SerializeField] private GameObject particleEffect;
    [SerializeField] private float spawnInForce = 5f;
    [SerializeField] private float effectDelay = 1f;
    [SerializeField] float effectDespawnDelay = 0.3f;
    [SerializeField] private float delayTime = 2f;
    [SerializeField] private Transform teamOnePlayArea;
    [SerializeField] private Transform teamTwoPlayArea;
    private bool _occupuied;

    public void Awake()
    {
        _ballLayer = LayerMask.NameToLayer("Ball");
    }
    
    public bool IsOccupied() => _occupuied;
    public void SpawnBall(GameObject ball)
    {
        if (_occupuied) return;
        _occupuied = true;
        StartCoroutine(SpawnBallIn(ball));
    }

    private IEnumerator SpawnBallIn(GameObject ball)
    {
        yield return new WaitForSeconds(delayTime);

        if (BallInPlay(ball) && ball.activeInHierarchy)
        {
            _occupuied = false;
            BallBoundsHelper.BallInPlay[ball] = true;
            yield break;
        }
        
        if (!ball.activeInHierarchy) yield break;
        if (!BallInPlay(ball))
        {
            ball.SetActive(false);
            ball.GetComponent<Rigidbody>().velocity = Vector3.zero;

            particleEffect.gameObject.SetActive(true);
            yield return new WaitForSeconds(effectDelay);
            yield return new WaitForSeconds(effectDelay);


            ball.transform.rotation = quaternion.identity;
            ball.transform.position = transform.position;
            var rb = ball.GetComponent<Rigidbody>();
            rb.angularVelocity = Vector3.zero;
            rb.velocity = Vector3.up * spawnInForce;
        
            ball.SetActive(true);
            yield return new WaitForSeconds(effectDespawnDelay);
            particleEffect.gameObject.SetActive(false);
            _occupuied = false;
            BallBoundsHelper.BallInPlay[ball] = true;
        }
    }

    private bool BallInPlay(GameObject ball)
    {
        var playAreaBounds = new Bounds(teamOnePlayArea.position,
            new Vector3(teamOnePlayArea.localScale.x, 5,
                teamOnePlayArea.localScale.z));
        if (playAreaBounds.Contains(ball.transform.position)) return true;
        playAreaBounds = new Bounds(teamTwoPlayArea.position,
            new Vector3(teamTwoPlayArea.localScale.x, 5,
                teamTwoPlayArea.localScale.z));
        
        if (playAreaBounds.Contains(ball.transform.position)) return true;
        
        return false;
    }
}