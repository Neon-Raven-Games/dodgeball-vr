using UnityEngine;

public class GrabbingHandState : BaseHandState
{
    protected internal override HandState State => HandState.Grabbing;

    private GameObject Ball
    {
        get => handController.ball;
        set => handController.ball = value;
    }

    public GrabbingHandState(HandStateController handController) : base(handController)
    {
    }

    public override void OnStateEnter()
    {
        base.OnStateEnter();
        var ball = Ball.GetComponent<DodgeBall>();
        if (!ball) Debug.LogError("Dodgeball not found in grabbing hand state");

        ball.Grab(handController.actor, handController.gameObject);
        ball.transform.position = grabTransform.position;
        ball.transform.rotation = grabTransform.rotation;
        triggerCollider.enabled = false;
        _working = true;
    }

    public override void OnStateExit()
    {
        _working = false;
    }

    private bool _working;

    public override void OnGrabRelease()
    {
        _working = false;
        base.OnGrabRelease();
        ChangeState(HandState.Throwing);
    }

    public override void OnStateLateUpdate()
    {
        if (!_working) return;
        Ball.transform.position = grabTransform.position;
        Ball.transform.rotation = grabTransform.rotation;
    }

    public override void OnStateStart()
    {
    }

    public override void OnStateAwake()
    {
    }


    public override void OnStateUpdate()
    {
        if (handController.actor.IsOutOfPlay()) ChangeState(HandState.Throwing);
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