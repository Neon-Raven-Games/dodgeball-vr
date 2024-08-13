using System.Collections.Generic;
using Hands.SinglePlayer.Lobby.RobotTasks.Sequence;
using UnityEngine;


namespace Hands.SinglePlayer.Lobby.RobotTasks
{
    public class LeadingBehavior : RobotTask
    {

        public override void EnterTask(RoboSequence sequence)
        {
            taskSequence = sequence;
            SetNewWaypoints();
            TaskManager.speed = sequence.speed;
            TaskManager.robotLeadingTarget.tracerSpeed = sequence.speed;
        }

        public override void ExitTask()
        {
            // cleanup task
            
        }

        public override void Update()
        {
            TaskManager.transform.position = Vector3.MoveTowards(TaskManager.transform.position,
                TaskManager.robotLeadingTarget.transform.position,
                TaskManager.speed * Time.fixedDeltaTime);
            TaskManager.transform.LookAt(TaskManager.robotLeadingTarget.transform.position);
        }

        public override void FixedUpdate()
        {

        }

        public LeadingBehavior(RobotTaskManager taskManager) : base(taskManager)
        {
        }
    }
}