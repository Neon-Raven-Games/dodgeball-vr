using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Hands.SinglePlayer.EnemyAI;
using Hands.SinglePlayer.EnemyAI.Priority;
using Hands.SinglePlayer.EnemyAI.Utilities;
using Multiplayer.SinglePlayer.EnemyAI.Utilities;
using Newtonsoft.Json;
using UnityEngine;
using Random = UnityEngine.Random;

public class ActorTeam
{
    public List<GameObject> actors;
    public Color color;
    public Transform playArea;
    public string layerName;
    public Transform outOfBounds { get; set; }
}

[Serializable]
public class GhostData
{
    public GameObject ghostLegs;
    public GameObject ghostHair;
    public GameObject humanLegs;
    public GameObject humanHair;

    public Material ghostMaterial;
    public Material humanMaterial;
    public GameObject bodyWithMaterial;
    public GameObject particleEffect;
}

public class DodgeballAI : Actor
{
    public void Logs()
    {
        string stateJson = "Call Stack:\n";
        stateJson += logBuffer.CaptureState(this);
        string logsJson = logBuffer.ExportLogs();
        stateJson += "\n\n" + "Logs\n" + logsJson;
        Debug.Log(stateJson);
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

    public bool stayIdle;

    // utility properties
    public DodgeUtilityArgs dodgeUtilityArgs;
    public TargetUtilityArgs targetUtilityArgs;
    public MoveUtilityArgs moveUtilityArgs;
    public CatchUtilityArgs catchUtilityArgs;
    public PickUpUtilityArgs pickUpUtilityArgs;
    public ThrowUtilityArgs throwUtilityArgs;
    public OutOfPlayUtilityArgs outOfBoundsUtilityArgs;

    private OutOfPlayUtility _outOfPlayUtility;
    private DodgeUtility _dodgeUtility;
    protected MoveUtility _moveUtility;
    public TargetUtility targetUtility;
    private CatchUtility _catchUtility;
    internal PickUpUtility _pickUpUtility;
    protected ThrowUtility _throwUtility;

    [SerializeField] private PriorityHandler priorityHandler;

    [SerializeField] private GhostData ghostData;

    // ai properties
    public AIState currentState;
    private Vector3 _targetPosition;

    // == ball possession ==
    internal float ballPossessionTime;


    // todo, handle this in rolling of move utility

    private float _nextMoveTime;

    // Weight for distance in utility calculation
    public float distanceWeight = 1.0f;

    // Difficulty factor for random component of utility calculation
    public float difficultyFactor = 1.0f;
    internal GameObject CurrentTarget => targetUtility.CurrentTarget;

    // Ball trajectories for live balls
    internal readonly Dictionary<int, Vector3> liveBallTrajectories = new();

    #region initialization

    public void DeRegisterBall(DodgeBall ball)
    {
        ball.GetComponent<DodgeBall>().throwTrajectory -= HandleBallTrajectory;
        ball.GetComponent<DodgeBall>().ballNotLive -= RemoveBallTrajectory;
        hasBall = false;
        leftBallIndex.SetBallType(BallType.None);
        rightBallIndex.SetBallType(BallType.None);
    }

    private void Start()
    {
        var colorLerp = GetComponent<ColorLerp>();
        if (colorLerp) colorLerp.onMaterialsLoaded += SwapPlayerBody;
        targetUtility.ResetTargetSwitchProbability();
        GameManager.onPhaseChange += OnPhaseChange;
    }

    private bool phaseChange;

    private void OnPhaseChange(BattlePhase obj)
    {
        if (stayIdle && obj == BattlePhase.Lackey)
        {
            phaseChange = false;
            stayIdle = false;
            return;
        }

        phaseChange = true;
    }

    private void SwapPlayerBody()
    {
        var colorLerp = GetComponent<ColorLerp>();
        colorLerp.onMaterialsLoaded -= SwapPlayerBody;
        ghostData.humanMaterial = colorLerp.GetPlayerMaterial();
    }

    private bool _isGhost;
    protected LogBuffer logBuffer;

    private void OnEnable()
    {
        PopulateTeamObjects();
        SubscribeToBallEvents(true);
        PopulateUtilities();

        logBuffer = GetComponent<LogBuffer>();
    }

    protected virtual void LogAction(string actionName, object additionalInfo = null)
    {
        if (logBuffer != null)
        {
            string logEntry = $"[{Time.time}] {actionName}";
            if (additionalInfo != null)
            {
                logEntry += $"\n[{Time.time} Context] {JsonConvert.SerializeObject(additionalInfo)}";
            }

            logBuffer.AppendLog(logEntry);
        }
    }


    private void SubscribeToBallEvents(bool sub)
    {
        if (sub)
        {
            foreach (var ball in playArea.dodgeBalls)
            {
                ball.GetComponent<DodgeBall>().throwTrajectory += HandleBallTrajectory;
                ball.GetComponent<DodgeBall>().ballNotLive += RemoveBallTrajectory;
            }
        }
        else
        {
            foreach (var ball in playArea.dodgeBalls)
            {
                if (ball == null) continue;
                ball.GetComponent<DodgeBall>().throwTrajectory -= HandleBallTrajectory;
                ball.GetComponent<DodgeBall>().ballNotLive -= RemoveBallTrajectory;
            }
        }
    }

    protected virtual void PopulateUtilities()
    {
        _utilityHandler = new UtilityHandler();
        targetUtility = new TargetUtility(targetUtilityArgs, this, priorityHandler.targetUtility);
        targetUtility.Initialize(friendlyTeam.playArea, team);

        _moveUtility = new MoveUtility(moveUtilityArgs);
        _moveUtility.Initialize(friendlyTeam.playArea, team);
        _utilityHandler.AddUtility(_moveUtility);

        _dodgeUtility = new DodgeUtility(dodgeUtilityArgs);
        _dodgeUtility.Initialize(friendlyTeam.playArea, team);
        _utilityHandler.AddUtility(_dodgeUtility);

        _catchUtility = new CatchUtility(catchUtilityArgs);
        _catchUtility.Initialize(friendlyTeam.playArea, team);
        _utilityHandler.AddUtility(_catchUtility);

        _pickUpUtility = new PickUpUtility(pickUpUtilityArgs, this);
        _pickUpUtility.Initialize(friendlyTeam.playArea, team);
        _utilityHandler.AddUtility(_pickUpUtility);

        _throwUtility = new ThrowUtility(throwUtilityArgs);
        _throwUtility.Initialize(friendlyTeam.playArea, team);
        _utilityHandler.AddUtility(_throwUtility);

        _outOfPlayUtility = new OutOfPlayUtility(outOfBoundsUtilityArgs);
        _outOfPlayUtility.Initialize(friendlyTeam.playArea, team);
        _utilityHandler.AddUtility(_outOfPlayUtility);
    }


    private void OnDisable()
    {
        foreach (var ball in playArea.dodgeBalls)
        {
            if (ball == null) continue;
            ball.GetComponent<DodgeBall>().throwTrajectory -= HandleBallTrajectory;
            ball.GetComponent<DodgeBall>().ballNotLive -= RemoveBallTrajectory;
        }
    }

    #endregion

    internal DodgeBall _possessedBall;
    [SerializeField] protected internal NetBallPossessionHandler leftBallIndex;
    [SerializeField] internal NetBallPossessionHandler rightBallIndex;


    public void PickUpBall(DodgeBall ball)
    {
        if (!ball.gameObject.activeInHierarchy)
        {
            currentState = AIState.BackOff;
            return;
        }

        hasBall = true;
        _possessedBall = ball;
        ball.SetOwner(this);
        ball.gameObject.SetActive(false);
        rightBallIndex.SetBallType(BallType.Dodgeball);
        animator.SetInteger(_SThrowVariation, Random.Range(0, 2));
    }

    [SerializeField] internal Animator animator;

    public void ThrowBall()
    {
        if (_possessedBall == null || !hasBall)
        {
            animator.SetTrigger(_SCancelThrow);
            throwAnimationPlaying = false;
            hasBall = false;
            return;
        }

        hasBall = false;
        animator.ResetTrigger(_SThrow);

        // todo, we can set the head better, null error message giving us gc alloc in editor
        var actor = CurrentTarget.GetComponent<Actor>();
        Vector3 enemyHeadPos = Vector3.zero;
        if (actor != null)
        {
            if (actor.head) enemyHeadPos = actor.head.position;
            else
            {
                enemyHeadPos = CurrentTarget.transform.position;
                enemyHeadPos.y += 1f;
            }
        }

        var ballPos = Vector3.zero;
        if (leftBallIndex._currentDodgeball) ballPos = leftBallIndex.BallPosition;
        else if (rightBallIndex._currentDodgeball) ballPos = rightBallIndex.BallPosition;
        else
        {
            Debug.LogError("No ball to throw");
        }

        var velocity = _throwUtility.CalculateThrow(this, ballPos, enemyHeadPos);
        _possessedBall.transform.position = ballPos + velocity * Time.deltaTime * 4;

        ballPossessionTime = 0;

        _possessedBall.gameObject.SetActive(true);
        rightBallIndex.SetBallType(BallType.None);
        leftBallIndex.SetBallType(BallType.None);

        var rb = _possessedBall.GetComponent<Rigidbody>();
        rb.velocity = velocity;

        _possessedBall.HandleThrowTrajectory(velocity);
        _possessedBall.SetLiveBall();
        _possessedBall._team = team;
        BallThrowRecovery().Forget();
    }

    [SerializeField] private float ballThrowRecovery = 0.5f;

    private async UniTaskVoid BallThrowRecovery()
    {
        await UniTask.Delay(TimeSpan.FromSeconds(ballThrowRecovery));
        _possessedBall = null;
        throwAnimationPlaying = false;
    }

    protected bool throwAnimationPlaying;

    public void ThrowBallAnimation()
    {
        throwAnimationPlaying = true;
        targetUtilityArgs.ik.solvers.lookAt.SetLookAtWeight(0f);
        targetUtility.ResetLookWeight();
        animator.SetTrigger(_SThrow);
    }

    internal void HandleBallTrajectory(int ballIndex, Vector3 position, Vector3 trajectory)
    {
        liveBallTrajectories[ballIndex] = trajectory;
    }

    internal void RemoveBallTrajectory(int ballIndex)
    {
        liveBallTrajectories.Remove(ballIndex);
    }

    private float _lastTargetScore;
    private static readonly int _SThrow = Animator.StringToHash("Throw");
    private static readonly int _SThrowVariation = Animator.StringToHash("ThrowVariation");
    private static readonly int _SXAxis = Animator.StringToHash("xAxis");
    private static readonly int _SYAxis = Animator.StringToHash("yAxis");
    private static readonly int _SCancelThrow = Animator.StringToHash("CancelThrow");

    private bool triggerOutOfPlay;
    private static readonly int _SPlayGhost = Animator.StringToHash("PlayGhost");
    private static readonly int _SHitVariation = Animator.StringToHash("HitVariation");

    protected UtilityHandler _utilityHandler;

    internal override void SetOutOfPlay(bool value)
    {
        if (outOfPlay == value) return;
        base.SetOutOfPlay(value);
        animator.SetInteger(_SHitVariation, Random.Range(0, 3));
        animator.SetTrigger(_SPlayGhost);

        if (!_possessedBall) return;
        if (hasBall) _possessedBall._ballState = BallState.Dead;
    }

    protected virtual void HandlePhaseChange()
    {
        if (stayIdle && phaseChange)
        {
            stayIdle = false;
            phaseChange = false;
            return;
        }
        
        if (currentState != AIState.Special && currentState != AIState.Throw && currentState != AIState.OutOfPlay)
        {
            stayIdle = true;
            phaseChange = false;
            Debug.Log("Notify idle for phase changing");
        }
    }

    // do extended update methods get called?
    protected virtual void Update()
    {
        if (stayIdle) return;
        if (phaseChange)
        {
            HandlePhaseChange();
            if (stayIdle) return;
        }

        if (currentState == AIState.Special)
        {
            HandleSpecial();
            return;
        }

        _pickUpUtility.Update();
        if (hasBall) ballPossessionTime += Time.deltaTime;

        // Override all other behaviors
        if (outOfPlay || currentState == AIState.OutOfPlay)
        {
            if (OutOfPlayUtilityMethod()) return;
        }

        if (throwAnimationPlaying && currentState != AIState.Throw)
        {
            throwAnimationPlaying = false;
        }

        if (throwAnimationPlaying) targetUtility.Execute(this);
        var targetScore = targetUtility.Roll(this);
        if (throwAnimationPlaying) return;
        if (targetScore > _lastTargetScore)
        {
            targetUtility.UpdateTarget(currentState);
        }

        _lastTargetScore = targetScore;

        if (currentState == AIState.BackOff)
        {
            if (!_moveUtility.BackOff(this)) currentState = AIState.Idle;
            else return;
        }

        _moveUtility.ResetBackOff();
        var utility = _utilityHandler.EvaluateUtility(this, out _);

        var inPickup = currentState == AIState.PickUp;

        currentState = _utilityHandler.GetState();

        if (inPickup && currentState != AIState.PickUp)
        {
            _pickUpUtility.StopPickup(this);
        }

        ExecuteCurrentState(utility);
    }

    private bool OutOfPlayUtilityMethod()
    {
        if (triggerOutOfPlay)
        {
            triggerOutOfPlay = false;
            ghostData.bodyWithMaterial.GetComponent<Renderer>().material = ghostData.ghostMaterial;
            ghostData.ghostLegs.SetActive(true);
            ghostData.ghostHair.SetActive(true);
            ghostData.humanLegs.SetActive(false);
            ghostData.humanHair.SetActive(false);
        }

        if (hasBall)
        {
            ballPossessionTime = 0;
            if (leftBallIndex._currentDodgeball)
            {
                _possessedBall.transform.position = leftBallIndex.BallPosition;
                leftBallIndex.SetBallType(BallType.None);
            }
            else
            {
                // todo, this needs better validation, and is temporary
                if (rightBallIndex._currentDodgeball)
                    _possessedBall.transform.position = rightBallIndex.BallPosition;
            }

            rightBallIndex.SetBallType(BallType.None);
            _possessedBall.gameObject.SetActive(true);

            var rb = _possessedBall.GetComponent<Rigidbody>();
            rb.velocity = transform.forward;

            _possessedBall = null;

            targetUtilityArgs.ik.solvers.lookAt.SetLookAtWeight(0f);
            targetUtility.ResetLookWeight();
            hasBall = false;
            throwAnimationPlaying = false;
        }

        animator.SetFloat(_SXAxis, 0);
        animator.SetFloat(_SYAxis, 0);
        _pickUpUtility.StopPickup(this);
        targetUtility.ResetLookWeight();

        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Praying Ghost")) triggerOutOfPlay = true;
        else return true;
        _outOfPlayUtility.Execute(this);

        if (_outOfPlayUtility.Roll(this) == 0) return true;

        ghostData.ghostLegs.SetActive(false);
        ghostData.ghostHair.SetActive(false);
        ghostData.humanLegs.SetActive(true);
        ghostData.humanHair.SetActive(true);
        animator.Play("Anticipating");
        ghostData.bodyWithMaterial.GetComponent<Renderer>().material = ghostData.humanMaterial;
        outOfPlay = false;
        triggerOutOfPlay = false;
        currentState = AIState.Idle;
        return false;
    }

    // invoke on trigger enter/dodgeball hit
    public void SetOutOfPlay()
    {
        currentState = AIState.OutOfPlay;
        outOfPlay = true;
    }


    internal bool IsTargetingBall(GameObject ball) => CurrentTarget == ball;

    // extract to state machine
    protected void ExecuteCurrentState(IUtility utility)
    {
        switch (currentState)
        {
            case AIState.PickUp:
                if (_pickUpUtility.Execute(this) == 1f)
                {
                    currentState = AIState.Possession;
                    _pickUpUtility.StopPickup(this);
                }

                if (!_moveUtility.PickupMove(this))
                {
                    currentState = AIState.BackOff;
                    _pickUpUtility.StopPickup(this);
                }

                break;
            case AIState.Throw:
                if (_throwUtility.Execute(this) == 0f)
                {
                    _moveUtility.Roll(this);
                    _moveUtility.PossessionMove(this);
                }
                else ThrowBallAnimation();

                break;
            case AIState.Possession:
                if (!_moveUtility.PossessionMove(this)) currentState = AIState.BackOff;
                break;
            default:
                utility.Execute(this);
                break;
        }
    }

    protected virtual void HandleSpecial()
    {
        _moveUtility.Execute(this);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("ai collision detected!");
    }

    public void SwitchBallSideToLeft()
    {
        leftBallIndex.SetBallType(BallType.Dodgeball);
        rightBallIndex.SetBallType(BallType.None);
    }
}