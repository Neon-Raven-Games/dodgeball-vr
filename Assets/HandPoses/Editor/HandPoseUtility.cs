using UnityEngine;

public static class HandPoseUtility
{
    public static void MirrorLeftToRight(Transform[] leftHandTransforms, Transform[] rightHandTransforms)
    {
        if (leftHandTransforms.Length != rightHandTransforms.Length)
        {
            Debug.LogError("Left and Right hand hierarchies do not match!");
            return;
        }

        for (int i = 0; i < leftHandTransforms.Length; i++)
        {
            Transform leftTransform = leftHandTransforms[i];
            Transform rightTransform = rightHandTransforms[i];

            rightTransform.localPosition = MirrorPosition(leftTransform.localPosition);
            rightTransform.localRotation = MirrorRotation(leftTransform.localRotation);
        }
    }

    private static Vector3 MirrorPosition(Vector3 position)
    {
        return new Vector3(-position.x, position.y, position.z);
    }

    private static Quaternion MirrorRotation(Quaternion rotation)
    {
        return new Quaternion(-rotation.x, rotation.y, rotation.z, -rotation.w);
    }
}