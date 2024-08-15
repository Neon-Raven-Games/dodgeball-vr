using System.Linq;
using UnityEngine;

public class ShootingState : BaseHandCanonState
{
    public ShootingState(HandCannon handCannon) : base(handCannon)
    {
    }
    
    public override void EnterState()
    {
        base.EnterState();
        handCannon.trajectoryLineRenderer.enabled = true;
        if (handCannon.dodgeBallAmmo.Count > 0)
        {
            LaunchDodgeball(handCannon.dodgeBallAmmo.FirstOrDefault());
            handCannon.dodgeBallAmmo.RemoveAt(0);
        }
        
        handCannon.ChangeState(CannonState.Idle);
    }
    
    public void LaunchDodgeball(DodgeBall dodgeball)
    {
        dodgeball.transform.position = handCannon.barrelTransform.position;
        dodgeball._team = Team.TeamOne;
        dodgeball.SetLiveBall();
        Rigidbody rb = dodgeball.GetComponent<Rigidbody>();
        rb.velocity = Vector3.zero;
        dodgeball.gameObject.SetActive(true);
        rb.AddForce(handCannon.barrelTransform.forward * handCannon.launchForce, ForceMode.Impulse);
    }
    
    
}
