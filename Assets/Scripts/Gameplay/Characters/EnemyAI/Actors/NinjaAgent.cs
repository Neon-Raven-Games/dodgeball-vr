using System.Collections;
using Hands.SinglePlayer.EnemyAI;
using Hands.SinglePlayer.EnemyAI.StatefulRefactor;
using Multiplayer.SinglePlayer.EnemyAI.Utilities;
using UnityEngine;
using Random = UnityEngine.Random;

public class NinjaAgent : DodgeballAI
{
    [SerializeField] private ShadowStepUtilityArgs shadowStepArgs;
    [SerializeField] private SubstitutionUtilityArgs substitutionUtilityArgs;
    [SerializeField] private NinjaHandSignUtilityArgs handSignUtilityArgs;
    [SerializeField] private FakeoutUtilityArgs fakeoutUtilityArgs;
    [SerializeField] private SmokeBombUtilityArgs smokeBombUtilityArgs;
    [SerializeField] private NinjaOutOfPlayArgs ninjaOutOfPlayUtilityArgs;
    internal Vector3 currentSmokeBombPosition;
    private FakeoutBallUtility _fakeoutBallUtility;
    private SubstitutionUtility _substitutionUtility;
    private ShadowStepUtility _shadowStepUtility;
    private NinjaHandSignUtility _handSignUtility;

    private DerivedAIStateController<NinjaState> _stateController;
    private CharacterGizmo _gizmo;

    internal override void TriggerRespawn()
    {
        gameObject.SetActive(false);
        transform.position = spawnInPos[Random.Range(0, spawnInPos.Count)].position;
        gameObject.SetActive(true);
        SetOutOfPlay(false);
        animator.Rebind();
        stateController.Rebind();
        stateController.SubscribeRolling();
    }

    protected override void CreateStateController()
    {
        _gizmo = GetComponent<CharacterGizmo>();
        stateController = AIStateMachineFactory.CreateStateMachine(this,
            moveUtilityArgs, pickUpUtilityArgs, throwUtilityArgs,
            substitutionUtilityArgs, shadowStepArgs, fakeoutUtilityArgs, ninjaOutOfPlayUtilityArgs);
        _ninjaStateController = stateController as NinjaStateController;
        _ninjaStateController.SetNinja(handSignUtilityArgs, fakeoutUtilityArgs, smokeBombUtilityArgs);
    }

    protected void Nothing()
    {
        return;
        // utilityHandler = UtilityHandler.Create(this, moveUtilityArgs, 
        //     pickUpUtilityArgs, throwUtilityArgs, outOfPlayUtilityArgs,
        //     substitutionUtilityArgs, handSignUtilityArgs, shadowStepArgs);

        // we will move this logic to the controller.
        // the controller will override states if handsign active
        _handSignUtility = new NinjaHandSignUtility(handSignUtilityArgs, this);
        _handSignUtility.Initialize(friendlyTeam.playArea, team);

        // this will be a utility
        _fakeoutBallUtility = new FakeoutBallUtility(fakeoutUtilityArgs, AIState.Special, this);
        _fakeoutBallUtility.Initialize(friendlyTeam.playArea, team);
        // todo, implement phase manager
        // GameManager.onPhaseChange += NinjaPhase;
    }

    private IEnumerator ReturnFromSmokeBomb()
    {
        yield return new WaitForSeconds(Random.Range(1f, 1.7f));
        shadowStepArgs.entryEffect.SetActive(false);
        shadowStepArgs.entryEffect.SetActive(true);
        aiAvatar.SetActive(true);
        var duration = 1.4f;
        var curTime = 0f;

        var rand = Random.insideUnitCircle * 2;
        var targetPos = new Vector3(transform.position.x + rand.x, transform.position.y, transform.position.z + rand.y);

        while (curTime < duration)
        {
            transform.position = Vector3.Lerp(transform.position, targetPos, curTime / duration);
            curTime += Time.deltaTime;
            yield return null;
        }

        _stateController.ChangeState(NinjaState.Default);
        if (outOfPlay) SetOutOfPlay(false);
        lock (this)
        {
            stayIdle = false;
            aiAvatar.SetActive(true);
        }
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

        // _stateController.ChangeState(StateStruct.Move);
    }


    // protected override void Update()
    // {
    //     if (stayIdle) return;
    //     if (_stateController.State == NinjaState.SmokeBomb) return;
    //     _gizmo.gizmoText = _stateController.State.ToString();
    //     if (currentState == "Special")
    //     {
    //         var currentUtil = utilityHandler.GetCurrentUtility();
    //         // if (currentUtil.State != AIState.Special)
    //         // {
    //             // Debug.LogError("Out of special utility but current state is special");
    //             // return;
    //         // }
    //
    //         if (_stateController.State == NinjaState.Default)
    //         {
    //             if (currentUtil == _handSignUtility) _stateController.ChangeState(NinjaState.HandSign);
    //             else if (currentUtil == _fakeoutBallUtility) _stateController.ChangeState(NinjaState.FakeOut);
    //         }
    //     }
    //
    //     _stateController.UpdateState();
    //     base.Update();
    // }

    private NinjaStateController _ninjaStateController;

    internal override void SetOutOfPlay(bool value)
    {
        if (value && _ninjaStateController.IsHandSigning()) return;
        if (_ninjaStateController.State == NinjaStruct.Substitution ||
            _ninjaStateController.State == NinjaStruct.ShadowStep)
            return;
        base.SetOutOfPlay(value);
    }

    protected void SpawnIn()
    {
        // todo, out of play for ninja
        var jump = spawnInPos[Random.Range(0, spawnInPos.Count)].GetComponent<NinjaJump>();
        jump.QueueJump(transform);
    }

    // private void OnTriggerEnter(Collider other)
    // {
    //     _stateController.OnTriggerEnter(other);
    // }


    private bool entryFinished = false;

    public void PrepareSmokeBomb()
    {
        stayIdle = true;
        _stateController.ChangeState(NinjaState.SmokeBomb);
    }


    public void EndSmokeBomb()
    {
        stayIdle = false;
        StartCoroutine(ReturnFromSmokeBomb());
    }
}