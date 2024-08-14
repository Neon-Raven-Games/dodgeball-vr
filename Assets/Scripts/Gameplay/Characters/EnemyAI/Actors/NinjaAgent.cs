using Hands.SinglePlayer.EnemyAI;
using Multiplayer.SinglePlayer.EnemyAI.Utilities;
using UnityEngine;

public class NinjaAgent : DodgeballAI
{
    [SerializeField] private ShadowStepUtilityArgs shadowStepArgs;
    [SerializeField] private NinjaHandSignUtilityArgs handSignUtilityArgs;
    private ShadowStepUtility _shadowStepUtility;
    private NinjaHandSignUtility _handSignUtility;

    protected override void PopulateUtilities()
    {
        base.PopulateUtilities();
        _shadowStepUtility = new ShadowStepUtility(shadowStepArgs, AIState.Special, this);
        _shadowStepUtility.Initialize(friendlyTeam.playArea, team);
        
        _handSignUtility = new NinjaHandSignUtility(handSignUtilityArgs, this);
        _handSignUtility.Initialize(friendlyTeam.playArea, team);
        
        _utilityHandler.AddUtility(_shadowStepUtility);
        _utilityHandler.AddUtility(_handSignUtility);
    }

    protected override void Update()
    {
        base.Update();
        if (IsOutOfPlay() || currentState == AIState.PickUp) return;
        
        if (_handSignUtility.Roll(this) > 0)
            _handSignUtility.Execute(this);
    }

    internal override void SetOutOfPlay(bool value)
    {
        if (currentState == AIState.Special) return;
        base.SetOutOfPlay(value);
    }

    protected override void HandleSpecial()
    {
        if (IsOutOfPlay() || currentState == AIState.PickUp) return;
        
        var execute = _shadowStepUtility.Roll(this);
        if (execute > 0) _shadowStepUtility.Execute(this);
        else if (!_shadowStepUtility._shadowSteppingSequencePlaying) _moveUtility.Execute(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ball"))
        {
            _shadowStepUtility.ballInTrigger = true;
            _shadowStepUtility.Execute(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ball"))
            _shadowStepUtility.ballInTrigger = false;
    }

    public void InitialShadowStepFinished()
    {
        _shadowStepUtility.InitialShadowStepFinished();
    }
}