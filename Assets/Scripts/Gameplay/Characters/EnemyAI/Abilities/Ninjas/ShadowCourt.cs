using System;
using System.Collections;
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
    
    public float smokeScreenDuration = 20f;
    private static ShadowCourt _instance;
    
    
    public static void SmokeScreen()
    {
        _instance.smokeEffect.SetActive(true);
        foreach (var ninja in _instance.ninjaCharacters)
            ninja.SetActive(false);

        _instance.StartSmokeScreen().Forget();
    }
    
    private void EndSmokeScreen()
    {
        for (var i = 0; i < ninjaCharacters.Count; i++)
        {
            shadowEffects[i].gameObject.SetActive(false);
            ninjaCharacters[i].SetActive(true);
        }
        smokeEffect.SetActive(false);
    }
    
    private async UniTaskVoid StartSmokeScreen()
    {
        var currentTime = 0f;
        var secondsToSpawnSmoke = GenerateRandomTimes();
        // why does this not work?

        Debug.Log(secondsToSpawnSmoke.Count);
        while (currentTime < smokeScreenDuration)
        {
            currentTime += Time.deltaTime;
            if (secondsToSpawnSmoke.Contains((int) currentTime))
            {
                var shadow = shadowEffects.FirstOrDefault(x => !x.gameObject.activeInHierarchy);

                if (shadow)
                {
                    var courtSide = Random.Range(0, 2);
                    shadow.transform.position = GenerateRandomValidPoint(courtSide, ninjaCharacters[0], out var distance);
                    shadow.SetDirectionAndDistance(courtSide, distance);
                    shadow.gameObject.SetActive(true);
                }
                secondsToSpawnSmoke.Remove((int) currentTime);
            }
            await UniTask.Yield();
        }
        EndSmokeScreen();
    }

    private Vector3 GenerateRandomValidPoint(int courtSideIndex, GameObject ninja, out float distance)
    {
        var x = playArea.transform.position.x;
        var randomX = Random.Range(x, x + playArea.localScale.x);
        var randomZ = 0f;
        if (courtSideIndex == 0)
        {
            randomZ = Random.Range(playArea.position.z - playArea.localScale.z / 2, playArea.position.z- playArea.localScale.z / 3);
        }
        else
        {
            randomZ = Random.Range(playArea.position.z + playArea.localScale.z / 2, playArea.position.z + playArea.localScale.z / 3);
        }
        distance = Random.Range(playArea.localScale.z / 2, playArea.localScale.z * 0.8f);
        return new Vector3(randomX, ninja.transform.position.y, randomZ);
    }
    
    private List<int> GenerateRandomTimes()
    {
        var randomTimes = new List<int>();
        for (var i = 0; i < spawnCount; i++)
        {
            // can we make sure that only 3 are active at a time?
            randomTimes.Add(Random.Range(0, (int) smokeScreenDuration));
        }
        return randomTimes;
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
