#if !UNITY_SERVER
// using CloudFine.ThrowLab;
// using CloudFine.ThrowLab.UnityXR;
// using UnityEngine.XR.Interaction.Toolkit;
#endif

using System;
using FishNet.Object;
using UnityEngine;

namespace Unity.Template.VR.Multiplayer.Server
{
    public class NetworkClientXRObject : NetworkBehaviour, IClientXRObject
    {
        public ClientXRObjectType objectType;

        public void InitializeClient(GameObject go)
        {
            InitializeClientObject(go, objectType);
        }
#if !UNITY_SERVER
        public void InitializeClientObject(GameObject go, ClientXRObjectType type)
        {
            // todo, somehow we have to instantiate the concrete client xr object
            // on a server build, we don't want the concrete compiled.
            // on the XR client, we want to dynamically add it. They both need to implement this
            // interfact. But the client needs to know it's not a server build.
            // the main issue we are having is referencing the XR library from the server library.
            // we need to create something that the server will not care. 

            // on client loaded, this needs to index the concrete type we are looking for.
            // on our network player is in the main build. the dodgeball will always be there
            // the dodgeball needs to populate components only available to the XR library.
            // lets make a static XR class that will initialize these values. We need a script in the 
            // XR library that acts as a wrapper for our game object to initialize our client.

            // GO has component XR Initializer
            // XR Initializer is in common. 
            // The XR initializer will invoke a method on initialization.
            // if it's a server build, it will do nothing.
            // if it's on XR, our XR Initializer invokation will target the static class in XR

            // // switch
            // if (objectType == ClientXRObjectType.Ball)
            // {
            //     var throwHandle = go.AddComponent<ThrowHandle>();
            //     NetBallController.SetThrowableConfig(throwHandle);
            //     var interactable = go.AddComponent<ThrowLabXRGrabInteractable>();
            //     interactable.movementType = XRBaseInteractable.MovementType.Kinematic;
            //     interactable.smoothPosition = true;
            //     interactable.smoothPositionAmount = 5;
            //     interactable.tightenPosition = 0.5f;
            //     interactable.trackScale = false;
            // }
        }
#else
        public void InitializeClientObject(GameObject go, ClientXRObjectType type)
        {
            // nothing probably needs to be done here
        }
#endif
    }
}


// this guy can't be in the XR library, or in the main.
// standalone could be a helpful folder to bridge this gap.

// asmdef Standalone references both main and XR while always compiling
public class XRInitializer : NetworkBehaviour
{
    ClientXRObjectType objectType;
    private Action onInitialization;

    public override void OnStartClient()
    {
        base.OnStartClient();
        #if !UNITY_SERVER
        // make XRAsset library call
        #endif
    }
}