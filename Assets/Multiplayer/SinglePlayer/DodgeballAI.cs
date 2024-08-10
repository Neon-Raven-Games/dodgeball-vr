using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Hands.SinglePlayer.EnemyAI;
using Hands.SinglePlayer.EnemyAI.Priority;
using Hands.SinglePlayer.EnemyAI.Utilities;
using Multiplayer.SinglePlayer.EnemyAI.Utilities;
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
        Possession
    }

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
    private MoveUtility _moveUtility;
    public TargetUtility targetUtility;
    private CatchUtility _catchUtility;
    internal PickUpUtility _pickUpUtility;
    private ThrowUtility _throwUtility;

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
        targetUtility.ResetTargetSwitchProbability();
    }

    private bool _isGhost;

    private void OnEnable()
    {
        PopulateTeamObjects();
        SubscribeToBallEvents(true);
        PopulateUtilities();
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

    private void PopulateUtilities()
    {
        _utilityHandler = new UtilityHandler();
        targetUtility = new TargetUtility(targetUtilityArgs, this, priorityHandler.targetUtility);
        targetUtility.Initialize(friendlyTeam.playArea, team);
        // _utilityHandler.AddUtility(targetUtility);
        
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

    private DodgeBall _possessedBall;
    [SerializeField] private NetBallPossessionHandler leftBallIndex;
    [SerializeField] private NetBallPossessionHandler rightBallIndex;


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
            // got out mid throw, poor bastard
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

        var velocity = _throwUtility.CalculateThrow(this, rightBallIndex.BallPosition, enemyHeadPos);
        _possessedBall.transform.position = rightBallIndex.BallPosition + velocity * Time.deltaTime * 2;

        ballPossessionTime = 0;

        _possessedBall.gameObject.SetActive(true);
        rightBallIndex.SetBallType(BallType.None);

        var rb = _possessedBall.GetComponent<Rigidbody>();
        rb.velocity = velocity;

        _possessedBall.HandleThrowTrajectory(velocity);
        _possessedBall.SetLiveBall();
        BallThrowRecovery().Forget();
    }

    [SerializeField] private float ballThrowRecovery = 0.5f;

    private async UniTaskVoid BallThrowRecovery()
    {
        await UniTask.Delay(TimeSpan.FromSeconds(ballThrowRecovery));
        await UniTask.SwitchToMainThread();
        _possessedBall = null;
        throwAnimationPlaying = false;
    }

    private bool throwAnimationPlaying;

    private void ThrowBallAnimation()
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

    internal override void SetOutOfPlay(bool value)
    {
        if (outOfPlay == value) return;
        base.SetOutOfPlay(value);
        if (hasBall) _possessedBall._ballState = BallState.Dead;
        // todo, hits sub state machine - 2
        animator.SetInteger(_SHitVariation, Random.Range(0, 3));
        animator.SetTrigger(_SPlayGhost);
    }

    private UtilityHandler _utilityHandler;
    private void Update()
    {
        _pickUpUtility.Update();
        if (hasBall) ballPossessionTime += Time.deltaTime;

        // Override all other behaviors
        if (outOfPlay || currentState == AIState.OutOfPlay)
        {
            if (OutOfPlayUtilityMethod()) return;
        }

        if (throwAnimationPlaying) targetUtility.Execute(this);
        var targetScore = targetUtility.Roll(this);
        if (throwAnimationPlaying) return;
        if (targetScore > _lastTargetScore) targetUtility.UpdateTarget(currentState);
        _lastTargetScore = targetScore;

        if (currentState == AIState.BackOff)
        {
            if (!_moveUtility.BackOff(this)) currentState = AIState.Idle;
            else return;
        }

        _moveUtility.ResetBackOff();

        var utility = _utilityHandler.EvaluateUtility(this);
        var inPickup = currentState == AIState.PickUp;
        currentState = _utilityHandler.GetState();
            
        if (inPickup && currentState != AIState.PickUp) 
            _pickUpUtility.StopPickup(this);

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
            _possessedBall.transform.position = rightBallIndex.BallPosition;
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
    private void ExecuteCurrentState(IUtility utility)
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

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("ai collision detected!");
    }
}