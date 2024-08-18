using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Hands.SinglePlayer.EnemyAI.Abilities;
using UnityEngine;
using Random = UnityEngine.Random;

public class ShadowCourt : MonoBehaviour
{
    public GameObject smokeEffect;

    public List<ShadowEffectEntity> shadowEffects = new();
    public List<GameObject> ninjaCharacters = new();
    [SerializeField] private Transform playArea;
    [SerializeField] private int spawnCount;
    [SerializeField] private float maxY;
    [SerializeField] private float xOffset;
    public float smokeScreenDuration = 20f;
    [SerializeField] private float delay;
    private static ShadowCourt _instance;
    private static bool active;
    [SerializeField] private float ballLaunchTimeStep;

    public static void SmokeScreen()
    {
        if (active) return;
        GameManager.ChangePhase(BattlePhase.Lackey);
        active = true;
        _instance.smokeEffect.SetActive(true);

        _instance.StartSmokeScreen().Forget();
    }

    private void EndSmokeScreen()
    {
        for (var i = 0; i < ninjaCharacters.Count; i++)
        {
            shadowEffects[i].gameObject.SetActive(false);
        }

        smokeEffect.SetActive(false);
        active = false;
        GameManager.ChangePhase(BattlePhase.Lackey);
    }

    [SerializeField]
    private BallSpawner ballSpawner;

    private async UniTaskVoid StartSmokeScreen()
    {
        var currentTime = 0f;
        var secondsToSpawnSmoke = GenerateRandomTimes();
        var launchBalls = 0f;

        await UniTask.Delay(TimeSpan.FromSeconds(delay));
        var i = 0;
        while (currentTime < smokeScreenDuration)
        {
            await UniTask.Yield();
            currentTime += Time.deltaTime;
            if (launchBalls < currentTime)
            {
                var playAreaPos = playArea.position;
                playAreaPos.y = ballSpawner.planeTransform.position.y;
                ballSpawner.planeTransform.transform.position = new Vector3(playAreaPos.x, playAreaPos.y,
                    playAreaPos.z + Random.Range(-playArea.localScale.z / 3, playArea.localScale.z / 3));
                
                ballSpawner.planeTransform.transform.localRotation = Quaternion.Euler(4, Random.Range(80, 95), 3);
                ballSpawner.SpawnBalls();
                ballLaunchTimeStep = Random.Range(1f, 4.5f);
                launchBalls += ballLaunchTimeStep;
                
                if (launchBalls >= smokeScreenDuration - 4f)
                    launchBalls = smokeScreenDuration + 4f;
                
                i++;
            }
            if (shadowEffects.Any(x => x.gameObject.activeInHierarchy))
            {
                if (secondsToSpawnSmoke.Count == 0 || secondsToSpawnSmoke[0] > currentTime) continue;
            } 
            var shadow = shadowEffects.FirstOrDefault(x => !x.gameObject.activeInHierarchy);
            if (shadow)
            {
                var courtSide = Random.Range(0, 2);
                shadow.transform.position = GenerateRandomValidPoint(courtSide);
                var endPoint = GenerateRandomEndPoint(courtSide == 0 ? 1 : 0, shadow.transform.position.x);
                Vector3[] controlPoints = GenerateBezierControlPoints(shadow.transform.position, endPoint);
                shadow.SetBezierCurve(controlPoints);
                shadow.gameObject.SetActive(true);
            }
            
            if (secondsToSpawnSmoke.Count > 0 && secondsToSpawnSmoke[0] < currentTime) secondsToSpawnSmoke.RemoveAt(0);
        }

        EndSmokeScreen();
    }

    private Vector3 GenerateRandomEndPoint(int courtSide, float xPos)
    {
        var randomZ = 0f;
        if (courtSide == 0)
        {
            randomZ = Random.Range(playArea.position.z - playArea.localScale.z / 2,
                playArea.position.z - playArea.localScale.z / 3);
        }
        else
        {
            randomZ = Random.Range(playArea.position.z + playArea.localScale.z / 2,
                playArea.position.z + playArea.localScale.z / 3);
        }

        return new Vector3(xPos, Random.Range(0f, 1.5f), randomZ);
    }

    private Vector3 GenerateRandomValidPoint(int courtSideIndex)
    {
        var x = playArea.transform.position.x;
        var randomX = Random.Range(x, x + playArea.localScale.x / 2);
        var randomZ = 0f;
        if (courtSideIndex == 0)
        {
            randomZ = Random.Range(playArea.position.z,
                playArea.position.z - playArea.localScale.z * 0.4f);
        }
        else
        {
            randomZ = Random.Range(playArea.position.z ,
                playArea.position.z + playArea.localScale.z * 0.4f);
        }

        return new Vector3(randomX, ninjaCharacters[0].transform.position.y, randomZ);
    }

    private Vector3[] GenerateBezierControlPoints(Vector3 startPoint, Vector3 endPoint)
    {
        var midPoint = (startPoint + endPoint) * Random.Range(0.1f, 0.9f);
        var offset = Random.Range(-xOffset, xOffset);
        var controlPoint = midPoint + new Vector3(0, Random.Range(1, maxY), offset);
        controlPoint.x += offset / 4;
        return new[] {startPoint, controlPoint, endPoint};
    }

    private List<float> GenerateRandomTimes()
    {
        var randomTimes = new List<float>();
        for (var i = 0; i < spawnCount; i++)
        {
            randomTimes.Add(Random.Range(0, smokeScreenDuration));
        }

        return randomTimes.OrderBy(x => x).ToList();
    }

    private void Start()
    {
        if (!_instance)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}