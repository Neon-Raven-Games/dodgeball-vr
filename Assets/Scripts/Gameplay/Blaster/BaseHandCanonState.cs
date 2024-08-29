using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseHandCanonState
{
    protected HandCannon handCannon;

    protected BaseHandCanonState(HandCannon handCannon)
    {
        this.handCannon = handCannon;
    }

    public virtual void EnterState()
    {
    }

    public virtual void ExitState()
    {
    }

    public virtual void Update()
    {
        if (handCannon && handCannon.trajectoryAssist)
            DrawTrajectory();
    }

    protected void ChangeState(CannonState state) => handCannon.ChangeState(state);

    private void DrawTrajectory()
    {
        return;
        Vector3[] points = new Vector3[handCannon.trajectoryPoints];
        Vector3 startPosition = handCannon.barrelTransform.position;
        Vector3 velocity = handCannon.barrelTransform.forward * handCannon.launchForce;

        for (int i = 0; i < handCannon.trajectoryPoints; i++)
        {
            float time = i * 0.1f;
            points[i] = startPosition + velocity * time + Physics.gravity * time * time / 2f;
        }

        // handCannon.trajectoryLineRenderer.positionCount = handCannon.trajectoryPoints;
        // handCannon.trajectoryLineRenderer.SetPositions(points);
    }

    public virtual void GripAction()
    {
    }

    public virtual void GripReleaseAction()
    {
    }

    public virtual void FireAction()
    {
    }

    public virtual void FireReleaseAction()
    {
    }

    public virtual void OnTriggerExit(Collider other)
    {
    }

    public virtual void OnTriggerEnter(Collider other)
    {
    }

    public virtual void OnDrawGizmos()
    {
    }

    public virtual void OnTriggerStay(Collider other)
    {
    }
}