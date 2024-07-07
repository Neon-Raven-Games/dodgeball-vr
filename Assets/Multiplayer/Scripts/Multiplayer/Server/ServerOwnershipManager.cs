using FishNet.Connection;
using FishNet.Object;
using Unity.Template.VR.Multiplayer;
using UnityEngine;

public class ServerOwnershipManager : NetworkBehaviour
{
    private static ServerOwnershipManager _instance;
    private void Start()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public static void RequestOwnershipFromServer(NetworkBehaviour networkObject, NetworkConnection requestingPlayer)
    {
        _instance.RequestOwnershipServerRpc(networkObject, requestingPlayer);
    }
    
    public static void ReleaseOwnershipFromServer(NetworkBehaviour networkObject, Vector3 velocity, Vector3 position)
    {
        _instance.ReleaseOwnershipServerRpc(networkObject, velocity, position);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void RequestOwnershipServerRpc(NetworkBehaviour networkObject, NetworkConnection requestingPlayer)
    {
        Debug.Log("Attempting to transfer ownership...");
        if (IsOwnershipRequestValid(networkObject, requestingPlayer))
        {
            networkObject.GetComponent<NetDodgeball>().state.Value = BallState.Possessed;
            networkObject.GiveOwnership(requestingPlayer);
            Debug.Log($"Ownership of {networkObject.name} granted to {requestingPlayer.ClientId}");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReleaseOwnershipServerRpc(NetworkBehaviour networkObject,  Vector3 velocity, Vector3 position)
    {
        Debug.Log("Removing ownership...");
        networkObject.RemoveOwnership();
        var netDb = networkObject.GetComponent<NetDodgeball>();
        netDb.StartCoroutine(netDb.WaitForServerOwner(velocity, position));
    }

    private bool IsOwnershipRequestValid(NetworkBehaviour networkObject, NetworkConnection requestingPlayer)
    {
        return networkObject.HasAuthority && !networkObject.IsOwner;
    }
}