
public class IdleState : BaseHandCanonState
{
    public IdleState(HandCannon handCannon) : base(handCannon)
    {
    }
    
    public override void EnterState()
    {
        base.EnterState();
        handCannon.trajectoryLineRenderer.enabled = true;
    }
    
    public override void Update()
    {
        base.Update();
    }
    
    public override void GripAction()
    {
        base.GripAction();
        handCannon.ChangeState(CannonState.Sucking);
    }
    
    public override void FireAction()
    {
        base.FireAction();
        handCannon.ChangeState(CannonState.Shooting);
    }
    
    public override void FireReleaseAction()
    {
        base.FireReleaseAction();
    }
    
    public override void GripReleaseAction()
    {
        base.GripReleaseAction();
    }
}