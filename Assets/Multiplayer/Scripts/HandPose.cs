using UnityEngine;

[System.Serializable]
public class HandPose : ScriptableObject
{
    public TransformData[] fingerTransforms;

    [System.Serializable]
    public struct TransformData
    {
        public Vector3 localPosition;
        public Quaternion localRotation;
    }
}