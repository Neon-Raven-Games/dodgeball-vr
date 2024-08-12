using Hands.SinglePlayer.EnemyAI;
using UnityEngine;

public class NinjaAgent : DodgeballAI
{
    [SerializeField] private ShadowStepUtilityArgs shadowStepArgs;
   protected override void PopulateUtilities()
   {
       base.PopulateUtilities();
       _utilityHandler.AddUtility(new ShadowStepUtility(shadowStepArgs, AIState.Special));
    }
}
