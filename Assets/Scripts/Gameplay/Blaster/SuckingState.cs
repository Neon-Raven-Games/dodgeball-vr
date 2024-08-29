using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SuckingState : BaseHandCanonState
{
    private readonly List<Collider> _ballsToRemove = new();
    private readonly List<Collider> _ballsInRange = new();

    private float _nextSuctionTime;
    private float _lastSuctionTime;
    private float _suctionEndTime;

    public SuckingState(HandCannon handCannon) : base(handCannon)
    {
    }

    public override void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ball"))
        {
            _ballsInRange.Add(other);
        }
    }

    public override void OnTriggerExit(Collider other)
    {
        if (!_ballsInRange.Contains(other) || other.gameObject.layer != LayerMask.NameToLayer("Ball")) return;
        if (other.GetComponent<DodgeBall>()._ballState == BallState.Possessed)
            _ballsInRange.Remove(other);
    }

    public override void OnTriggerStay(Collider other)
    {
        base.OnTriggerStay(other);
        if (other.gameObject.layer == LayerMask.NameToLayer("Ball") && !_ballsInRange.Contains(other))
        {
            _ballsInRange.Add(other);
        }
    }

    public override void Update()
    { 
        ApplySuction();
    }

    private void ApplySuction()
    {
        foreach (Collider ball in _ballsInRange)
        {
            var ballScript = ball.GetComponent<DodgeBall>();
            if (ballScript && (ballScript._ballState == BallState.Dead || (ballScript._ballState == BallState.Live && Vector3.Distance(handCannon.barrelTransform.position, ball.transform.position) < handCannon.liveBallRange)))
            {
                Rigidbody rb = ball.GetComponent<Rigidbody>();
                if (rb)
                {
                    rb.isKinematic = true;
                    var distanceToBarrel =
                        Vector3.Distance(ball.transform.position, handCannon.barrelTransform.position);

                    var currentRadius = Mathf.Lerp(handCannon.swirlRadius, 0f, 1f - (distanceToBarrel / 10f));
                    var pivotPoint = handCannon.barrelTransform.position +
                                     handCannon.barrelTransform.forward * distanceToBarrel;
                    pivotPoint += (pivotPoint - ball.transform.position) *
                                  (currentRadius / distanceToBarrel * handCannon.swirlRadius);

                    var angle = Time.deltaTime * handCannon.swirlSpeed * 180f;
                    ball.transform.RotateAround(pivotPoint, handCannon.barrelTransform.forward, angle);

                    var directionToBarrel = (handCannon.barrelTransform.position - ball.transform.position).normalized;
                    var targetPosition = ball.transform.position +
                                         directionToBarrel * handCannon.suctionForce * Time.deltaTime;

                    var ballRadius = ball.bounds.extents.magnitude; // Adjust this to match the ball's radius
                    if (!Physics.SphereCast(ball.transform.position, ballRadius, directionToBarrel, out RaycastHit hit,
                            (targetPosition - ball.transform.position).magnitude))
                    {
                        ball.transform.position = targetPosition;
                    }
                    else
                    {
                        ball.transform.position = hit.point - directionToBarrel * ballRadius;
                    }

                    ball.transform.localScale = Vector3.Lerp(ball.transform.localScale,
                        Vector3.one * handCannon.ballEndScale,
                        Time.deltaTime * (handCannon.suctionForce / distanceToBarrel));

                    if (distanceToBarrel < 0.1f)
                    {
                        rb.isKinematic = false;
                        rb.velocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                        if (ballScript.gameObject.activeInHierarchy)
                            handCannon.AddDodgeBall(ballScript);
                        ball.gameObject.SetActive(false);
                        _ballsToRemove.Add(ball);
                    }
                }
            }
        }

        _ballsToRemove.ForEach(x =>
        {
            {
                x.transform.localScale = Vector3.one;
                _ballsInRange.Remove(x);
            }
        });
        _ballsToRemove.Clear();
    }

    public override void GripReleaseAction()
    {
        base.GripReleaseAction();

        // todo, update visuals to suck all the way up
        _ballsInRange.ForEach(x =>
        {
            if (x.gameObject.activeInHierarchy)
                handCannon.AddDodgeBall(x.gameObject.GetComponent<DodgeBall>());
            x.transform.localScale = Vector3.one;
            x.GetComponent<Rigidbody>().isKinematic = false;
        });
        _ballsInRange.Clear();
        _ballsToRemove.Clear();

        ChangeState(CannonState.Idle);
    }
}