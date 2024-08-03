using System;
using Hands.SinglePlayer.Lobby.RobotTasks.Sequence;
using UnityEngine;

public class ThrowLabTrigger : MonoBehaviour
{
    [SerializeField] private GameObject playerObject;
    [SerializeField] private SequenceQueue sequenceQueue;
    [SerializeField] private RoboSequence entrySequence;
    [SerializeField] private RoboSequence sequence;
    [SerializeField] private RoboSequence exitSequence;
    [SerializeField] private RobotTaskManager robotTaskManager;
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ignore Raycast") ||
            other.gameObject.layer == LayerMask.NameToLayer("TeamOne"))
        {
            if (sequenceQueue.Count > 0 || !robotTaskManager.initialized || robotTaskManager.currentTask == RobotTasks.Informing)
            {
                Debug.Log("Sequence queue is not empty, returning.");
                return;
            }
            sequenceQueue.ClearQueue();
            sequenceQueue.QueueSequence(entrySequence);
            sequenceQueue.QueueSequence(sequence);
            sequenceQueue.QueueSequence(exitSequence);

            robotTaskManager.EndCurrentTask();
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ignore Raycast") ||
            other.gameObject.layer == LayerMask.NameToLayer("TeamOne"))
        {
            if(!robotTaskManager.initialized || robotTaskManager.currentTask == RobotTasks.Informing)
            {
                Debug.Log("Sequence queue is not empty, returning.");
                return;
            }
            sequenceQueue.ClearQueue();
            robotTaskManager.EndCurrentTask();
        }
    }
}
