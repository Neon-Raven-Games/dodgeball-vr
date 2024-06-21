using UnityEngine;

public class NetIKTargetHelper : MonoBehaviour
{
    private Transform _leftHandTransform;
    private Transform _rightHandTransform;
    private Animator _animator;
    private bool _initialized;
    private void SetLeftIKTarget(Transform leftHandTransform)
    {
        if (!_initialized) return;
        _animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandTransform.position);
        _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
        
        _animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandTransform.rotation);
        _animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);
    }

    private void SetRightIKTarget(Transform rightHandTransform)
    {
        if (!_initialized) return;
        _animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandTransform.position);
        _animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
        
        _animator.SetIKRotation(AvatarIKGoal.RightHand, rightHandTransform.rotation);
        _animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);
    }

    public void AssignIKTargets(Transform leftHandTransform, Transform rightHandTransform)
    {
        _animator = GetComponent<Animator>();
        _leftHandTransform = leftHandTransform;
        _rightHandTransform = rightHandTransform;
        _initialized = true;
        Debug.Log("Calling Init");
    }

    private void OnAnimatorIK(int layerIndex)
    {
        SetLeftIKTarget(_leftHandTransform);
        SetRightIKTarget(_rightHandTransform);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
