using Hands.SinglePlayer.EnemyAI;

public class ShadowStepUtility : Utility<ShadowStepUtilityArgs>, IUtility
{

    public ShadowStepUtility(ShadowStepUtilityArgs args, DodgeballAI.AIState state) : base(args, state)
    {
    }

    public override float Execute(DodgeballAI ai)
    {
        return 0f;
    }

    public override float Roll(DodgeballAI ai)
    {
        return 0f;
    }
}
