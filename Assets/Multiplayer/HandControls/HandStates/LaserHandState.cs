using UnityEngine;

public class LaserHandState : BaseHandState
{
    protected internal override HandState State => HandState.Laser;

    public LaserHandState(HandStateController handController) : base(handController)
    {
    }

    public override void OnStateStart()
    {
        handController.laserSetup.gameObject.SetActive(false);
    }

    public override void OnStateAwake()
    {
    }

    public override void OnStateExit()
    {
        handController.laserSetup.gameObject.SetActive(false);
    }

    public override void OnStateEnter()
    {
        handController.ui = true;
        base.OnStateEnter();
        handController.laserSetup.gameObject.SetActive(true);
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

    public override void OnGrab()
    {
        Debug.Log("Laser grab");
    }
}
