using Hands.SinglePlayer.EnemyAI;
using Multiplayer.SinglePlayer.EnemyAI.Utilities;
using UnityEngine;

public class NinjaAgent : DodgeballAI
{
    [SerializeField] private ShadowStepUtilityArgs shadowStepArgs;
    [SerializeField] private ShadowStepUtilityArgs substitutionArgs;
    [SerializeField] private NinjaHandSignUtilityArgs handSignUtilityArgs;
    private SubstitutionUtility _substitutionUtility;
    private ShadowStepUtility _shadowStepUtility;
    private NinjaHandSignUtility _handSignUtility;

    protected override void PopulateUtilities()
    {
        base.PopulateUtilities();
        _substitutionUtility = new SubstitutionUtility(substitutionArgs, AIState.Special, this);
        _substitutionUtility.Initialize(friendlyTeam.playArea, team);

        _handSignUtility = new NinjaHandSignUtility(handSignUtilityArgs, this);
        _handSignUtility.Initialize(friendlyTeam.playArea, team);

        _shadowStepUtility = new ShadowStepUtility(shadowStepArgs, AIState.Special, this);
        _shadowStepUtility.Initialize(friendlyTeam.playArea, team);

        _utilityHandler.AddUtility(_shadowStepUtility);
        _utilityHandler.AddUtility(_substitutionUtility);
    }

    protected override void Update()
    {
        base.Update();
        if (IsOutOfPlay() || currentState == AIState.PickUp || currentState == AIState.Throw) return;
        if (currentState != AIState.Special && _handSignUtility.active)
        {
            if (_substitutionUtility.Roll(this) > 0)
            {
                currentState = AIState.Special;
                _substitutionUtility.Execute(this);
            }
            return;
        }

        if (_handSignUtility.Roll(this) > 0)
            _handSignUtility.Execute(this);
        
    }

    internal override void SetOutOfPlay(bool value)
    {
        if (currentState == AIState.Special || _substitutionUtility._shadowSteppingSequencePlaying || _shadowStepUtility._shadowSteppingSequencePlaying)
            return;
        base.SetOutOfPlay(value);
    }

    protected override void HandleSpecial()
    {
        return;
        if (IsOutOfPlay() || currentState == AIState.PickUp)
        {
            return;
        }

        if (_shadowStepUtility.Roll(this) > 0)
        {
            Debug.Log("Shadow Step");
            _shadowStepUtility.Execute(this);
            return;
        }

        var execute = _substitutionUtility.Roll(this);
        if (execute > 0)
        {
            Debug.Log("Substitution");
            _substitutionUtility.Execute(this);
        }
        else
        {
            Debug.Log("Cancelling Special");
            currentState = hasBall ? AIState.Possession : AIState.Idle;
            _moveUtility.Execute(this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_shadowStepUtility._shadowSteppingSequencePlaying)
        {
            Debug.Log("ShadowStep cancel");
            return;
        }

        if (_substitutionUtility._shadowSteppingSequencePlaying)
        {
            Debug.Log("Substitution cancel");
            return;
        }
        if (other.gameObject.layer == LayerMask.NameToLayer("Ball"))
        {
            var db = other.GetComponent<DodgeBall>();
            if (db._ballState != BallState.Live || db._team == team)
            {
                Debug.Log("Ball not live or same team");
                return;
            }

            _substitutionUtility.ballInTrigger = true;
            var rb = db.GetComponent<Rigidbody>();
            _substitutionUtility.ballDirection = rb.velocity;
            _substitutionUtility.ballHitPoint = db.transform.position;
            rb.velocity = Vector3.Reflect(rb.velocity, transform.forward);
            {
                Debug.Log("Substitution");
                _substitutionUtility.Execute(this);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ball"))
            _substitutionUtility.ballInTrigger = false;
    }

    public void InitialShadowStepFinished()
    {
        _shadowStepUtility.InitialShadowStepFinished();
    }

    public void InitialSubstitutionFinished()
    {
        _substitutionUtility.InitialShadowStepFinished();
    }
}