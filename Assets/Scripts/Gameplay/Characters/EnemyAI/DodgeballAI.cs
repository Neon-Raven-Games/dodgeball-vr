using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Hands.SinglePlayer.EnemyAI;
using Hands.SinglePlayer.EnemyAI.Priority;
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

[Serializable]
public class GhostData
{
    public GameObject ghostLegs;
    public GameObject ghostHair;
    public GameObject humanLegs;
    public GameObject humanTshirt;

    public Material ghostMaterial;
    public Material humanMaterial;
    public GameObject bodyWithMaterial;
}

public class DodgeballAI : Actor
{
    public bool stayIdle;
    public AIState currentState;
    public float difficultyFactor = 1.0f;

    public BipedIK ik;
    [SerializeField] private PriorityHandler priorityHandler;
    [SerializeField] protected internal NetBallPossessionHandler leftBallIndex;
    [SerializeField] internal NetBallPossessionHandler rightBallIndex;
    [SerializeField] internal Animator animator;
    [SerializeField] private GhostData ghostData;
    
    public DodgeUtilityArgs dodgeUtilityArgs;
    public TargetUtilityArgs targetUtilityArgs;
    public MoveUtilityArgs moveUtilityArgs;
    public CatchUtilityArgs catchUtilityArgs;
    public PickUpUtilityArgs pickUpUtilityArgs;
    public ThrowUtilityArgs throwUtilityArgs;
    public OutOfPlayUtilityArgs outOfBoundsUtilityArgs;
    internal float ballPossessionTime;
    internal GameObject CurrentTarget => targetUtility.CurrentTarget;
    internal readonly Dictionary<int, Vector3> liveBallTrajectories = new();

    // todo, move to private
    public TargetUtility targetUtility;
    internal PickUpUtility pickUpUtility;
    protected internal MoveUtility moveUtility;
    
    private OutOfPlayUtility _outOfPlayUtility;
    private DodgeUtility _dodgeUtility;
    private CatchUtility _catchUtility;
    private ThrowUtility _throwUtility;

    private Vector3 _targetPosition;
    private bool _isGhost;
    private float _nextMoveTime;
    private DodgeBall _possessedBall;
    public GameObject aiAvatar => ik.gameObject;

    public bool hasSpecials;

    private static readonly int _SThrow = Animator.StringToHash("Throw");
    private static readonly int _SThrowVariation = Animator.StringToHash("ThrowVariation");
    private static readonly int _SXAxis = Animator.StringToHash("xAxis");
    private static readonly int _SYAxis = Animator.StringToHash("yAxis");
    private static readonly int _SCancelThrow = Animator.StringToHash("CancelThrow");

    private bool triggerOutOfPlay;
    private static readonly int _SPlayGhost = Animator.StringToHash("PlayGhost");
    private static readonly int _SHitVariation = Animator.StringToHash("HitVariation");

    protected UtilityHandler _utilityHandler;

    #region initialization

    private void Start()
    {
        var colorLerp = GetComponent<ColorLerp>();
        if (colorLerp) colorLerp.onMaterialsLoaded += SwapPlayerBody;
    }

    private void SwapPlayerBody()
    {
        var colorLerp = GetComponent<ColorLerp>();
        colorLerp.onMaterialsLoaded -= SwapPlayerBody;
        ghostData.humanMaterial = colorLerp.GetPlayerMaterial();
    }


    private void OnEnable()
    {
        PopulateTeamObjects();
        SubscribeToBallEvents(true);
        PopulateUtilities();
    }

    protected virtual void PopulateUtilities()
    {
        _utilityHandler = new UtilityHandler();

        targetUtility = new TargetUtility(targetUtilityArgs, this, priorityHandler.targetUtility);
        targetUtility.Initialize(friendlyTeam.playArea, team);

        moveUtility = new MoveUtility(moveUtilityArgs);
        moveUtility.Initialize(friendlyTeam.playArea, team);
        _utilityHandler.AddUtility(moveUtility);

        _dodgeUtility = new DodgeUtility(dodgeUtilityArgs);
        _dodgeUtility.Initialize(friendlyTeam.playArea, team);
        _utilityHandler.AddUtility(_dodgeUtility);

        _catchUtility = new CatchUtility(catchUtilityArgs);
        _catchUtility.Initialize(friendlyTeam.playArea, team);
        _utilityHandler.AddUtility(_catchUtility);

        pickUpUtility = new PickUpUtility(pickUpUtilityArgs, this);
        pickUpUtility.Initialize(friendlyTeam.playArea, team);
        _utilityHandler.AddUtility(pickUpUtility);

        _throwUtility = new ThrowUtility(throwUtilityArgs);
        _throwUtility.Initialize(friendlyTeam.playArea, team);
        _utilityHandler.AddUtility(_throwUtility);

        _outOfPlayUtility = new OutOfPlayUtility(outOfBoundsUtilityArgs);
        _outOfPlayUtility.Initialize(friendlyTeam.playArea, team);
        _utilityHandler.AddUtility(_outOfPlayUtility);
    }

    #endregion

    #region ball dodge/catch events

    private void SubscribeToBallEvents(bool sub)
    {
        Debug.Log("Subscribing to ball events needs to be fixed");
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

    private void OnDisable()
    {
        // foreach (var ball in playArea.dodgeBalls)
        // {
        //     if (ball == null) continue;
        //     ball.GetComponent<DodgeBall>().throwTrajectory -= HandleBallTrajectory;
        //     ball.GetComponent<DodgeBall>().ballNotLive -= RemoveBallTrajectory;
        // }
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


    public void PickUpBall(DodgeBall ball)
    {
        if (!ball.gameObject.activeInHierarchy)
        {
            currentState = AIState.BackOff;
            return;
        }

        hasBall = true;
        ball.gameObject.SetActive(false);
        rightBallIndex.SetBallType(BallType.Dodgeball);
        animator.SetInteger(_SThrowVariation, Random.Range(0, 2));
    }

    public void SetPossessedBall(DodgeBall ball)
    {
        _possessedBall = ball;
    }

    public void ThrowBall()
    {
        lock (leftBallIndex)
        {
            throwAnimationPlaying = false;
            animator.SetTrigger(_SCancelThrow);
            if (!hasBall) return;

            var actor = targetUtility.ActorTarget;
            if (!actor) targetUtility.Roll(this);
            hasBall = false;
            actor = targetUtility.ActorTarget;
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

            if (!_possessedBall) _possessedBall = BallPool.GetBall(ballPos + velocity * Time.deltaTime * 2);
            else _possessedBall.transform.position = ballPos + velocity * Time.deltaTime * 4;

            try
            {
                rightBallIndex.SetBallType(BallType.None);
                leftBallIndex.SetBallType(BallType.None);
            }
            catch
            {
                Debug.LogError("Ball on index is null");
            }

            ballPossessionTime = 0;
            _possessedBall.SetOwner(this);
            _possessedBall.gameObject.SetActive(true);


            var rb = _possessedBall.GetComponent<Rigidbody>();
            rb.velocity = velocity;

            _possessedBall.HandleThrowTrajectory(velocity);
            _possessedBall.SetLiveBall();
            _possessedBall._team = team;
        }

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
        ik.solvers.lookAt.SetLookAtWeight(0f);
        targetUtility.ResetLookWeight();
        animator.SetTrigger(_SThrow);
    }


    internal override void SetOutOfPlay(bool value)
    {
        if (outOfPlay == value) return;
        base.SetOutOfPlay(value);
        animator.SetInteger(_SHitVariation, Random.Range(0, 3));
        animator.SetTrigger(_SPlayGhost);

        if (!_possessedBall) return;
        if (hasBall) _possessedBall._ballState = BallState.Dead;
    }


    protected virtual void Update()
    {
        if (stayIdle) return;

        if (hasBall) ballPossessionTime += Time.deltaTime;
        
        if (outOfPlay || currentState == AIState.OutOfPlay)
        {
            currentState = AIState.OutOfPlay;
            if (OutOfPlayUtilityMethod()) return;
        }

        if (throwAnimationPlaying && currentState != AIState.Throw) 
            throwAnimationPlaying = false;

        SetTarget();
        if (throwAnimationPlaying) return;

        if (currentState == AIState.BackOff)
        {
            if (!moveUtility.BackOff(this)) currentState = AIState.Idle;
            else return;
        }

        moveUtility.ResetBackOff();
        SetStateFromUtility();
    }

    private void SetTarget()
    {
        targetUtility.Roll(this);
        targetUtility.Execute(this);
    }

    private void SetStateFromUtility()
    {
        var utility = hasSpecials
            ? _utilityHandler.EvaluateUtility(this, out _)
            : _utilityHandler.EvaluateUtilityWithoutSpecial(this, out _);

        var inPickup = currentState == AIState.PickUp;
        currentState = _utilityHandler.GetState();

        if (inPickup && currentState != AIState.PickUp) pickUpUtility.StopPickup(this);
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
            ghostData.humanTshirt.SetActive(false);
        }

        if (hasBall)
        {
            ThrowBall();

            targetUtilityArgs.ik.solvers.lookAt.SetLookAtWeight(0f);
            targetUtility.ResetLookWeight();
            hasBall = false;
            throwAnimationPlaying = false;
        }

        animator.SetFloat(_SXAxis, 0);
        animator.SetFloat(_SYAxis, 0);
        if (pickUpUtility.pickup) pickUpUtility.StopPickup(this);
        targetUtility.ResetLookWeight();

        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Praying Ghost")) triggerOutOfPlay = true;
        else return true;
        _outOfPlayUtility.Execute(this);

        if (_outOfPlayUtility.Roll(this) == 0) return true;

        ghostData.ghostLegs.SetActive(false);
        ghostData.ghostHair.SetActive(false);
        ghostData.humanLegs.SetActive(true);
        ghostData.humanTshirt.SetActive(true);
        animator.Play("Anticipating");
        ghostData.bodyWithMaterial.GetComponent<Renderer>().material = ghostData.humanMaterial;
        outOfPlay = false;
        triggerOutOfPlay = false;
        currentState = AIState.Idle;
        return false;
    }

    internal bool IsTargetingBall(GameObject ball) => CurrentTarget == ball;

    // extract to state machine
    protected void ExecuteCurrentState(IUtility utility)
    {
        switch (currentState)
        {
            case AIState.PickUp:
                if (pickUpUtility.Execute(this) == 1f)
                {
                    currentState = AIState.Possession;
                    pickUpUtility.StopPickup(this);
                }
                else if (pickUpUtility.Execute(this) == -1f)
                {
                    currentState = AIState.BackOff;
                    targetUtility.Roll(this);
                }

                if (!moveUtility.PickupMove(this))
                {
                    currentState = AIState.BackOff;
                }

                break;
            case AIState.Throw:
                if (_throwUtility.Execute(this) == 0f) moveUtility.Roll(this);
                else if (!throwAnimationPlaying) ThrowBallAnimation();
                break;
            case AIState.Possession:
                if (!moveUtility.PossessionMove(this)) currentState = AIState.BackOff;
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

    public void SwitchBallSideToLeft()
    {
        leftBallIndex.SetBallType(BallType.Dodgeball);
        rightBallIndex.SetBallType(BallType.None);
    }
}