using Hands.SinglePlayer.Lobby.RobotTasks.Sequence;

namespace Hands.SinglePlayer.Lobby
{
    public abstract class RobotTask
    {
        protected RobotTaskManager TaskManager { get; }
        protected RoboSequence taskSequence;

        protected RobotTask(RobotTaskManager taskManager)
        {
            TaskManager = taskManager;
        }
        // pass in the sequence
        public abstract void EnterTask(RoboSequence sequence);
        
        // dequeue the sequence
        public abstract void ExitTask();
        public abstract void Update();
        public abstract void FixedUpdate();

        public void SetNewWaypoints()
        {
            TaskManager.robotLeadingTarget.SetWaypoints(taskSequence.GetWaypoints());
        }
    }
}