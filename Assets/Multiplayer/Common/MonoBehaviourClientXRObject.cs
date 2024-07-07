using UnityEngine;

namespace Unity.Template.VR.Multiplayer.Server
{
    // Local player character:
    // base character
        // XR Origin 
        // DevController
    // Main Camera
        // TrackedPoseDriver
    // Hands
        // XRController
        // XR DirectInteractor
        // Unity XR_DeviceDetector
        // Unity XR_GrabThresholdModifier
        // HandController (we could probably just exclude the throw handle for quick fix)
    public class MonoBehaviourClientXRObject : MonoBehaviour, IClientXRObject
    {
        public ClientXRObjectType objectType;

        public void InitializeClient(GameObject go)
        {
            InitializeClientObject(go, objectType);
        }
#if !UNITY_SERVER
        public void InitializeClientObject(GameObject go, ClientXRObjectType type)
        {
            // switch
        }
#else
        public void InitializeClientObject(GameObject go, ClientXRObjectType type)
        {
            // nothing probably needs to be done here
        }
#endif
    }
}