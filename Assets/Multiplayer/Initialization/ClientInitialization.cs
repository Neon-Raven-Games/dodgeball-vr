using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using Unity.Template.VR.Multiplayer;
using UnityEngine;

public class ClientInitialization : NetworkBehaviour
{
#if !UNITY_SERVER
    public ClientXRObjectType objectType;
    public ThrowConfiguration config;
#endif
    
    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("Initializing Dodgeball");
#if !UNITY_SERVER
        if (!HasAuthority)
        {
            InitializeClient(gameObject);
        }
#endif
    }
    
    public void InitializeClient(GameObject go)
    {
#if !UNITY_SERVER
        var concrete = new ConcreteClientXRObject();
        concrete.InitializeClientObject(go, objectType, config);
#endif
    }
}