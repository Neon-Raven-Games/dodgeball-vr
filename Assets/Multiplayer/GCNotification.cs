using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GCNotification : MonoBehaviour
{
    private static int[] lastCollectionCounts;
    [SerializeField] private List<DodgeballAI> dodgeballAIs;

    private void Start()
    {
        lastCollectionCounts = new int[GC.MaxGeneration + 1];
    }

    private void Update()
    {
        CheckForGarbageCollection().Forget();
    }

    private async UniTaskVoid CheckForGarbageCollection()
    {
        for (int i = 0; i <= GC.MaxGeneration; i++)
        {
            int currentCollectionCount = GC.CollectionCount(i);
            if (currentCollectionCount != lastCollectionCounts[i])
            {
                Debug.Log($"Garbage Collection occurred for Generation {i}. Count: {currentCollectionCount}");
                lastCollectionCounts[i] = currentCollectionCount;
                OnGCApproach();
            }
        }
        await UniTask.Yield();
    }

    private void OnGCApproach()
    {
        Debug.Log("GC Approach: Analyze what's happening");
        foreach (var ai in dodgeballAIs)
        {
            Debug.Log(ai.currentState);
        }
    }

    private void OnGCComplete()
    {
        Debug.Log("GC Complete: Analyze post-GC state");
        foreach (var ai in dodgeballAIs)
        {
            Debug.Log(ai.currentState);
        }
    }
}