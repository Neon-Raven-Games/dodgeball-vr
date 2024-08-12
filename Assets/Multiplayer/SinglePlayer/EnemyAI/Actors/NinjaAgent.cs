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
        if (_handSignUtility.Roll(this) > 0)
            _handSignUtility.Execute(this);
    }

    protected override void HandleSpecial()
    {
        base.HandleSpecial();
        Debug.Log("Special State");
        var execute = _shadowStepUtility.Execute(this);
        if (execute > 0) currentState = AIState.Special;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ball"))
        {
            _shadowStepUtility.ballInTrigger = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ball") && _shadowStepUtility.ballInTrigger)
            _shadowStepUtility.ballInTrigger = false;
    }
}