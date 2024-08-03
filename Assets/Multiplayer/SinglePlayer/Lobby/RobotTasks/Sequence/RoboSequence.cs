using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hands.SinglePlayer.Lobby.RobotTasks.Sequence
{
    [Serializable]
    public class RoboSequence
    {
        public global::RobotTasks task;
        
        public Transform waypointParent;
        public Transform target;
        public List<AudioClip> dialogueClips;
        
        private List<Transform> _waypoints;
        public List<Transform> GetWaypoints()
        {
            _waypoints = new List<Transform>();
            foreach (Transform child in waypointParent)
            {
                _waypoints.Add(child);
            }

            return _waypoints;
        }
        public float speed;

        public bool IsComplete()
        {
            if (target == null) return false;
            return true;
        }
        
        public void Update()
        {
            
        }

        public void FixedUpdate()
        {
            
        }
    }
}