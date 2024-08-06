using UnityEngine;

public class IdleHandState : BaseHandState
{
    protected internal override HandState State => HandState.Idle;

    public IdleHandState(HandStateController handController) : base(handController)
    {
    }

    private int teamOneLayer = LayerMask.NameToLayer("TeamOne");
    public override void OnStateEnter()
    {
        Physics.IgnoreLayerCollision(teamOneLayer, ballLayer, false);
        base.OnStateEnter();
    }


    public override void OnStateExit()
    {
    }

    public override void OnGrab()
    {
        base.OnGrab();
        ChangeState(HandState.Sucking);
    }

    public override void OnStateStart()
    {
    }

    public override void OnStateAwake()
    {
    }

    public override void OnStateUpdate()
    {
    }

    public override void OnStateLateUpdate()
    {
    }

    public override void OnTriggerEnter(Collider other)
    {
    }

    public override void OnTriggerStay(Collider other)
    {
    }

    public override void OnTriggerExit(Collider other)
    {
    }
}