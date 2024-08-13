#if !UNITY_SERVER
using Unity.Template.VR.Multiplayer;
#endif
using UnityEngine;

public class ClientInitialization : MonoBehaviour
{
#if !UNITY_SERVER
    public ClientXRObjectType objectType;
    public ThrowConfiguration config;
#endif

    public void OnStartClient()
    {
        // base.OnStartClient();
        Debug.Log("Initializing Dodgeball");
#if !UNITY_SERVER
        // if (!HasAuthority)
        // {
        InitializeClient(gameObject);
        // }
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