public class IdleState : BaseHandCanonState
{
    public IdleState(HandCannon handCannon) : base(handCannon)
    {
    }

    public override void EnterState()
    {
        base.EnterState();
        handCannon.trajectoryLineRenderer.enabled = handCannon.trajectoryAssist;
    }

    public override void GripAction()
    {
        base.GripAction();
        handCannon.ChangeState(CannonState.Sucking);
    }

    public override void FireAction()
    {
        base.FireAction();
        if (handCannon.dodgeBallAmmo.Count > 0)
            handCannon.animator.Play("Fire");
    }
}