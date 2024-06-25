using Fusion;
using UnityEngine;

namespace Unity.Template.VR.Multiplayer.Players
{
    public struct IKInput : INetworkInput
    {
        public Vector3 leftHandPosition;
        public Quaternion leftHandRotation;
        
        public Vector3 rightHandPosition;
        public Quaternion rightHandRotation;
        public Vector3 hmdPosition;
        public Quaternion hmdRotation;
        
        public Vector2 axis;
    }
}