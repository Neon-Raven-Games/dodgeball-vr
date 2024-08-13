using UnityEngine;

public class SuckingHandState : BaseHandState
{
    protected internal override HandState State => HandState.Sucking;
    private float suckingForce => handController.suckingForce;
    private float suckingSnapDistance => handController.suckingSnapDistance;
    private readonly GameObject _fxPrefab;

    private GameObject Ball
    {
        get => handController.ball;
        set => handController.ball = value;
    }

    public SuckingHandState(HandStateController handController) : base(handController)
    {
        _fxPrefab = handController.fxPrefab;
    }

    public override void OnGrabRelease()
    {
        base.OnGrabRelease();
        ChangeState(Ball ? HandState.Throwing : HandState.Idle);
    }

    public override void OnStateStart()
    {
        _fxPrefab.SetActive(false);
    }

    public override void OnStateAwake()
    {
    }

    public override void OnStateEnter()
    {
        base.OnStateEnter();
        _fxPrefab.SetActive(true);
        triggerCollider.enabled = true;
    }

    public override void OnStateExit()
    {
        _fxPrefab.SetActive(false);
    }
    

    // can we simulate force to the ball instead? I want the control so we don't overshoot
    // and make sure it looks good going into the hand, but also take the ball's velocity into account
    // so we can have a really good looking "suck" effect. right now, it seems like the sucking force
    public override void OnStateUpdate()
    {
        if (!Ball) return;
        if (Vector3.Distance(Ball.transform.position, grabTransform.position) > suckingSnapDistance)
        {
            Ball.transform.position = Vector3.Lerp(Ball.transform.position, grabTransform.position,
                Time.fixedDeltaTime * suckingForce);
            Ball.transform.rotation = Quaternion.Lerp(Ball.transform.rotation, grabTransform.rotation,
                Time.fixedDeltaTime * suckingForce);
        }
        else ChangeState(HandState.Grabbing);
    }

    public override void OnStateLateUpdate()
    {
    }

    public override void OnTriggerEnter(Collider other)
    {
        if (!Ball && other.gameObject.layer == ballLayer)
            Ball = other.gameObject;
    }

    public override void OnTriggerStay(Collider other)
    {
        if (!Ball && other.gameObject.layer == ballLayer)
            Ball = other.gameObject;
    }

    public override void OnTriggerExit(Collider other)
    {
        Ball = null;
    }
}