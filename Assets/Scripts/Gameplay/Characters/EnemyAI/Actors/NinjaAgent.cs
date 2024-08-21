using Hands.SinglePlayer.EnemyAI;
using Hands.SinglePlayer.EnemyAI.StatefulRefactor;
using Multiplayer.SinglePlayer.EnemyAI.Utilities;
using UnityEngine;
using Random = UnityEngine.Random;

public class NinjaAgent : DodgeballAI
{
    public NinjaState State => _stateController.State;
    
    [SerializeField] private ShadowStepUtilityArgs shadowStepArgs;
    [SerializeField] private SubstitutionUtilityArgs substitutionUtilityArgs;
    [SerializeField] private NinjaHandSignUtilityArgs handSignUtilityArgs;
    [SerializeField] private FakeoutUtilityArgs fakeoutUtilityArgs;
    [SerializeField] private SmokeBombUtilityArgs smokeBombUtilityArgs;
    
    internal Vector3 currentSmokeBombPosition;
    private FakeoutBallUtility _fakeoutBallUtility;
    private SubstitutionUtility _substitutionUtility;
    private ShadowStepUtility _shadowStepUtility;
    private NinjaHandSignUtility _handSignUtility;

    private DerivedAIStateController<NinjaState> _stateController;
    private CharacterGizmo _gizmo;

    protected override void PopulateUtilities()
    {
        _gizmo = GetComponent<CharacterGizmo>();
        base.PopulateUtilities();
        _substitutionUtility = new SubstitutionUtility(substitutionUtilityArgs, AIState.Special, this);
        _substitutionUtility.Initialize(friendlyTeam.playArea, team);

        _handSignUtility = new NinjaHandSignUtility(handSignUtilityArgs, this);
        _handSignUtility.Initialize(friendlyTeam.playArea, team);

        _shadowStepUtility = new ShadowStepUtility(shadowStepArgs, AIState.Special, this);
        _shadowStepUtility.Initialize(friendlyTeam.playArea, team);

        _fakeoutBallUtility = new FakeoutBallUtility(fakeoutUtilityArgs, AIState.Special, this);
        _fakeoutBallUtility.Initialize(friendlyTeam.playArea, team);

        _utilityHandler.AddUtility(_substitutionUtility);
        _utilityHandler.AddUtility(_handSignUtility);

        // todo implement factory helper
        // if we populate a map of the utilities, or have a ninja state on them, we can pass the utilitys in
        // _stateController = DerivedAIStateFactory.CreateNinja(OnDefaultRestored, _shadowStepUtility, _substitutionUtility, _fakeoutBallUtility,
        //     _handSignUtility);

        _stateController = new DerivedAIStateController<NinjaState>();

        _stateController.onDefaultRestored += OnDefaultRestored;

        _stateController.AddState(NinjaState.SmokeBomb,
            DerivedAIStateFactory.CreateState(this, _stateController, NinjaState.SmokeBomb, smokeBombUtilityArgs));
        _stateController.AddState(NinjaState.Default,
            DerivedAIStateFactory.CreateState(this, _stateController, NinjaState.Default, default));
        _stateController.AddState(NinjaState.Substitution,
            DerivedAIStateFactory.CreateState(this, _stateController, NinjaState.Substitution,
                substitutionUtilityArgs));
        _stateController.AddState(NinjaState.ShadowStep,
            DerivedAIStateFactory.CreateState(this, _stateController, NinjaState.ShadowStep, shadowStepArgs));
        _stateController.AddState(NinjaState.HandSign,
            DerivedAIStateFactory.CreateState(this, _stateController, NinjaState.HandSign, handSignUtilityArgs));
        _stateController.AddState(NinjaState.FakeOut,
            DerivedAIStateFactory.CreateState(this, _stateController, NinjaState.FakeOut, fakeoutUtilityArgs));
        _stateController.Initialize(NinjaState.Default);
        hasSpecials = true;
        
    }

    internal void SmokeBomb(Vector3 newPos)
    {
        currentSmokeBombPosition = newPos;
        _stateController.ChangeState(NinjaState.SmokeBomb);
    }

    private void OnDefaultRestored(NinjaState obj)
    {
        if (obj != NinjaState.Default)
            Debug.LogError("Default state was not initialized properly.");

        currentState = AIState.Move;
    }


    protected override void Update()
    {
        _gizmo.gizmoText = _stateController.State.ToString();
        if (currentState == AIState.Special)
        {
            var currentUtil = _utilityHandler.GetCurrentUtility();
            if (currentUtil.State != AIState.Special)
            {
                Debug.LogError("Out of special utility but current state is special");
                return;
            }

            if (_stateController.State == NinjaState.Default)
            {
                if (currentUtil == _handSignUtility) _stateController.ChangeState(NinjaState.HandSign);
                else if (currentUtil == _fakeoutBallUtility) _stateController.ChangeState(NinjaState.FakeOut);
            }
        }

        _stateController.UpdateState();
        base.Update();
    }

    internal override void SetOutOfPlay(bool value)
    {
        if (stayIdle || _stateController.State == NinjaState.Substitution ||
            _stateController.State == NinjaState.HandSign ||
            _stateController.State == NinjaState.ShadowStep)
            return;

        base.SetOutOfPlay(value);
        currentState = AIState.OutOfPlay;
        _stateController.ChangeState(NinjaState.Default);
    }

    private void OnTriggerEnter(Collider other)
    {
        _stateController.OnTriggerEnter(other);
    }

    private void OnDestroy()
    {
        _handSignUtility.Dispose();
    }

    protected override void HandlePhaseChange()
    {
        if (!_substitutionUtility.inSequence && !_shadowStepUtility._shadowSteppingSequencePlaying)
        {
            if (hasBall) ThrowBall();
            else currentState = AIState.Move;
            base.HandlePhaseChange();
            _phaseDelay = Time.time + Random.Range(1, 1.6f);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // if (other.gameObject.layer == LayerMask.NameToLayer("Ball"))
        // _substitutionUtility.ballInTrigger = false;
    }

    private bool entryFinished = false;
    public bool fakeOut;

    public override void ReturnControl()
    {
        _phaseDelay = float.MaxValue;
        _shadowStepUtility.ShadowStepOutroOverride().Forget();
        outOfPlay = false;
        if (hasBall) ThrowBall();
        entryFinished = false;
    }

    public override void PossessAI()
    {
        if (_shadowStepUtility._shadowSteppingSequencePlaying ||
            _substitutionUtility.inSequence ||
            _fakeoutBallUtility.active)
        {
            return;
        }

        _shadowStepUtility._shadowSteppingSequencePlaying = false;
        _substitutionUtility.args.sequencePlaying = false;
        _fakeoutBallUtility.active = false;
        if (entryFinished) return;
        if (Time.time <= _phaseDelay)
        {
            if (hasBall) ThrowBall();
            _moveUtility.FlockMove(this);
        }
        else if (!outOfPlay)
        {
            _shadowStepUtility.ShadowStepIntroOverride();
            _shadowStepUtility._shadowSteppingSequencePlaying = true;
            entryFinished = true;
        }
        else
        {
            stayIdle = false;
        }
    }

    public void PrepareSmokeBomb()
    {
        stayIdle = true;
    }

    public void EndSmokeBomb()
    {
        _stateController.ChangeState(NinjaState.Default);
    }
}