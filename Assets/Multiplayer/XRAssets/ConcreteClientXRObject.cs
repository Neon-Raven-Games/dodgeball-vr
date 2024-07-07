using CloudFine.ThrowLab.UnityXR;
using Unity.Template.VR.Multiplayer;

using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine;

public class ConcreteClientXRObject : IClientXRObject
{
    public ClientXRObjectType objectType;

    public void InitializeClient(GameObject go)
    {
        InitializeClientObject(go, objectType);
    }

    public void InitializeClientObject(GameObject go, ClientXRObjectType type)
    {
        // switch
        if (type == ClientXRObjectType.Ball)
        {
            var throwHandle = go.AddComponent<ThrowHandle>();
            throwHandle.SetConfigSet(new ThrowConfigurationSet(1));
            
            // todo, we need to get a throw config
            var config = ScriptableObject.CreateInstance<ThrowConfiguration>();
            throwHandle.SetConfigForDevice(Device.UNSPECIFIED, config);
            
            var interactable = go.AddComponent<ThrowLabXRGrabInteractable>();
            interactable.movementType = XRBaseInteractable.MovementType.Kinematic;
            interactable.smoothPosition = true;
            interactable.smoothPositionAmount = 5;
            interactable.tightenPosition = 0.5f;
            interactable.trackScale = false;
        }
    }
}
