using UnityEngine;

// updates the net players
public class NetIKTargetHelper : MonoBehaviour
{
    [SerializeField] private Transform networkCharacterTarget;
    [SerializeField] private Transform networkHeadTarget;
    [SerializeField] private Transform headObject;

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
        // _leftHandTransform = leftHandTransform;
        // _rightHandTransform = rightHandTransform;
        _initialized = true;
    }

    private void OnAnimatorIK(int layerIndex)
    {
        // SetLeftIKTarget(_leftHandTransform);
        // SetRightIKTarget(_rightHandTransform);
    }

    void Update()
    {
        // if (!_initialized) return;

        // transform.position = networkCharacterTarget.position;
        // transform.rotation = networkCharacterTarget.rotation;
    }

    // if using IK final we can use this to update the body position
    // headObject.position = networkHeadTarget.position;
    
    private void LateUpdate()
    {
        // headObject.rotation = networkHeadTarget.rotation;
    }

    private Vector2 _moveInputAxis;
    private static readonly int _SYAxis = Animator.StringToHash("yAxis");
    private static readonly int _SXAxis = Animator.StringToHash("xAxis");

    public void SetAxis(Vector2 getMoveInput)
    {
        if (getMoveInput == _moveInputAxis) return;
        _animator.SetInteger(_SXAxis, (int)getMoveInput.x);
        _animator.SetInteger(_SYAxis, (int)getMoveInput.y);
    }
}