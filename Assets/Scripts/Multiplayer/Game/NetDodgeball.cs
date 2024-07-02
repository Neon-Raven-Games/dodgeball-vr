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
    public Vector3 velocity;
    public Vector3 position;
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
    public class NetDodgeball : NetworkBehaviour
    {
        [Networked] public int index { get; set; }
        [Networked] public Team team { get; set; }
        [Networked] public BallType type { get; set; }

        private NetworkRigidbody3D _rb;
        [SerializeField] private GameObject visualBall;
        public override void Spawned()
        {
            base.Spawned();
            _rb = GetComponent<NetworkRigidbody3D>();
            NetBallController.SetBallConfig(GetComponent<ThrowHandle>());
        }

        internal void SetBallType(BallType ballType)
        {
            if (ballType == BallType.None)
            {
                visualBall.SetActive(false);
                return;
            }

            type = ballType;
            visualBall.SetActive(true);
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


        public void SetOwner(NetworkId playerRef, NetBallPossession possession)
        {
        }

        public void Initialize(BallType spawnType, int mappedIndex, Team ownerTeam, NetworkRunner runner)
        {
            SetBallType(spawnType);
            team = ownerTeam;
            index = mappedIndex;
        }
    }
}