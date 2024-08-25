using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Gameplay.InGameEvents;
using Gameplay.Util;
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
    // targeting
    private DodgeballTargetModule targetUtility;
    internal GameObject CurrentTarget => targetUtility.CurrentTarget;
    internal DodgeBall BallTarget => targetUtility.BallTarget;
    internal Actor ActorTarget => targetUtility.ActorTarget;
    
    public bool stayIdle;
    public AIState currentState;
    public float difficultyFactor = 1.0f;
    public GameObject aiAvatar => ik.gameObject;
    public bool hasSpecials;
    public BipedIK ik;
    
    // needs to be fixed
    [SerializeField] private PriorityHandler priorityHandler;
    [SerializeField] protected internal NetBallPossessionHandler leftBallIndex;
    [SerializeField] internal NetBallPossessionHandler rightBallIndex;
    [SerializeField] internal Animator animator;
    [SerializeField] private GhostData ghostData;
    [SerializeField] private float ballThrowRecovery = 0.5f;

    public DodgeUtilityArgs dodgeUtilityArgs;
    public MoveUtilityArgs moveUtilityArgs;
    public CatchUtilityArgs catchUtilityArgs;
    public PickUpUtilityArgs pickUpUtilityArgs;
    public ThrowUtilityArgs throwUtilityArgs;
    public OutOfPlayUtilityArgs outOfBoundsUtilityArgs;

    internal PickUpUtility pickUpUtility;
    internal float ballPossessionTime;
    
    internal readonly Dictionary<int, Vector3> liveBallTrajectories = new();

    protected internal MoveUtility moveUtility;
    protected UtilityHandler utilityHandler;

    private bool _triggerOutOfPlay;
    private bool _throwAnimationPlaying;
    private OutOfPlayUtility _outOfPlayUtility;
    private DodgeUtility _dodgeUtility;
    private CatchUtility _catchUtility;
    private ThrowUtility _throwUtility;
    private Vector3 _targetPosition;
    private bool _isGhost;
    private float _nextMoveTime;
    private DodgeBall _possessedBall;

    private static readonly int _SThrow = Animator.StringToHash("Throw");
    private static readonly int _SThrowVariation = Animator.StringToHash("ThrowVariation");
    private static readonly int _SXAxis = Animator.StringToHash("xAxis");
    private static readonly int _SYAxis = Animator.StringToHash("yAxis");
    private static readonly int _SCancelThrow = Animator.StringToHash("CancelThrow");
    private static readonly int _SPlayGhost = Animator.StringToHash("PlayGhost");
    private static readonly int _SHitVariation = Animator.StringToHash("HitVariation");

    public List<Transform> spawnInPos;

    #region initialization

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
        stayIdle = false;
    }


    // forces us to look at target, shouldn't be doing this
    public void RotateToTargetManually()
    {
        var flatDirection = new Vector3(
            CurrentTarget.transform.position.x, 0,
            CurrentTarget.transform.position.z).normalized;
        transform.rotation = Quaternion.LookRotation(flatDirection);
    }
    protected virtual void PopulateUtilities()
    {
        targetUtility = new DodgeballTargetModule(this, priorityHandler.targetUtility);
        utilityHandler = new UtilityHandler();
        moveUtility = new MoveUtility(moveUtilityArgs);
        moveUtility.Initialize(friendlyTeam.playArea, team);
        utilityHandler.AddUtility(moveUtility);

        _dodgeUtility = new DodgeUtility(dodgeUtilityArgs);
        _dodgeUtility.Initialize(friendlyTeam.playArea, team);
        utilityHandler.AddUtility(_dodgeUtility);

        _catchUtility = new CatchUtility(catchUtilityArgs);
        _catchUtility.Initialize(friendlyTeam.playArea, team);
        utilityHandler.AddUtility(_catchUtility);

        pickUpUtility = new PickUpUtility(pickUpUtilityArgs, this);
        pickUpUtility.Initialize(friendlyTeam.playArea, team);
        utilityHandler.AddUtility(pickUpUtility);

        _throwUtility = new ThrowUtility(throwUtilityArgs);
        _throwUtility.Initialize(friendlyTeam.playArea, team);
        utilityHandler.AddUtility(_throwUtility);

        _outOfPlayUtility = new OutOfPlayUtility(outOfBoundsUtilityArgs);
        _outOfPlayUtility.Initialize(friendlyTeam.playArea, team);
        utilityHandler.AddUtility(_outOfPlayUtility);
    }

    #endregion

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
            _throwAnimationPlaying = false;
            animator.SetTrigger(_SCancelThrow);
            if (!hasBall) return;

            var actor = targetUtility.ActorTarget;
            if (!actor) RotateToTargetManually();
            
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

    private async UniTaskVoid BallThrowRecovery()
    {
        await UniTask.Delay(TimeSpan.FromSeconds(ballThrowRecovery));
        _possessedBall = null;
        _throwAnimationPlaying = false;
    }

    public void ThrowBallAnimation()
    {
        _throwAnimationPlaying = true;
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

    protected virtual void PlayPhaseOverOutro()
    {
        gameObject.SetActive(false);
    }

    protected virtual void Update()
    {
        if (stayIdle) return;

        if (hasBall) ballPossessionTime += Time.deltaTime;

        if (outOfPlay || currentState == AIState.OutOfPlay)
        {
            if (!PhaseManager.CanSpawnTeam(team))
            {
                PlayPhaseOverOutro();
                return;
            }

            currentState = AIState.OutOfPlay;
            if (NewOutOfPlayUtilityMethod()) return;
        }

        if (_throwAnimationPlaying && currentState != AIState.Throw)
            _throwAnimationPlaying = false;

        SetTarget();
        if (_throwAnimationPlaying) return;

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
        targetUtility.UpdateTarget();
    }

    private void SetStateFromUtility()
    {
        var utility = hasSpecials
            ? utilityHandler.EvaluateUtility(this, out _)
            : utilityHandler.EvaluateUtilityWithoutSpecial(this, out _);

        var inPickup = currentState == AIState.PickUp;
        currentState = utilityHandler.GetState();

        if (inPickup && currentState != AIState.PickUp) pickUpUtility.StopPickup(this);
        ExecuteCurrentState(utility);
    }

    private float _deadStep;

    private bool NewOutOfPlayUtilityMethod()
    {
        if (hasBall)
        {
            ThrowBall();
            ik.solvers.lookAt.SetLookAtWeight(0f);
            targetUtility.ResetLookWeight();
            hasBall = false;
            _throwAnimationPlaying = false;
        }

        animator.SetFloat(_SXAxis, 0);
        animator.SetFloat(_SYAxis, 0);
        if (pickUpUtility.pickup) pickUpUtility.StopPickup(this);
        targetUtility.ResetLookWeight();

        var newps = transform.position;
        newps.y -= Time.deltaTime * 5;
        transform.position = newps;
        // can we set this to a 1 second return
        if (setDead) return true;
        setDead = true;
        
        stayIdle = true;
        TimerManager.AddTimer(2.5f, TriggerRespawn);
        return false;
    }
    
    private bool setDead;

    private void TriggerRespawn()
    {
        setDead = false;
        gameObject.SetActive(false);
        SetOutOfPlay(false);
        outOfPlay = false;
        _triggerOutOfPlay = false;
        _throwAnimationPlaying = false;
        SpawnIn();
    }


    protected virtual void SpawnIn()
    {
        gameObject.SetActive(false);
        transform.position = spawnInPos[Random.Range(0, spawnInPos.Count)].position;
        gameObject.SetActive(true);
        stayIdle = false;
    }

    private bool OutOfPlayUtilityMethod()
    {
        if (_triggerOutOfPlay)
        {
            _triggerOutOfPlay = false;
            ghostData.bodyWithMaterial.GetComponent<Renderer>().material = ghostData.ghostMaterial;
            ghostData.ghostLegs.SetActive(true);
            ghostData.ghostHair.SetActive(true);
            ghostData.humanLegs.SetActive(false);
            ghostData.humanTshirt.SetActive(false);
        }

        if (hasBall)
        {
            ThrowBall();

            ik.solvers.lookAt.SetLookAtWeight(0f);
            targetUtility.ResetLookWeight();
            hasBall = false;
            _throwAnimationPlaying = false;
        }

        animator.SetFloat(_SXAxis, 0);
        animator.SetFloat(_SYAxis, 0);
        if (pickUpUtility.pickup) pickUpUtility.StopPickup(this);
        targetUtility.ResetLookWeight();

        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Praying Ghost")) _triggerOutOfPlay = true;
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
        _triggerOutOfPlay = false;
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
                    targetUtility.UpdateTarget();
                }

                if (!moveUtility.PickupMove(this))
                {
                    currentState = AIState.BackOff;
                }

                break;
            case AIState.Throw:
                if (_throwUtility.Execute(this) == 0f) moveUtility.Roll(this);
                else if (!_throwAnimationPlaying) ThrowBallAnimation();
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