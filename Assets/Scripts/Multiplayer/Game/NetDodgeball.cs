using System;
using System.Collections.Generic;
using CloudFine.ThrowLab;
using Fusion;
using Fusion.Addons.Physics;
using Fusion.Sockets;
using UnityEngine;

public struct BallInput : INetworkInput
{
    public NetBallPossession possession;
    public NetworkId player;
    public Vector3 ownerHandPosition;
}

namespace Unity.Template.VR.Multiplayer
{
    [Serializable]
    public class DodgeballIndex
    {
        public BallType type;
        public GameObject dodgeball;
    }

    // we need to make the hand grab the dodgeball
    // IK final should be able to handle this
    public class NetDodgeball : NetworkBehaviour, INetworkRunnerCallbacks
    {
        [Networked] public int index { get; set; }
        [Networked] public Team team { get; set; }
        [Networked] public BallType type { get; set; }

        private NetworkRigidbody3D _rb;
        public override void Spawned()
        {
            base.Spawned();
            _rb = GetComponent<NetworkRigidbody3D>();
            NetBallController.SetBallConfig(GetComponent<ThrowHandle>());
        }

        private void SetBallType(BallType ballType)
        {
            if (ballType == BallType.None)
            {
                gameObject.SetActive(false);
                return;
            }

            type = ballType;
            gameObject.SetActive(true);
        }

        public void ThrowBall(Vector3 position, Vector3 velocity)
        {
            ownerHandPosition = Vector3.zero;
            _rb.Teleport(position);
            _rb.Rigidbody.velocity = velocity;
        }

        // todo, this is how we need to handle the ball possession
        /*
        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();
            int maxColliders = 5;
            Collider[] hitColliders = new Collider[maxColliders];
            
            Physics.OverlapSphereNonAlloc(transform.position, _collider.bounds.extents.magnitude, hitColliders, 
                LayerMask.NameToLayer("TeamOne"));
            
            for (int i = 0; i < maxColliders; i++)
            {
                if (hitColliders[i] == null) break;
                if (hitColliders[i].gameObject == gameObject) continue;
                GetComponent<HandController>()._ball = hitColliders[i].gameObject;
                break;
            }
        }
*/
        private NetworkId ownerId;
        private NetBallPossession currentPossession;
        [Networked] private Vector3 ownerHandPosition { get; set; }

        public override void FixedUpdateNetwork()
        {
            if (GetComponent<DodgeBall>()._ballState == BallState.Possessed)
            {
                if (!GetInput<BallInput>(out var ballInput)) return;
                
                // update position
                if (ballInput.ownerHandPosition != Vector3.zero)
                {
                    ownerHandPosition = ballInput.ownerHandPosition;
                    transform.position = ownerHandPosition;
                    _rb.Teleport(transform.position);
                }
                
                // update possession
                currentPossession = ballInput.possession;
                ownerId = ballInput.player;
                if (HasStateAuthority) NetBallController.PossessBall(ownerId, currentPossession);
            }
        }

        // something is wrong here
        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            var ballInput = new BallInput()
            {
                ownerHandPosition = ownerHandPosition,
                possession = currentPossession,
                player = ownerId
            };
            
            input.Set(ballInput);
        }

        public void Initialize(BallType spawnType, Vector3 velocity, int mappedIndex, Team ownerTeam,
            NetworkRunner runner)
        {
            SetBallType(spawnType);
            team = ownerTeam;
            index = mappedIndex;

            runner.AddCallbacks(this);
        }

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

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request,
            byte[] token)
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

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key,
            ArraySegment<byte> data)
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

        public void SetLocalOwnerPosition(Vector3 position)
        {
            ownerHandPosition = position;
        }

        public void SetOwner(NetworkId playerRef, NetBallPossession possession)
        {
            ownerId = playerRef;
            currentPossession = possession;
        }
    }
}