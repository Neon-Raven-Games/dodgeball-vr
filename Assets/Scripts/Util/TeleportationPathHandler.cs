using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

[Serializable]
public class TeleportPath
{
    [Header("Teleportation")]
    public TeleportationType teleportationType;
    
    [Header("Distance Parameters")]
    public float stepDistance = 5f;
    public float travelDuration = 1f;
    
    public float introPointDistance;
    public float outroStartPointDistance;
    
    [Header("Intro Curve")]
    public float introLength;
    public AnimationCurve introCurve;
    public AnimationClip introAnimation;
    [Header("Outro Curve")]
    public float outroLength;
    public AnimationCurve outroCurve;
    public AnimationClip outroAnimation;
    public readonly Vector3[] pathPoints = new Vector3[4];
}

public enum TeleportationType
{
    ShadowStep,
    Substitution,
    ZigZag
}
public class TeleportationPathHandler : MonoBehaviour
{

    private readonly Dictionary<TeleportationType, TeleportPath> _teleportPathMap = new();
    [SerializeField] List<TeleportPath> teleportPaths;

    private void Awake()
    {
        foreach (var teleportPath in teleportPaths)
            _teleportPathMap[teleportPath.teleportationType] = teleportPath;
    }

    public float FrameToSeconds(int frameNumber, AnimationClip clip)
    {
        return frameNumber / clip.frameRate;
    } 
    
    public async UniTaskVoid Teleport(TeleportationType teleType, Vector3 direction, Action onIntroPointReached, Action onMovedToOutroPoint, Action onFinishTeleport)
    {
        var teleportation = _teleportPathMap[teleType];
        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition + direction * teleportation.stepDistance;

        teleportation.pathPoints[0] = startPosition;
        teleportation.pathPoints[1] = Vector3.Lerp(startPosition, endPosition, teleportation.introPointDistance); 
        teleportation.pathPoints[2] = Vector3.Lerp(startPosition, endPosition, teleportation.outroStartPointDistance);
        teleportation.pathPoints[3] = endPosition;
        
        foreach (var pathPoint in teleportation.pathPoints)
        {
            Debug.DrawRay(pathPoint, Vector3.up, Color.cyan, 2f);
        }
        
        await UniTask.Yield();
        var start = teleportation.pathPoints[0];
        var entryPoint = teleportation.pathPoints[1];
        var duration = _teleportPathMap[teleType].introLength > 0 ? _teleportPathMap[teleType].introLength : _teleportPathMap[teleType].introAnimation.length; // Total duration of the intro animation in seconds
        var entryTime = 0f;

        while (entryTime < 1)
        {
            var t = teleportation.introCurve.Evaluate(entryTime);
            transform.position = Vector3.Lerp(start, entryPoint, t);

            entryTime += Time.deltaTime / duration;
    
            await UniTask.Yield();
        }
        onIntroPointReached?.Invoke();
        
        await UniTask.Delay(TimeSpan.FromSeconds(teleportation.travelDuration));
        
        var exitPoint = teleportation.pathPoints[3];
        var playerPosition = teleportation.pathPoints[2];
        transform.position = playerPosition;
        
        onMovedToOutroPoint?.Invoke();
        await UniTask.Yield();
        
        var targetTransform = transform;
        duration = _teleportPathMap[teleType].outroLength > 0 ? _teleportPathMap[teleType].outroLength : _teleportPathMap[teleType].outroAnimation.length; // Total duration of the intro animation in seconds
        var exitTime = 0f;
        while (exitTime < 1)
        {
            transform.LookAt(targetTransform);
            transform.position = Vector3.Lerp(playerPosition, exitPoint, teleportation.outroCurve.Evaluate(exitTime));
            exitTime += Time.deltaTime / duration;
            await UniTask.Yield();
        }
        
        transform.position = exitPoint;
        onFinishTeleport?.Invoke();
    }
}