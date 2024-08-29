using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Gameplay.Characters.EnemyAI.Utilities.UtilityRefactor;
using Gameplay.Characters.EnemyAI.Utilities.UtilityRefactor.UtilityCalculators;
using Gameplay.InGameEvents;
using Gameplay.Util;
using Hands.SinglePlayer.EnemyAI;
using Hands.SinglePlayer.EnemyAI.Priority;
using Hands.SinglePlayer.EnemyAI.StatefulRefactor;
using Hands.SinglePlayer.EnemyAI.Utilities;
using Multiplayer.SinglePlayer.EnemyAI.Utilities;
using Newtonsoft.Json;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class ActorTeam
{
    public List<Actor> actors;
    public Color color;
    public Transform playArea;
    public string layerName;
    public Transform outOfBounds { get; set; }
}

public class SceneActors
{
    public Actor boss;
    public List<Actor> lackeys;
}

public enum AIState
{
    Idle,
    Dodge,
    Catch,
    PickUp,
    Throw,
    Move,
    OutOfPlay,
    BackOff,
    Possession,
    Special
}

public class DodgeballAI : Actor
{
    // targeting
    private DodgeballTargetModule targetModule;
    internal GameObject CurrentTarget => targetModule.CurrentTarget;
    internal DodgeBall BallTarget => targetModule.BallTarget;
    internal Actor ActorTarget => targetModule.ActorTarget;

    public bool stayIdle;
    public string currentState;
    public float difficultyFactor = 1.0f;
    public GameObject aiAvatar => ik.gameObject;
    public bool hasSpecials;
    public BipedIK ik;

    // needs to be fixed
    [SerializeField] private PriorityHandler priorityHandler;
    [SerializeField] protected internal NetBallPossessionHandler leftBallIndex;
    [SerializeField] internal NetBallPossessionHandler rightBallIndex;
    [SerializeField] internal Animator animator;
    [SerializeField] private float ballThrowRecovery = 0.5f;

    public DodgeUtilityArgs dodgeUtilityArgs;
    public MoveUtilityArgs moveUtilityArgs;
    public CatchUtilityArgs catchUtilityArgs;
    public PickUpUtilityArgs pickUpUtilityArgs;
    public ThrowUtilityArgs throwUtilityArgs;

    [FormerlySerializedAs("outOfBoundsUtilityArgs")]
    public OutOfPlayUtilityArgs outOfPlayUtilityArgs;

    internal PickUpUtility pickUpUtility;
    internal readonly Dictionary<int, Vector3> liveBallTrajectories = new();

    protected internal MoveUtility moveUtility;
    protected UtilityHandler utilityHandler;
    private Action _stateBallThrown;
    
    internal float ballPossessionTime;
    
    protected virtual void PlayPhaseOverOutro() => gameObject.SetActive(false);

    public void AddCallbackForThrowBall(Action ballThrown) => _stateBallThrown = ballThrown;
    public void RemoveCallbacksForThrowBall() => _stateBallThrown = null;
    public void ResetLookWeight() => targetModule.ResetLookWeight();
    internal bool IsTargetingBall(GameObject ball) => CurrentTarget == ball;
    internal DodgeBall _possessedBall;

    private static readonly int _SThrowVariation = Animator.StringToHash("ThrowVariation");
    internal static readonly int _SXAxis = Animator.StringToHash("xAxis");
    internal static readonly int _SYAxis = Animator.StringToHash("yAxis");
    private static readonly int _SCancelThrow = Animator.StringToHash("CancelThrow");
    private static readonly int _SPlayGhost = Animator.StringToHash("PlayGhost");
    private static readonly int _SHitVariation = Animator.StringToHash("HitVariation");

    public List<Transform> spawnInPos;
    protected AIStateController stateController;
    
    #region initialization
    public Vector3 GetBallPosition()
    {
        Vector3 ballPos = Vector3.zero;

        if (leftBallIndex._currentDodgeball)
        {
            ballPos = leftBallIndex.BallPosition; 
        }
        else if (rightBallIndex._currentDodgeball)
        {
            ballPos = rightBallIndex.BallPosition; // Get the position of the right hand
        }
        else
        {
            throw new ArgumentOutOfRangeException();
        }

        return ballPos;
    }
    public int state => stateController.State;

    private void Start()
    {
        PopulateTeamObjects();
        targetModule = new DodgeballTargetModule(this, priorityHandler.targetUtility);
        PopulateUtilities();
    }

    
    protected virtual void CreateStateController()
    {
        stateController = AIStateMachineFactory.CreateStateMachine(this,
            moveUtilityArgs, pickUpUtilityArgs, throwUtilityArgs, outOfPlayUtilityArgs);
        stateController.ChangeState(StateStruct.Move);
    }

    protected override void PopulateUtilities()
    {
        CreateStateController();
        stateController.ChangeState(StateStruct.Move);

        var utilityCalculators = new List<IUtilityCalculator>();
        var pickup = new PickUpUtilityCalculator();
        pickup.PriorityData = pickUpUtilityArgs.priorityData;
        pickup.PriorityData.Initialize();
        utilityCalculators.Add(pickup);
        
        var thrw = new ThrowUtilityCalculator();
        thrw.PriorityData = throwUtilityArgs.priorityData;
        thrw.PriorityData.Initialize();
        thrw.probabilityList = throwUtilityArgs.throwProbability;
        utilityCalculators.Add(thrw);
        
        var move = new MoveUtilityCalculator();
        utilityCalculators.Add(move);
        
        stateMatrix = new StateMatrix(this, 0.1f, utilityCalculators);
        stateController.SetAndStartStateMatrix(stateMatrix, this);
        stateController.SetTargetModule(targetModule);
    }

    #endregion

    private void OnTriggerEnter(Collider other)
    {
        stateController.OnTriggerEnter(other);
    }

    #region ball dodge/catch events

    private void SubscribeToBallEvents(bool sub)
    {
        return;
        if (sub)
        {
            foreach (var ball in playArea.dodgeBalls)
            {
                // ball.GetComponent<DodgeBall>().throwTrajectory += HandleBallTrajectory;
                // ball.GetComponent<DodgeBall>().ballNotLive += RemoveBallTrajectory;
            }
        }
        else
        {
            foreach (var ball in playArea.dodgeBalls)
            {
                // if (ball == null) continue;
                // ball.GetComponent<DodgeBall>().throwTrajectory -= HandleBallTrajectory;
                // ball.GetComponent<DodgeBall>().ballNotLive -= RemoveBallTrajectory;
            }
        }
    }

    // todo, we need to refactor this to be more dynamic
    public void DeRegisterBall(DodgeBall ball)
    {
        // ball.GetComponent<DodgeBall>().throwTrajectory -= HandleBallTrajectory;
        // ball.GetComponent<DodgeBall>().ballNotLive -= RemoveBallTrajectory;
        hasBall = false;
        leftBallIndex.SetBallType(BallType.None);
        rightBallIndex.SetBallType(BallType.None);
    }

    internal void HandleBallTrajectory(int ballIndex, Vector3 position, Vector3 trajectory)
    {
        liveBallTrajectories[ballIndex] = trajectory;
    }

    internal void RemoveBallTrajectory(int ballIndex)
    {
        liveBallTrajectories.Remove(ballIndex);
    }

    #endregion


    public void SwapActor(bool isBoss)
    {
        if (isBoss)
        {
            opposingTeam.actors.Clear();
            opposingTeam.actors.Add(playArea.aiSceneActors.boss);
        }
        else
        {
            opposingTeam.actors.Clear();
            playArea.aiSceneActors.lackeys.ForEach(actor => opposingTeam.actors.Add(actor));
        }
    }

    private void OnDestroy()
    {
        stateController.Dispose();
    }

    public void PickUpBall(DodgeBall ball)
    {
        if (!ball.gameObject.activeInHierarchy)
        {
            stateController.ChangeState(StateStruct.Move);
            return;
        }
        _possessedBall = ball;
        hasBall = true;
        ball.gameObject.SetActive(false);
        rightBallIndex.SetBallType(BallType.Dodgeball);
        animator.SetInteger(_SThrowVariation, Random.Range(0, 2));
    }

    public void SetPossessedBall(DodgeBall ball) => _possessedBall = ball;

    private IEnumerator DelayFrame()
    {
        yield return null;
    }
    public void ThrowBall()
    {
        hasBall = false;
        if (leftBallIndex._currentDodgeball) Debug.DrawRay(leftBallIndex.BallPosition, Vector3.up, Color.red, 2f);
        _stateBallThrown?.Invoke();
        animator.SetTrigger(_SCancelThrow);
    }

    internal override void SetOutOfPlay(bool value)
    {
        if (outOfPlay == value) return;
        base.SetOutOfPlay(value);
        animator.SetInteger(_SHitVariation, Random.Range(0, 3));
        animator.SetTrigger(_SPlayGhost);

        if (PhaseManager.phasing && !PhaseManager.CanSpawnTeam(team))
        {
            PlayPhaseOverOutro();
            return;
        }

        stateController.ChangeState(StateStruct.OutOfPlay);

        if (!_possessedBall) return;
        if (hasBall) _possessedBall._ballState = BallState.Dead;
    }

    public void RotateToTargetManually(GameObject currentTarget)
    {
        targetModule.LookAtTarget(currentTarget.transform.position);
        return;
        var directionToTarget = currentTarget.transform.position - transform.position;
        directionToTarget.y = 0;
        var flatDirection = directionToTarget.normalized;
    
        if (flatDirection != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(flatDirection);
    }
    
    protected virtual void Update()
    {
        currentState = stateController.GetStateName();
        if (stayIdle) return;
        if (hasBall) ballPossessionTime += Time.deltaTime;
        stateController.UpdateState();
    }

    internal virtual void TriggerRespawn()
    {
        gameObject.SetActive(false);
        transform.position = spawnInPos[Random.Range(0, spawnInPos.Count)].position;
        gameObject.SetActive(true);
        SetOutOfPlay(false);
        animator.Rebind();
        stateController.Rebind();
        stateController.SubscribeRolling();
    }


    public void SwitchBallSideToLeft()
    {
        leftBallIndex.SetBallType(BallType.Dodgeball);
        rightBallIndex.SetBallType(BallType.None);
    }

    public void DropBall()
    {
        var ballPos = Vector3.zero;
        if (leftBallIndex._currentDodgeball) ballPos = leftBallIndex.BallPosition;
        else if (rightBallIndex._currentDodgeball) ballPos = rightBallIndex.BallPosition;
        else
        {
            Debug.LogError("No ball to throw");
            return;
        }

        try
        {
            rightBallIndex.SetBallType(BallType.None);
            leftBallIndex.SetBallType(BallType.None);
        }
        catch
        {
            Debug.LogError("Ball on index is null");
        }

        var ball = BallPool.GetBall(ballPos);
        ball.SetDeadBall();
        ball.gameObject.SetActive(true);
        hasBall = false;
    }
}