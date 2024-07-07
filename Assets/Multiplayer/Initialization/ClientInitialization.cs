using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public class ClientInitialization : NetworkBehaviour // , IClientXRObject
{
#if !UNITY_SERVER
    public ClientXRObjectType objectType;
    
    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("Initializing Dodgeball");
        if (!HasAuthority)
        {
            InitializeClient(gameObject);
        }
    }
    
    public void InitializeClient(GameObject go)
    {
        var concrete = new ConcreteClientXRObject();
        concrete.InitializeClientObject(go, objectType);
    }
#endif
}