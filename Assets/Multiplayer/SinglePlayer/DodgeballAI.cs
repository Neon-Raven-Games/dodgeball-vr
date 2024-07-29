using System;
using System.Collections;
using System.Collections.Generic;
using Hands.SinglePlayer.EnemyAI;
using Hands.SinglePlayer.EnemyAI.Priority;
using Hands.SinglePlayer.EnemyAI.Utilities;
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

    private void Start()
    {
        targetUtility.ResetTargetSwitchProbability();
    }

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
        targetUtility = new TargetUtility(targetUtilityArgs, this, priorityHandler.targetUtility);
        _moveUtility = new MoveUtility(moveUtilityArgs);
        _dodgeUtility = new DodgeUtility(dodgeUtilityArgs);
        _catchUtility = new CatchUtility(catchUtilityArgs);
        _pickUpUtility = new PickUpUtility(pickUpUtilityArgs);
        _throwUtility = new ThrowUtility(throwUtilityArgs);
        _outOfPlayUtility = new OutOfPlayUtility(outOfBoundsUtilityArgs);
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
        ball.SetOwner(team);
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
        var enemyHeadPos = CurrentTarget.GetComponent<Actor>().head.position;
        var velocity = _throwUtility.CalculateThrow(this, rightBallIndex.BallPosition, enemyHeadPos);
        _possessedBall.transform.position = rightBallIndex.BallPosition + velocity * Time.deltaTime * 2;

        ballPossessionTime = 0;

        _possessedBall.gameObject.SetActive(true);
        rightBallIndex.SetBallType(BallType.None);

        var rb = _possessedBall.GetComponent<Rigidbody>();
        rb.velocity = velocity;

        _possessedBall.HandleThrowTrajectory(velocity);
        _possessedBall.SetLiveBall();
        StartCoroutine(BallThrowRecovery());
    }

    [SerializeField] private float ballThrowRecovery = 0.5f;

    private IEnumerator BallThrowRecovery()
    {
        yield return new WaitForSeconds(ballThrowRecovery);
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

    private void Update()
    {
        if (hasBall) ballPossessionTime += Time.deltaTime;

        // Override all other behaviors
        if (outOfPlay || currentState == AIState.OutOfPlay)
        {
            if (!triggerOutOfPlay)
            {
                ghostData.particleEffect.SetActive(true);
                triggerOutOfPlay = true;
                ghostData.bodyWithMaterial.GetComponent<Renderer>().material = ghostData.ghostMaterial;
                animator.SetTrigger(_SPlayGhost);
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
            _outOfPlayUtility.Execute(this);
            _pickUpUtility.StopPickup(this);
            targetUtility.ResetLookWeight();
            if (_outOfPlayUtility.Roll(this) == 0) return;
            ghostData.ghostLegs.SetActive(false);
            ghostData.ghostHair.SetActive(false);
            ghostData.humanLegs.SetActive(true);
            ghostData.humanHair.SetActive(true);
            animator.Play("Anticipating");
            ghostData.bodyWithMaterial.GetComponent<Renderer>().material = ghostData.humanMaterial;
            outOfPlay = false;
            triggerOutOfPlay = false;
            currentState = AIState.Idle;
        }

        if (throwAnimationPlaying) return;
        var targetScore = targetUtility.Roll(this);
        if (targetScore > _lastTargetScore) targetUtility.UpdateTarget(currentState);
        _lastTargetScore = targetScore;

        if (currentState == AIState.BackOff)
        {
            if (!_moveUtility.BackOff(this)) currentState = AIState.Idle;
            else return;
        }

        _moveUtility.ResetBackOff();

        var dodgeUtility = _dodgeUtility.Roll(this);
        var catchUtility = _catchUtility.Roll(this);
        var pickUpUtility = _pickUpUtility.Roll(this);
        var throwUtility = _throwUtility.Roll(this);
        var moveUtility = _moveUtility.Roll(this);

        if (dodgeUtility > catchUtility && dodgeUtility > pickUpUtility && dodgeUtility > throwUtility &&
            dodgeUtility > moveUtility)
        {
            currentState = AIState.Dodge;
        }
        else if (catchUtility > pickUpUtility && catchUtility > throwUtility && catchUtility > moveUtility)
        {
            Debug.Log($"[{gameObject.name}] is catching a ball!");
            currentState = AIState.Catch;
        }
        else if (pickUpUtility > throwUtility && pickUpUtility > moveUtility)
        {
            currentState = AIState.PickUp;
        }
        else if (throwUtility > moveUtility)
        {
            currentState = AIState.Throw;
        }
        else
        {
            if (currentState == AIState.PickUp) _pickUpUtility.StopPickup(this);
            currentState = AIState.Move;
        }

        ExecuteCurrentState();
    }

    // invoke on trigger enter/dodgeball hit
    public void SetOutOfPlay()
    {
        currentState = AIState.OutOfPlay;
        outOfPlay = true;
    }


    internal bool IsTargetingBall(GameObject ball) => CurrentTarget == ball;

    private void ExecuteCurrentState()
    {
        switch (currentState)
        {
            case AIState.Dodge:
                _dodgeUtility.Execute(this);
                break;
            case AIState.Catch:
                _catchUtility.Execute(this);
                break;
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
                    // todo, we need to handle the timer better
                    _moveUtility.Roll(this);
                    _moveUtility.PossessionMove(this);
                }
                else ThrowBallAnimation();

                break;
            case AIState.Move:
                _moveUtility.Execute(this);
                break;
            case AIState.OutOfPlay:
                break;
            case AIState.Possession:
                if (!_moveUtility.PossessionMove(this)) currentState = AIState.BackOff;
                break;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("ai collision detected!");
    }
}