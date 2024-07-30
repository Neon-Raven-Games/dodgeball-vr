using System;
using Hands.SinglePlayer.Lobby.RobotTasks.Sequence;
using UnityEngine;

public class ThrowLabTrigger : MonoBehaviour
{
    [SerializeField] private GameObject playerObject;
    [SerializeField] private SequenceQueue sequenceQueue;
    [SerializeField] private RoboSequence sequence;
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == playerObject)
        {
            sequenceQueue.QueueSequence(sequence);
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == playerObject)
        {
            sequenceQueue.DequeueSequence();
        }
    }
}
