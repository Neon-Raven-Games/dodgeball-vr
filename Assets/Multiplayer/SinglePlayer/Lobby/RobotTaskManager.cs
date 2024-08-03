using System;
using System.Collections;
using System.Collections.Generic;
using Hands.SinglePlayer.Lobby;
using Hands.SinglePlayer.Lobby.RobotTasks;
using Hands.SinglePlayer.Lobby.RobotTasks.Sequence;
using UnityEngine;


public enum RobotTasks
{
    Informing,
    Leading,
    Idle,
    Interactive
}

public class RobotTaskManager : MonoBehaviour
{
    [SerializeField] private DevController player;
    public AudioSource audioSource;
    public DevController Player => player;
    public Transform waypointParent;
    public List<Transform> waypoints;

    // todo, we can abstract out targets
    public PathTracer robotLeadingTarget;
    public GameObject robotInformingTarget;

    [SerializeField] private SequenceQueue sequenceQueue;
    public RobotTasks currentTask;
    private Dictionary<RobotTasks, Hands.SinglePlayer.Lobby.RobotTask> _tasks = new();
    private InformingBehavior _informingBehavior;
    private LeadingBehavior _leadingBehavior;
    private IdleLivelyBehavior _idleLivelyBehavior;
    private InteractiveBehavior _interactiveBehavior;

    public float speed;

    public Action<RobotTasks> OnTaskComplete;

    private float leadingTargetInitialPosition;
    public bool initialized;

    public void SetInteractiveRotationActive(bool active, TracerTask tracerTask)
    {
        _interactiveBehavior.SetRotationActive(active);
    }
    
    private void Start()
    {
        leadingTargetInitialPosition = Vector3.Distance(robotLeadingTarget.transform.position, transform.position);
        
        _informingBehavior = new InformingBehavior(this);
        _leadingBehavior = new LeadingBehavior(this);
        _idleLivelyBehavior = new IdleLivelyBehavior(this);
        _interactiveBehavior = new InteractiveBehavior(this);
        _tasks.Add(RobotTasks.Informing, _informingBehavior);
        _tasks.Add(RobotTasks.Leading, _leadingBehavior);
        _tasks.Add(RobotTasks.Idle, _idleLivelyBehavior);
        _tasks.Add(RobotTasks.Interactive, _interactiveBehavior);

        currentTask = RobotTasks.Leading;
        _tasks[currentTask].EnterTask(sequenceQueue.defaultSequence);
        OnTaskComplete += SetTask;
        StartCoroutine(EntryExitSequence());
    }

    private IEnumerator EntryExitSequence()
    {
        yield return new WaitForSeconds(5);
        EndCurrentTask();
    }

    private void SetTask(RobotTasks task)
    {
        _tasks[currentTask].ExitTask();
        currentTask = task;
        _tasks[currentTask].EnterTask(sequenceQueue.DequeueSequence());
        if (currentTask == RobotTasks.Informing) initialized = true;
    }

    public void EndCurrentTask()
    {
        robotLeadingTarget.transform.position = transform.position + Vector3.forward * leadingTargetInitialPosition;
        OnTaskComplete?.Invoke(sequenceQueue.NextTask());
    }

    private void Update() => _tasks[currentTask].Update();
    private void FixedUpdate() => _tasks[currentTask].FixedUpdate();
}