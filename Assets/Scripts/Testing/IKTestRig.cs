using UnityEngine;

[RequireComponent(typeof(Animator))]
public class IKTestRig : MonoBehaviour
{
    public Transform leftHandTarget;
    public Transform rightHandTarget;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator)
        {
            // Update IK for the left hand
            if (leftHandTarget != null)
            {
                animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandTarget.position);
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
                animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandTarget.rotation);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);
            }

            // Update IK for the right hand
            if (rightHandTarget != null)
            {
                animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandTarget.position);
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
                animator.SetIKRotation(AvatarIKGoal.RightHand, rightHandTarget.rotation);
                animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);
            }
        }
    }
}