using UnityEngine;

public class ThrowingHandState : BaseHandState
{
    protected internal override HandState State => HandState.Throwing;
    private float _throwTime = 0.5f;
    private float _throwTimer;
    
    
    private GameObject Ball
    {
        get => handController.ball;
        set => handController.ball = value;
    }

    public ThrowingHandState(HandStateController handController) : base(handController)
    {
    }

    public override void OnStateEnter()
    {
        Ball.GetComponent<DodgeBall>().Throw();
        base.OnStateEnter();
    }

    public override void OnStateExit()
    {
        Ball = null;
        _throwTimer = 0f;
    }
    
    public override void OnStateStart()
    {
    }

    
    public override void OnStateAwake()
    {
    }


    public override void OnStateUpdate()
    {
        if (_throwTimer < handController.throwStateTime)
        {
            _throwTimer += Time.deltaTime;
        }
        else
        {
            ChangeState(HandState.Idle);
        }
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