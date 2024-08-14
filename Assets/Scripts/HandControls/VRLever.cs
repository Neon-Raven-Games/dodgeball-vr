using System;
using UnityEngine;

public class VRLever : MonoBehaviour
{
    [SerializeField] private float snapRange = 0.5f;
    [SerializeField] private float openAngle = 1.5f;
    [SerializeField] private float closedAngle = 35f;
    [SerializeField] private float rotationSpeed = 2f;

    [SerializeField] private Transform leftHandSnapPoint;
    [SerializeField] private Transform rightHandSnapPoint;
    [SerializeField] private Transform leverPivot;
    
    private Transform _leftHand;
    private Transform _rightHand;
    private Vector3 _leftHandOriginalPosition;
    private Vector3 _rightHandOriginalPosition;
    private bool _isLeftHandSnapped;
    private bool _isRightHandSnapped;

    private void Update()
    {
        if (_isLeftHandSnapped)
        {
            _leftHand.position = leftHandSnapPoint.position;
            _leftHand.rotation = leftHandSnapPoint.rotation;
            RotateLever(_leftHandParent, _leftHandOriginalPosition);
        }
        if (_isRightHandSnapped)
        {
            _rightHand.position = rightHandSnapPoint.position;
            _rightHand.rotation = rightHandSnapPoint.rotation;
            RotateLever(_rightHandParent, _rightHandOriginalPosition);
        }
    }

    private void RotateLever(Transform hand, Vector3 originalPosition)
    {
        var distance = Vector3.Distance(hand.position, originalPosition);
        var lerpFactor = Mathf.Clamp01(distance / snapRange);

        var targetAngle = Mathf.Lerp(openAngle, closedAngle, lerpFactor);

        var step = rotationSpeed * Time.deltaTime;
        var currentAngle = Mathf.MoveTowardsAngle(transform.localEulerAngles.z, targetAngle, step);
        leverPivot.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, currentAngle);

        // Release the hand if the lever reaches the target angle
        if (Math.Abs(currentAngle - targetAngle) < 0.1f)
        {
            if (hand == _leftHand)
            {
                ReleaseLeftHand();
            }
            else if (hand == _rightHand)
            {
                ReleaseRightHand();
            }
        }
    }

    private Transform _leftHandParent;
    private Transform _rightHandParent;

    public void SnapLeftHand(Transform hand)
    {
        if (!_isLeftHandSnapped && Vector3.Distance(hand.position, transform.position) <= snapRange)
        {
            _leftHand = hand;
            _leftHand.position = leftHandSnapPoint.position;
            _leftHand.rotation = leftHandSnapPoint.rotation;
            _leftHandParent = _leftHand.parent;
            _leftHand.parent = leftHandSnapPoint;
            _leftHandOriginalPosition = hand.position;
            _isLeftHandSnapped = true;
        }
    }

    public void SnapRightHand(Transform hand)
    {
        if (!_isRightHandSnapped && Vector3.Distance(hand.position, transform.position) <= snapRange)
        {
            _rightHand = hand;
            
            _rightHand.position = rightHandSnapPoint.position;
            _rightHand.rotation = rightHandSnapPoint.rotation;
            _rightHandParent = _rightHand.parent;
            _rightHand.parent = rightHandSnapPoint;
            
            _rightHandOriginalPosition = hand.position;
            _isRightHandSnapped = true;
        }
    }

    public void ReleaseLeftHand()
    {
        if (_isLeftHandSnapped)
        {
            _isLeftHandSnapped = false;
            _leftHand.transform.position = _leftHandOriginalPosition;
            _leftHand.transform.rotation = Quaternion.identity;
            _leftHand.SetParent(_leftHandParent);
            _leftHand = null;
        }
    }

    public void ReleaseRightHand()
    {
        if (_isRightHandSnapped)
        {
            _isRightHandSnapped = false;
            _rightHand.transform.position = _rightHandOriginalPosition;
            _rightHand.transform.rotation = Quaternion.identity;
            _rightHand.SetParent(_rightHandParent);
            _rightHand = null;
        }
    }
}