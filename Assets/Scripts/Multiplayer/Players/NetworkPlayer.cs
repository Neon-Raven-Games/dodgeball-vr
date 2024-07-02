using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Addons.Physics;
using Fusion.Sockets;
using Unity.Template.VR.Multiplayer.Players;
using UnityEngine;

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

// mouth sprites for talking

// hair movement when player moves:
// hair bones

public class NetworkPlayer : NetworkBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private PlayerRig localPlayer;
    [SerializeField] public PlayerRig ikTargetModel;
    [SerializeField] private Transform networkPlayerTarget;
    [SerializeField] private Transform networkHeadTarget;
    [SerializeField] private DevController localController;

    [SerializeField] private NetBallPossessionHandler leftBallIndex;
    [SerializeField] private NetBallPossessionHandler rightBallIndex;

    #region inputs

    private Vector3 _leftHandPosition;
    private Vector3 _rightHandPosition;

    private Quaternion _leftHandRotation;
    private Quaternion _rightHandRotation;

    private Vector3 _hmdPosition;
    private Quaternion _hmdRotation;

    private Vector2 _moveInput;
    private NetIKTargetHelper _netIKTargetHelper;

    private bool _gripPreformed;
    private Action _gripPerformedAction;

    private bool _gripCancelled;
    private Action _gripCancelledAction;

    public void GripPreform()
    {
        _gripPreformed = true;
    }

    public void GripCancel()
    {
        _gripCancelled = true;
    }

    public void SubscribeInput(Action gripPerformed, Action gripCancelled)
    {
        _gripPerformedAction = gripPerformed;
        _gripCancelledAction = gripCancelled;
    }

    public void Update()
    {
        // // tested p2 client for state authority sync issues
        // if (!Object.HasInputAuthority || Object.HasStateAuthority) return;
        // if (Keyboard.current.aKey.wasPressedThisFrame)
        // {
        //     GripPreform();
        // }
        //
        // if (Keyboard.current.sKey.wasPressedThisFrame)
        // {
        //     GripCancel();
        // }
    }

    public void UnsubscribeGrips()
    {
        _gripCancelledAction = null;
        _gripPerformedAction = null;
    }

    private bool ExtractNetInput()
    {
        if (Object.HasInputAuthority)
        {
            _leftHandPosition = localPlayer.leftHandTarget.position;
            _rightHandPosition = localPlayer.rightHandTarget.position;
            _leftHandRotation = localPlayer.leftHandTarget.rotation;
            _rightHandRotation = localPlayer.rightHandTarget.rotation;

            _hmdPosition = localPlayer.hmdTarget.position;
            _hmdRotation = localPlayer.hmdTarget.rotation;

            _moveInput = localController.GetMoveInput();
            InvokeActions();
        }
        else
        {
            UpdateNetBallPossessions();

            var input = GetInput<IKInput>();
            if (input == null) return true;

            _leftHandPosition = input.Value.leftHandPosition;
            _rightHandPosition = input.Value.rightHandPosition;
            _leftHandRotation = input.Value.leftHandRotation;
            _rightHandRotation = input.Value.rightHandRotation;

            _hmdPosition = input.Value.hmdPosition;
            _hmdRotation = input.Value.hmdRotation;

            _moveInput = input.Value.axis;
            _gripPreformed = input.Value.gripPreformed != 0;
            _gripCancelled = input.Value.gripCancelled != 0;

            _grabData = input.Value.grabData;
            InvokeActions();
        }

        return false;
    }

    private void RefreshInput()
    {
        
    }
    private void UpdateNetBallPossessions()
    {
        leftBallIndex.UpdatePossession(Object.Id);
        rightBallIndex.UpdatePossession(Object.Id);
    }

    private GrabData _grabData;

    public void SetGrabData(GrabData grabData)
    {
        _grabData = grabData;
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
            axis = _moveInput,
            gripPreformed = _gripPreformed ? 1 : 0,
            gripCancelled = _gripCancelled ? 1 : 0,
            grabData = _grabData
        };

        input.Set(ikInput);
    }

    #endregion

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
        _grabData.ballIndex = -1;
        NetBallController.AddPlayerData(Object.Id, Team.TeamOne);
        if (client)
        {
            Object.Runner.AddCallbacks(this);
            localPlayer.playerModel.SetActive(true);
            ikTargetModel.playerModel.SetActive(false);

            if (Object.HasStateAuthority)
            {
                var dodgeball = NetBallController.SpawnInitialBall(0, Object.Runner);
                dodgeball.Initialize(BallType.Dodgeball, 0, Team.None, Runner);

                dodgeball = NetBallController.SpawnInitialBall(1, Object.Runner);
                dodgeball.Initialize(BallType.Dodgeball, 1, Team.None, Runner);

                dodgeball = NetBallController.SpawnInitialBall(2, Object.Runner);
                dodgeball.Initialize(BallType.Dodgeball, 2, Team.None, Runner);

                return;
            }

            ikTargetModel.leftHandTarget.GetComponent<NetworkTransform>().enabled = false;
            ikTargetModel.rightHandTarget.GetComponent<NetworkTransform>().enabled = false;
        }
        else
        {
            localPlayer.playerModel.SetActive(false);
            ikTargetModel.playerModel.SetActive(true);
            _netIKTargetHelper = ikTargetModel.playerModel.GetComponent<NetIKTargetHelper>();
        }
    }

    public override void FixedUpdateNetwork()
    {

        if (ExtractNetInput()) return;
        
        if (_grabData.velocity != Vector3.zero)
        {
            NetBallController.PossessBall(Object.Id, _grabData.possession);
            var ball = NetBallController.GetBall(_grabData.ballIndex);
            if (ball == null)
            {
                Debug.LogError("Ball not found");
                return;
            }

            ball.SetBallType(BallType.Dodgeball);
            var ballRb = ball.GetComponent<NetworkRigidbody3D>();
            ballRb.Teleport(_grabData.position);
            ballRb.RBIsKinematic = false;
            ballRb.Rigidbody.velocity = _grabData.velocity;


            NetBallController.UpdatePlayerPossessionData(Object.Id, _grabData.possession, BallType.None);
            _grabData = new GrabData
            {
                ballIndex = -1
            };
        }
        
        UpdateBallPosition();
        
        if (!Object.HasInputAuthority) _netIKTargetHelper.SetAxis(_moveInput);
        if (!Object.HasStateAuthority) return;

        UpdateHostNetModels();
    }

    private void UpdateBallPosition()
    {
        if (_grabData.ballIndex >= 0 && _grabData.velocity == Vector3.zero)
        {
            NetBallController.PossessBall(Object.Id, _grabData.possession);
            var ball = NetBallController.GetBall(_grabData.ballIndex);
            if (ball == null) return;

            if (!Object.HasInputAuthority) ball.SetBallType(BallType.None);
            ball.transform.position = _grabData.position;
        }
    }

    private void InvokeActions()
    {
        if (_gripPreformed)
        {
            _gripPreformed = false;
            _gripPerformedAction?.Invoke();
        }

        if (_gripCancelled)
        {
            _gripCancelled = false;
            _gripCancelledAction?.Invoke();
        }
    }

    #region IKTargets

    private void UpdateHostNetModels()
    {
        UpdateLeftHand();
        UpdateRightHand();
        UpdateHead();
    }

    private void UpdateHead()
    {
        MoveNetIKTarget(networkHeadTarget.transform, _hmdPosition);
        RotateNextIKTarget(networkHeadTarget, _hmdRotation);
    }


    private void UpdateLeftHand()
    {
        var dxOffset = new Vector3(90, 90, -180);
        RotateNextIKTarget(ikTargetModel.leftHandTarget, _leftHandRotation, dxOffset);
        MoveNetIKTarget(ikTargetModel.leftHandTarget, _leftHandPosition);
    }

    private void UpdateRightHand()
    {
        var dzOffset = new Vector3(90, -90, 0);
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