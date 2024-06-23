using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using Unity.Template.VR.Multiplayer.Players;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class PlayerRig
{
    public GameObject playerModel;
    public Transform hmdTarget;
    public Transform leftHandTarget;
    public Transform rightHandTarget;
}
// blend shapes:
// boy = 0
// girl = 100
// body
// pants
// hoodie
// t-shirt

// expressions
// first one, blinking
// collection of blend shapes for faces:
// eye shape
// eye
// eyebrow position/rotation
// eye specular

// hair movement when player moves:
// hair bones

// 
public class NetworkPlayer : NetworkBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private PlayerRig localPlayer;
    [SerializeField] public PlayerRig ikTargetModel;
    [SerializeField] private Transform networkPlayerTarget;
    [SerializeField] private Transform networkHeadTarget;
    
    private Vector3 _leftHandPosition;
    private Vector3 _rightHandPosition;

    private Quaternion _leftHandRotation;
    private Quaternion _rightHandRotation;

    private Vector3 _hmdPosition;
    private Quaternion _hmdRotation;

    private Vector3 _playerPosition;
    private Quaternion _playerRotation;

    private void OnDrawGizmos()
    {
        if (Object == null) return;
        Gizmos.color = Object.HasInputAuthority ? Color.green : Color.red;

        Gizmos.DrawSphere(ikTargetModel.rightHandTarget.position, 0.1f);
        Gizmos.DrawSphere(ikTargetModel.leftHandTarget.position, 0.1f);
        Gizmos.DrawSphere(ikTargetModel.hmdTarget.position, 0.1f);
    }

    public override void Spawned()
    {
        var client = Object.HasInputAuthority;
        if (client)
        {
            Object.Runner.AddCallbacks(this);
            localPlayer.playerModel.SetActive(true);
            ikTargetModel.playerModel.SetActive(false);

            // no longer synchronizing the player model
            if (Object.HasStateAuthority) return;
            networkPlayerTarget.GetComponent<NetworkTransform>().enabled = false;
            ikTargetModel.leftHandTarget.GetComponent<NetworkTransform>().enabled = false;
            ikTargetModel.rightHandTarget.GetComponent<NetworkTransform>().enabled = false;
        }
        else
        {
            localPlayer.playerModel.SetActive(false);
            ikTargetModel.playerModel.SetActive(true);

            var netIKTargetHelper = ikTargetModel.playerModel.GetComponent<NetIKTargetHelper>();
            netIKTargetHelper.AssignIKTargets(ikTargetModel.leftHandTarget, ikTargetModel.rightHandTarget);
        }
    }


    public override void FixedUpdateNetwork()
    {
        if (SyncIkTargets()) return;
        if (!Object.HasStateAuthority) return;
        UpdateHostNetModels();
    }

    private void UpdateHostNetModels()
    {
        UpdateLeftHand();
        UpdateRightHand();
        UpdateCharacter();
        UpdateHead();
    }

    #region IKTargets

    private void UpdateHead()
    {
        MoveNetIKTarget(networkHeadTarget.transform, _hmdPosition);
        RotateNextIKTarget(networkHeadTarget, _hmdRotation);
        
        ikTargetModel.hmdTarget.position = networkHeadTarget.position;
        ikTargetModel.hmdTarget.rotation = networkHeadTarget.rotation;
    }
    
    private void UpdateCharacter()
    {
        MoveNetIKTarget(networkPlayerTarget.transform, _playerPosition);
        RotateNextIKTarget(networkPlayerTarget.transform, _playerRotation);

        ikTargetModel.playerModel.transform.position = networkPlayerTarget.position;
        ikTargetModel.playerModel.transform.rotation = networkPlayerTarget.rotation;
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var ikInput = new IKInput
        {
            leftHandPosition = _leftHandPosition,
            rightHandPosition = _rightHandPosition,
            leftHandRotation = _leftHandRotation,
            rightHandRotation = _rightHandRotation,
            hmdPosition = _hmdPosition,
            hmdRotation = _hmdRotation,
            playerPosition = _playerPosition,
            playerRotation = _playerRotation
        };

        input.Set(ikInput);
    }

    // profile this
    private bool SyncIkTargets()
    {
        if (Object.HasInputAuthority)
        {
            _leftHandPosition = localPlayer.leftHandTarget.position;
            _rightHandPosition = localPlayer.rightHandTarget.position;
            _leftHandRotation = localPlayer.leftHandTarget.rotation;
            _rightHandRotation = localPlayer.rightHandTarget.rotation;

            _hmdPosition = localPlayer.hmdTarget.position;
            _hmdRotation = localPlayer.hmdTarget.rotation;

            _playerPosition = localPlayer.playerModel.transform.position;
            _playerRotation = localPlayer.playerModel.transform.rotation;
        }
        else
        {
            var input = GetInput<IKInput>();
            if (input == null) return true;
            _leftHandPosition = input.Value.leftHandPosition;
            _rightHandPosition = input.Value.rightHandPosition;
            _leftHandRotation = input.Value.leftHandRotation;
            _rightHandRotation = input.Value.rightHandRotation;

            _hmdPosition = input.Value.hmdPosition;
            _hmdRotation = input.Value.hmdRotation;

            _playerPosition = input.Value.playerPosition;
            _playerRotation = input.Value.playerRotation;
        }

        return false;
    }

    private void UpdateLeftHand()
    {
        var dxOffset = new Vector3(-180, 90, -180);
        RotateNextIKTarget(ikTargetModel.leftHandTarget, _leftHandRotation, dxOffset);
        MoveNetIKTarget(ikTargetModel.leftHandTarget, _leftHandPosition);
    }

    private void UpdateRightHand()
    {
        var dzOffset = new Vector3(0, -90, 0);
        RotateNextIKTarget(ikTargetModel.rightHandTarget, _rightHandRotation, dzOffset);
        MoveNetIKTarget(ikTargetModel.rightHandTarget, _rightHandPosition);
    }

    private static void MoveNetIKTarget(Transform ikTransform, Vector3 playerTransform)
    {
        ikTransform.position =
            Vector3.Lerp(ikTransform.position, playerTransform, Time.deltaTime * 10);
    }

    private static void RotateNextIKTarget(Transform ikTransform, Quaternion rotation, Vector3 rotationOffset)
    {
        ikTransform.rotation =
            Quaternion.Lerp(ikTransform.rotation, rotation * Quaternion.Euler(rotationOffset),
                Time.deltaTime * 10);
    }

    private static void RotateNextIKTarget(Transform ikTransform, Quaternion rotation)
    {
        ikTransform.rotation =
            Quaternion.Lerp(ikTransform.rotation, rotation,
                Time.deltaTime * 10);
    }

    #endregion


    #region Unused Runner Callbacks

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
    }

    #endregion
}