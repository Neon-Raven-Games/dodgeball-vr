using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SuckingState : BaseHandCanonState
{
    private readonly List<Collider> _ballsToRemove = new();
    private readonly List<Collider> _ballsInRange = new();
    private readonly CooldownTimer _suctionCooldownTimer;

    private float _nextSuctionTime;
    private float _lastSuctionTime;
    private float _suctionEndTime;

    public SuckingState(HandCannon handCannon) : base(handCannon)
    {
        _suctionCooldownTimer = handCannon.AddComponent<CooldownTimer>();
        _suctionCooldownTimer.SetCooldownTime(handCannon.suctionCooldown);
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

#if UNITY_EDITOR
    public override void OnDrawGizmos()
    {
        if (handCannon.barrelTransform && _suctionCooldownTimer)
        {
            if (_suctionCooldownTimer.IsAvailable() && !_suctionCooldownTimer.IsOnCooldown())
                Gizmos.color = Color.green;
            else
                Gizmos.color = Color.red;
        
            Gizmos.DrawWireSphere(handCannon.barrelTransform.position, .2f);
        }
    }
#endif

    public override void EnterState()
    {
        base.EnterState();
        if (_suctionCooldownTimer.IsOnCooldown() || !_suctionCooldownTimer.IsAvailable())
        {
            ChangeState(CannonState.Idle);
            return;
        }

        handCannon.trajectoryLineRenderer.enabled = false;

        if (_suctionCooldownTimer.IsAvailable() && _suctionEndTime == 0)
            _suctionEndTime = Time.time + handCannon.suctionDuration;
    }


    public override void Update()
    {
        if (Time.time >= _suctionEndTime && _suctionEndTime != 0)
        {
            _suctionEndTime = 0;

            _suctionCooldownTimer.StartCooldown();
            
            _ballsInRange.ForEach(x =>
            {
                x.transform.localScale = Vector3.one;
                x.GetComponent<Rigidbody>().isKinematic = false;
            });
            
            _ballsInRange.Clear();
            _ballsToRemove.Clear();

            ChangeState(CannonState.Idle);
        }
        else
        {
            ApplySuction();
        }
    }

    private void ApplySuction()
    {
        foreach (Collider ball in _ballsInRange)
        {
            var ballScript = ball.GetComponent<DodgeBall>();
            if (ballScript && ballScript._ballState == BallState.Dead)
            {
                Rigidbody rb = ball.GetComponent<Rigidbody>();
                if (rb)
                {
                    // even though this rigid body is kinematic, can we make sure it
                    // doesn't clip through collisions?
                    rb.isKinematic = true;
                    var distanceToBarrel =
                        Vector3.Distance(ball.transform.position, handCannon.barrelTransform.position);

                    var currentRadius = Mathf.Lerp(handCannon.swirlRadius, 0f, 1f - (distanceToBarrel / 10f));
                    var pivotPoint = handCannon.barrelTransform.position +
                                     handCannon.barrelTransform.forward * distanceToBarrel;
                    pivotPoint += (pivotPoint - ball.transform.position) *
                                  (currentRadius / distanceToBarrel * handCannon.swirlRadius);

                    var angle = Time.fixedDeltaTime * handCannon.swirlSpeed * 180f;
                    ball.transform.RotateAround(pivotPoint, handCannon.barrelTransform.forward, angle);

                    var directionToBarrel = (handCannon.barrelTransform.position - ball.transform.position).normalized;
                    var targetPosition = ball.transform.position +
                                         directionToBarrel * handCannon.suctionForce * Time.deltaTime;

                    var ballRadius = ball.bounds.extents.magnitude; // Adjust this to match the ball's radius
                    if (!Physics.SphereCast(ball.transform.position, ballRadius, directionToBarrel, out RaycastHit hit,
                            (targetPosition - ball.transform.position).magnitude))
                    {
                        // Only move the ball if the spherecast didn't hit anything
                        ball.transform.position = targetPosition;
                    }
                    else
                    {
                        // Handle the collision (e.g., stop movement, or slide along the hit surface)
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

        _ballsInRange.ForEach(x =>
        {
            x.transform.localScale = Vector3.one;
            x.GetComponent<Rigidbody>().isKinematic = false;
        });
        _ballsInRange.Clear();
        _ballsToRemove.Clear();

        ChangeState(CannonState.Idle);
    }
}