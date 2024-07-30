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
    private RobotTasks currentTask;
    private Dictionary<RobotTasks, Hands.SinglePlayer.Lobby.RobotTask> _tasks = new();
    private InformingBehavior _informingBehavior;
    private LeadingBehavior _leadingBehavior;
    private IdleLivelyBehavior _idleLivelyBehavior;
    private InteractiveBehavior _interactiveBehavior;

    // fastmode = speed 3, tracer is 3.6
// wander = speed 1, tracer is 1.2
    public float speed;

    public Action<RobotTasks> OnTaskComplete;

    private void Start()
    {
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
    }

    public void EndCurrentTask()
    {
        OnTaskComplete?.Invoke(sequenceQueue.NextTask());
    }

    private void Update() => _tasks[currentTask].Update();
    private void FixedUpdate() => _tasks[currentTask].FixedUpdate();
}