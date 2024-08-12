using UnityEngine;

public class TracerThrowLabTask : TracerTask
{
    [SerializeField] private float targetSpeed;
    [SerializeField] private RobotTaskManager taskManager;
    public override void Execute()
    {
        taskActions?.Invoke();
        taskManager.speed = targetSpeed;
        taskManager.robotLeadingTarget.tracerSpeed = targetSpeed;
        taskManager.SetInteractiveRotationActive(true, this);
    }
}
