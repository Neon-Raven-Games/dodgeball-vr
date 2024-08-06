using Hands.SinglePlayer.Lobby.RobotTasks.Sequence;
using UnityEngine;

namespace Hands.SinglePlayer.Lobby.RobotTasks
{
    public class InformingBehavior : RobotTask
    {
        private const float _DISTANCE_FROM_PLAYER = 1.25f;
        private const float _FOV_OFFSET = 0.125f;
        private bool _isInforming;

        public InformingBehavior(RobotTaskManager taskManager) : base(taskManager)
        {
        }

        public override void EnterTask(RoboSequence sequence)
        {
            _isInforming = false;
            taskSequence = sequence;
            TaskManager.robotLeadingTarget.OnPathComplete += SetInforming;
            SetNewWaypoints();
        }

        private void SetInforming()
        {
            TaskManager.robotLeadingTarget.OnPathComplete -= SetInforming;
            _isInforming = true;
        }

        public override void ExitTask()
        {
            if (!_isInforming) TaskManager.robotLeadingTarget.OnPathComplete -= SetInforming;
            _isInforming = false;
            _currentDialogueIndex = 0;
            TaskManager.audioSource.Stop();
        }

        public override void Update()
        {
            if (!_isInforming) TraverseWaypoints();
            else Inform();
        }

        public override void FixedUpdate()
        {

        }

 private void Inform()
{
    var player = TaskManager.Player;
    var playerForward = player.transform.forward;
    var playerRight = player.transform.right;
    var playerPosition = player.transform.position;

    var offsetForward = playerForward * _DISTANCE_FROM_PLAYER;
    var offsetRight = playerRight * _FOV_OFFSET;
    var offsetUp = new Vector3(0, 0.5f, 0); 

    var desiredPosition = playerPosition + offsetForward + offsetRight + offsetUp;
    var minimumDistance = 1.0f; 
    if (Vector3.Distance(playerPosition, desiredPosition) < minimumDistance)
    {
        desiredPosition = playerPosition + (desiredPosition - playerPosition).normalized * minimumDistance;
        desiredPosition.y = playerPosition.y + 0.65f; // Ensure the robot stays at a reasonable height
    }

    if (Physics.Raycast(playerPosition, (desiredPosition - playerPosition).normalized, out var hit, _DISTANCE_FROM_PLAYER))
        desiredPosition = hit.point - (desiredPosition - playerPosition).normalized * 0.1f;

    TaskManager.transform.position = Vector3.Lerp(TaskManager.transform.position,
        desiredPosition, TaskManager.speed * Time.deltaTime);

    playerPosition.y += 0.5f;
    TaskManager.transform.LookAt(playerPosition);
    if (!ConfigurationManager.botMuted && Vector3.Distance(Camera.main.transform.position, TaskManager.transform.position) < 5f)
    {
        PlayAudio();
    }
}

private int _currentDialogueIndex;

        private void PlayAudio()
        {
            if (TaskManager.audioSource.isPlaying) return;
            if (_currentDialogueIndex >= taskSequence.dialogueClips.Count)
            {
                TaskManager.EndCurrentTask();
                return;
            }
            
            TaskManager.audioSource.clip = taskSequence.dialogueClips[_currentDialogueIndex];
            
            // todo, we need to clip the audio files
            TaskManager.audioSource.time = 0;
            TaskManager.audioSource.Play();
            _currentDialogueIndex++;
        }

        private void TraverseWaypoints()
        {
            TaskManager.transform.position = Vector3.MoveTowards(TaskManager.transform.position,
                TaskManager.robotLeadingTarget.transform.position,
                TaskManager.speed * Time.fixedDeltaTime);
            TaskManager.transform.LookAt(TaskManager.robotLeadingTarget.transform.position);
        }
    }
}