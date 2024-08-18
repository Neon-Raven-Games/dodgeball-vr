using Hands.SinglePlayer.EnemyAI;
using Multiplayer.SinglePlayer.EnemyAI.Utilities;
using UnityEngine;

public class NinjaAgent : DodgeballAI
{
    [SerializeField] private ShadowStepUtilityArgs shadowStepArgs;
    [SerializeField] private SubstitutionUtilityArgs substitutionUtilityArgs;
    [SerializeField] private NinjaHandSignUtilityArgs handSignUtilityArgs;
    [SerializeField] private FakeoutUtilityArgs fakeoutUtilityArgs;
    private FakeoutBallUtility _fakeoutBallUtility;
    private SubstitutionUtility _substitutionUtility;
    private ShadowStepUtility _shadowStepUtility;
    private NinjaHandSignUtility _handSignUtility;

    protected override void PopulateUtilities()
    {
        base.PopulateUtilities();
        _substitutionUtility = new SubstitutionUtility(substitutionUtilityArgs, AIState.Special, this);
        _substitutionUtility.Initialize(friendlyTeam.playArea, team);

        _handSignUtility = new NinjaHandSignUtility(handSignUtilityArgs, this);
        _handSignUtility.Initialize(friendlyTeam.playArea, team);

        _shadowStepUtility = new ShadowStepUtility(shadowStepArgs, AIState.Special, this);
        _shadowStepUtility.Initialize(friendlyTeam.playArea, team);

        _fakeoutBallUtility = new FakeoutBallUtility(fakeoutUtilityArgs, AIState.Special, this);
        _fakeoutBallUtility.Initialize(friendlyTeam.playArea, team);

        _utilityHandler.AddUtility(_shadowStepUtility);
        _utilityHandler.AddUtility(_substitutionUtility);
        _utilityHandler.AddUtility(_fakeoutBallUtility);
    }

    protected override void Update()
    {
        base.Update();
        if (_fakeoutBallUtility.active) return;
        if (IsOutOfPlay() || currentState == AIState.PickUp || currentState == AIState.Throw) return;
        if (currentState != AIState.Special && _handSignUtility.active) return;

        if (!_shadowStepUtility._shadowSteppingSequencePlaying &&
            !_substitutionUtility.inSequence &&
            _handSignUtility.Roll(this) > 0)
        {
            _handSignUtility.Execute(this);
        }
    }

    internal override void SetOutOfPlay(bool value)
    {
        if (_substitutionUtility.inSequence || _shadowStepUtility._shadowSteppingSequencePlaying)
            return;
        base.SetOutOfPlay(value);
        currentState = AIState.OutOfPlay;
        _handSignUtility.Cooldown();
        leftBallIndex.SetBallType(BallType.None);
    }

    protected override void HandleSpecial()
    {
        if (_fakeoutBallUtility.active)
        {
            return;
        }
        if (!_substitutionUtility.inSequence && !_shadowStepUtility._shadowSteppingSequencePlaying)
        {
            _handSignUtility.Cooldown();
            _substitutionUtility.Reset();
            currentState = AIState.Move;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_fakeoutBallUtility.active)
        {
            Debug.Log("Fakeout cancel");
            return;
        }

        if (_shadowStepUtility._shadowSteppingSequencePlaying)
        {
            Debug.Log("ShadowStep cancel");
            return;
        }

        if (_substitutionUtility.inSequence)
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

            _substitutionUtility.BallInTrigger();
            var rb = db.GetComponent<Rigidbody>();
            rb.velocity = Vector3.Reflect(rb.velocity, transform.forward);
            db.transform.position += rb.velocity.normalized * 3 * Time.fixedDeltaTime;
            Debug.Log("Ball in trigger, executing");
            _substitutionUtility.Execute(this);
            LogAction("Substitution execution");
            _handSignUtility.Cooldown();
            if (!_substitutionUtility.inSequence)
                currentState = AIState.Move;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // if (other.gameObject.layer == LayerMask.NameToLayer("Ball"))
        // _substitutionUtility.ballInTrigger = false;
    }

    public void InitialShadowStepFinished()
    {
    }
}