using System;
using System.Collections.Generic;
using CloudFine.ThrowLab;
using Fusion;
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
        public int index;
        public Team team;
        public BallType type;

        public override void Spawned()
        {
            base.Spawned();
            NetBallController.SetBallConfig(GetComponent<ThrowHandle>());
        }

        public void SetBallType(BallType ballType)
        {
            if (ballType == BallType.None)
            {
                gameObject.SetActive(false);
                return;
            }
            type = ballType;
            gameObject.SetActive(true);
        }

        public void ThrowBall(Vector3 velocity)
        {
            GetComponent<Rigidbody>().velocity = velocity;
        }

        public void Initialize(BallType type, Vector3 velocity, int throwCount, Team ownerTeam)
        {
            SetBallType(type);
            ThrowBall(velocity);
            team = ownerTeam;
            index = throwCount;
        }
    }
}