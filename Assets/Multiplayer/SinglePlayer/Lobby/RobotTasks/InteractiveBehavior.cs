using Hands.SinglePlayer.Lobby.RobotTasks.Sequence;
using UnityEngine;

namespace Hands.SinglePlayer.Lobby.RobotTasks
{
    public class InteractiveBehavior : RobotTask
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
            _rotationActive = false;
        }

        public override void Update()
        {
            TaskManager.transform.position = Vector3.MoveTowards(TaskManager.transform.position,
                TaskManager.robotLeadingTarget.transform.position,
                TaskManager.speed * Time.fixedDeltaTime);

            if (_rotationActive) TaskManager.transform.LookAt(TaskManager.Player.transform.position);
            else TaskManager.transform.LookAt(TaskManager.robotLeadingTarget.transform.position);
        }

        public override void FixedUpdate()
        {
        }

        public InteractiveBehavior(RobotTaskManager taskManager) : base(taskManager)
        {
        }

        private bool _rotationActive;

        public void SetRotationActive(bool active)
        {
            _rotationActive = active;
        }
    }
}