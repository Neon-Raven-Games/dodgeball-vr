using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class AllocationDebugger : MonoBehaviour
{
    private List<WeakReference> _gcTrackedObjects = new List<WeakReference>();
    private CancellationTokenSource _cancellationTokenSource;

    public int allocatedKilobyteThreshold = 5;
    // Threshold for memory allocation logging (in bytes)
    private int allocationThreshold = 1024; // 1KB

    // Toggle for detailed logging
    public bool detailedLogging = false;

    private void Start()
    {
        _cancellationTokenSource = new CancellationTokenSource();

        // Start monitoring allocations
        // MonitorAllocationsAsync(_cancellationTokenSource.Token).Forget();
        // CleanGCObjectsAsync(_cancellationTokenSource.Token).Forget();
    }

    private void OnDestroy()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }

    private void TrackAllocation(object obj)
    {
        _gcTrackedObjects.Add(new WeakReference(obj));
    }

    private async UniTaskVoid CleanGCObjectsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            long initialMemory = GC.GetTotalMemory(false);


            await UniTask.WaitForSeconds(15, cancellationToken: cancellationToken); // Yield to the next frame
            
            long finalMemory = GC.GetTotalMemory(false);
            long allocatedMemory = finalMemory - initialMemory;

            GC.Collect(0);
   
            Debug.Log($"Cleaning GC, removed gen 0 memory: {allocatedMemory}. Current memory: {finalMemory } KB. Objects tracked: {_gcTrackedObjects.Count}");
        }
    }
    
    private async UniTaskVoid MonitorAllocationsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            allocationThreshold = allocatedKilobyteThreshold * 1024;
            long initialMemory = GC.GetTotalMemory(false);

            await UniTask.Yield(); // Yield to the next frame

            long finalMemory = GC.GetTotalMemory(false);
            long allocatedMemory = finalMemory - initialMemory;

            if (allocatedMemory > allocationThreshold)
            {
                Debug.Log($"[AllocationDebugger] Memory spike detected: {allocatedMemory} bytes allocated in the last frame.");

                // can we pause the game here?
                if (detailedLogging)
                {
                    // Log detailed memory information
                    Debug.Log($"[AllocationDebugger] GC Allocated Memory: {finalMemory / 1024f} KB");
                    Debug.Log($"[AllocationDebugger] Current Time: {Time.time}s");

                    // Inspect active objects if necessary
                    foreach (var weakReference in _gcTrackedObjects)
                    {
                        if (weakReference.IsAlive)
                        {
                            Debug.Log($"[AllocationDebugger] Active Object: {weakReference.Target?.GetType().Name}");
                        }
                    }
                }
            }

            // Optional: Clear old references
            _gcTrackedObjects.RemoveAll(wr => !wr.IsAlive);
        }
    }

    // This method can be used to manually track objects if needed
    public void TrackObject(object obj)
    {
        TrackAllocation(obj);
    }
}
