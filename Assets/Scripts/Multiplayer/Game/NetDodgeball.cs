using System;
using System.Collections.Generic;
using CloudFine.ThrowLab;
using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

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

        public NetworkRigidbody3D rigidBody;
        public override void Spawned()
        {
            base.Spawned();
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

        private void ThrowBall(Vector3 velocity)
        {
            rigidBody.Rigidbody.velocity = velocity;
        }
        
        public void Initialize(BallType spawnType, Vector3 velocity, int mappedIndex, Team ownerTeam)
        {
            SetBallType(spawnType);
            ThrowBall(velocity);
            team = ownerTeam;
            index = mappedIndex;
        }
    }
}