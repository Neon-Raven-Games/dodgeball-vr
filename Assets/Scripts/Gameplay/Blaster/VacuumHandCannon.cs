using UnityEngine;
using System.Collections.Generic;

public class VacuumHandCannon : MonoBehaviour
{
    [SerializeField] private Transform barrelPoint;
    [SerializeField] private float suctionForce = 10f;
    [SerializeField] private float swirlRadius = 1f;
    [SerializeField] private float swirlSpeed = 2f;
    [SerializeField] private float suctionCooldown = 1f;
    [SerializeField] private float suctionDuration = 2f; // Added suction duration
    [SerializeField] private float ballEndScale = 0.4f;
    
    [SerializeField] private bool gripping;
    
    private readonly List<Collider> _ballsToRemove = new();
    private readonly List<Collider> _ballsInRange = new();

    private CooldownTimer _suctionCooldownTimer;
    private float _nextSuctionTime;
    private float _lastSuctionTime;
    private float _suctionEndTime;
    private bool _sucking;

    private void Start()
    {
        _suctionCooldownTimer = GetComponent<CooldownTimer>();
        _suctionCooldownTimer.SetCooldownTime(suctionCooldown);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ball"))
        {
            _ballsInRange.Add(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!_ballsInRange.Contains(other) || other.gameObject.layer != LayerMask.NameToLayer("Ball")) return;
        if (other.GetComponent<DodgeBall>()._ballState == BallState.Possessed)
            _ballsInRange.Remove(other);
    }

    private void Update()
    {
        if (_sucking && Time.time >= _suctionEndTime)
        {
            _sucking = false;
            _suctionCooldownTimer.StartCooldown();
        }
        else if (gripping && _suctionCooldownTimer.IsAvailable())
        {
            if (!_sucking)
            {
                _suctionEndTime = Time.time + suctionDuration;
            }

            _sucking = true;
            ApplySuction();
        }
        else
        {
            _ballsInRange.Clear();
            _ballsToRemove.Clear();
        }
    }

    private void ApplySuction()
    {
        // Process each ball in range and apply the suction effect
        foreach (Collider ball in _ballsInRange)
        {
            var ballScript = ball.GetComponent<DodgeBall>();
            if (ballScript && ballScript._ballState == BallState.Dead)
            {
                Rigidbody rb = ball.GetComponent<Rigidbody>();
                if (rb)
                {
                    rb.isKinematic = true;
                    var distanceToBarrel = Vector3.Distance(ball.transform.position, barrelPoint.position);

                    var currentRadius = Mathf.Lerp(swirlRadius, 0f, 1f - (distanceToBarrel / 10f));
                    var pivotPoint = barrelPoint.position + barrelPoint.forward * distanceToBarrel;
                    pivotPoint += (pivotPoint - ball.transform.position) *
                                  (currentRadius / distanceToBarrel * swirlRadius);

                    var angle = Time.fixedDeltaTime * swirlSpeed * 180f;
                    ball.transform.RotateAround(pivotPoint, barrelPoint.forward, angle);

                    var directionToBarrel = (barrelPoint.position - ball.transform.position).normalized;
                    var targetPosition = ball.transform.position + directionToBarrel * suctionForce * Time.deltaTime;

                    ball.transform.position = targetPosition;

                    ball.transform.localScale = Vector3.Lerp(ball.transform.localScale, Vector3.one * ballEndScale,
                        Time.deltaTime * (suctionForce / distanceToBarrel));

                    if (distanceToBarrel < 0.1f)
                    {
                        rb.isKinematic = false;
                        rb.velocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;

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


#if UNITY_EDITOR
private void OnDrawGizmos()
{
    if (barrelPoint && _suctionCooldownTimer)
    {
        if (_suctionCooldownTimer.IsAvailable() && !_suctionCooldownTimer.IsOnCooldown())
            Gizmos.color = Color.green;
        else
            Gizmos.color = Color.red;
        
        Gizmos.DrawWireSphere(barrelPoint.position, .2f);
    }
}
#endif
}