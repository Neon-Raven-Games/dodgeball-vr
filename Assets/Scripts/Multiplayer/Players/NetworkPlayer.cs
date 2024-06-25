using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using Unity.Template.VR.Multiplayer;
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


    public void GripPerform()
    {
        if (!Object.HasInputAuthority) return;
        _gripPreformed = true;
    }

    public void GripCancel()
    {
        if (!Object.HasInputAuthority) return;
        _gripCancelled = true;
    }

    public void SubscribeInput(Action gripPerformed, Action gripCancelled)
    {
        _gripPerformedAction = gripPerformed;
        _gripCancelledAction = gripCancelled;
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

            _moveInput = input.Value.axis;
        }
        return false;
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
        };

        input.Set(ikInput);
    }

    #endregion

    #region ball RPC

    private int _throwCount;
    private readonly Dictionary<int, NetworkObject> _dodgeballDictionary = new();
    [SerializeField] private GameObject ballPrefab;

    // This method on the non-state authority client does not remove the net ball, but does spawn local
    // this method is either not being called by non-state authority
    // or the _dodgeballDictionary on the state authority does not contain the key
    // ie. junk in/junk out, or rpc not being called. likely the former
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    public void RPC_PossessBall(NetBallPossession possession, BallType type, int ballIndex, NetworkId id)
    {
        if (possession == NetBallPossession.None)
        {
            Debug.LogError("Invalid ball possession: " + possession + " index: " + ballIndex + " type: " + type);
        }

        if (_dodgeballDictionary.ContainsKey(ballIndex))
        {
            var ballObject = _dodgeballDictionary[ballIndex];
            _dodgeballDictionary.Remove(ballIndex);
            Runner.Despawn(ballObject);
        }
        else
        {
            Debug.LogError("Ball not found: " + ballIndex);
        }

        RPC_SetBallPossession(possession, type, id);
    }

    // this method is called from non-state authority when RpcSources.All,
    // and local ball does delete, but the resulting ball is laggy as shit
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    public void RPC_ThrownBall(BallType type, Vector3 position, Vector3 velocity, Team ownerTeam,
        NetBallPossession possession, NetworkId id)
    {
        var ballIndex = _throwCount++;
        Runner.Spawn(ballPrefab, position, Quaternion.identity, Object.InputAuthority,
            (runner, obj) =>
            {
                var db = obj.GetBehaviour<NetDodgeball>();
                _dodgeballDictionary.Add(ballIndex, obj);
                db.Initialize(type, velocity, ballIndex, ownerTeam);
            });
        RPC_SetBallPossession(possession, BallType.None, id);
    }

    // not tested yet until other issues resolved
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, TickAligned = false)]
    public void RPC_TargetHit(Team ownerTeam, Team targetTeam, int ballIndex)
    {
        if (_dodgeballDictionary.ContainsKey(ballIndex) && ownerTeam != targetTeam)
        {
            if (ownerTeam == Team.TeamOne) GameManager.teamOneScore++;
            else if (ownerTeam == Team.TeamTwo) GameManager.teamTwoScore++;
            GameManager.UpdateScore();
            Debug.Log("Hit player!");
        }
        else
        {
            Debug.LogError("Ball not found: " + ballIndex + " " + ownerTeam + " -> " + targetTeam);
        }
    }

    // This method does not work properly when calling possession rpc is called from the non-state authority
    [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
    private void RPC_SetBallPossession(NetBallPossession possession, BallType type, NetworkId id)
    {
        // this condition may be working improperly, but it shouldn't
        if (Object.HasInputAuthority || id != Id.Object) return;

        if (possession == NetBallPossession.None)
        {
            leftBallIndex.SetBallType(BallType.None);
            rightBallIndex.SetBallType(BallType.None);
        }
        else if (possession == NetBallPossession.LeftHand)
        {
            leftBallIndex.SetBallType(type);
        }
        else if (possession == NetBallPossession.RightHand)
        {
            rightBallIndex.SetBallType(type);
        }
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
        if (client)
        {
            Object.Runner.AddCallbacks(this);
            localPlayer.playerModel.SetActive(true);
            ikTargetModel.playerModel.SetActive(false);

            // no longer synchronizing the player model
            if (Object.HasStateAuthority)
            {
                _dodgeballDictionary.Add(_throwCount,
                    NetBallController.SpawnInitialBall(0, Object.Runner).GetComponent<NetworkObject>());
                _dodgeballDictionary[_throwCount].GetComponent<NetDodgeball>()
                    .Initialize(BallType.Dodgeball, Vector3.zero, _throwCount, Team.None);
                _throwCount++;

                _dodgeballDictionary.Add(_throwCount,
                    NetBallController.SpawnInitialBall(1, Object.Runner).GetComponent<NetworkObject>());
                _dodgeballDictionary[_throwCount].GetComponent<NetDodgeball>()
                    .Initialize(BallType.Dodgeball, Vector3.zero, _throwCount, Team.None);
                _throwCount++;

                _dodgeballDictionary.Add(_throwCount,
                    NetBallController.SpawnInitialBall(2, Object.Runner).GetComponent<NetworkObject>());
                _dodgeballDictionary[_throwCount].GetComponent<NetDodgeball>()
                    .Initialize(BallType.Dodgeball, Vector3.zero, _throwCount, Team.None);
                _throwCount++;

                return;
            }

            networkPlayerTarget.GetComponent<NetworkTransform>().enabled = false;
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

        if (!Object.HasInputAuthority) _netIKTargetHelper.SetAxis(_moveInput);
        else InvokeActions();
        if (!Object.HasStateAuthority) return;

        UpdateHostNetModels();
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

        // this is trying to move the players head, but we are using the ik targets now
        // ikTargetModel.hmdTarget.position = networkHeadTarget.position;
        // ikTargetModel.hmdTarget.rotation = networkHeadTarget.rotation;
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