using System.Collections.Generic;
using Hands.SinglePlayer.Lobby.RobotTasks.Sequence;
using UnityEngine;


namespace Hands.SinglePlayer.Lobby.RobotTasks
{
    public class LeadingBehavior : RobotTask
    {
        private List<Transform> _waypoints;
        private int _currentWaypointIndex;

        public void SetWaypoints(List<Transform> newWaypoints)
        {
            _waypoints = newWaypoints;
            _currentWaypointIndex = 0;
        }

        public void SetTarget(PathTracer target)
        {
            TaskManager.robotLeadingTarget = target;
        }

        private void MoveAlongWaypoints()
        {
            if (_waypoints == null || _waypoints.Count == 0) return;
        }

        public override void EnterTask(RoboSequence sequence)
        {
            taskSequence = sequence;
            SetNewWaypoints();
        }

        public override void ExitTask()
        {
            // cleanup task
        }

        public override void Update()
        {
        }

        public override void FixedUpdate()
        {
            TaskManager.transform.position = Vector3.MoveTowards(TaskManager.transform.position,
                TaskManager.robotLeadingTarget.transform.position,
                TaskManager.speed * Time.deltaTime);
            TaskManager.transform.LookAt(TaskManager.robotLeadingTarget.transform.position);


        }

        public LeadingBehavior(RobotTaskManager taskManager) : base(taskManager)
        {
        }
    }
}