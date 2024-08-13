using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hands.SinglePlayer.Lobby
{
    public class PathTracer : MonoBehaviour
    {
        [SerializeField] public float tracerSpeed;
        public List<Transform> _waypoints;
        private int _currentTravelPointIndex;
        public GameObject robot;
        public event Action OnPathComplete;

        public void SetWaypoints(List<Transform> newWaypoints)
        {
            _waypoints = newWaypoints;
            _currentTravelPointIndex = 0;
        }
        
        private void Update()
        {
            if (!robot.activeSelf || _waypoints == null || _waypoints.Count == 0) return;
            var nextPoint = _waypoints[_currentTravelPointIndex];
            transform.position =
                Vector3.MoveTowards(transform.position, nextPoint.position, tracerSpeed * Time.fixedDeltaTime);
            if (Vector3.Distance(transform.position, nextPoint.position) < 0.1f)
            {
                var task = _waypoints[_currentTravelPointIndex].GetComponent<TracerTask>();
                if (task && task.enabled) task.Execute();
                
                _currentTravelPointIndex++;
                if (_currentTravelPointIndex >= _waypoints.Count)
                {
                    OnPathComplete?.Invoke();
                    _currentTravelPointIndex = 0;
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {

            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(transform.position, 0.05f);
            if (_waypoints == null || _waypoints.Count <= 1) return;
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_waypoints[_currentTravelPointIndex].position, 0.08f);
            // Gizmos.color = Color.red;
            // var from = _waypoints[^1].position;
            // foreach (var travelPoint in _waypoints)
            // {
            //     Gizmos.DrawLine(from, travelPoint.position);
            //     from = travelPoint.position;
            // }

        }
#endif
    }
}