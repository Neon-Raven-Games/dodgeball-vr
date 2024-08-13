using UnityEngine;

public abstract class BaseHandState
{
    protected internal abstract HandState State { get; }
    protected readonly Transform grabTransform;
    protected readonly Collider triggerCollider;
    protected readonly int ballLayer = LayerMask.NameToLayer("Ball");
    protected readonly HandStateController handController;
    private static readonly int _SState = Animator.StringToHash("State");
    private readonly Animator _animator;

    protected BaseHandState(HandStateController handController)
    {
        _animator = handController.animator;
        this.handController = handController;
        grabTransform = handController.grabTransform;
        triggerCollider = handController.grabTriggerCollider;
        triggerCollider.enabled = false;
    }

    protected void ChangeState(HandState newState) =>
        handController.ChangeState(newState);

    public abstract void OnStateStart();
    public abstract void OnStateAwake();

    public virtual void OnStateEnter()
    {
        _animator.SetInteger(_SState, (int) State);
    }

    public abstract void OnStateExit();
    public abstract void OnStateUpdate();
    public abstract void OnStateLateUpdate();
    public abstract void OnTriggerEnter(Collider other);
    public abstract void OnTriggerStay(Collider other);
    public abstract void OnTriggerExit(Collider other);

    public virtual void OnGrab(){}
    public virtual void OnGrabRelease(){}

    public virtual void OnUITrigger()
    {
    }
    public virtual void OnUITriggerRelease(){}

    public virtual void MenuButton()
    {
        // ChangeState(State == HandState.Laser ? HandState.Idle : HandState.Laser);
        
    }

}