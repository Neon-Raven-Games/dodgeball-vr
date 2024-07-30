using System.Collections.Generic;
using UnityEngine;

namespace Hands.SinglePlayer.Lobby.RobotTasks.Sequence
{
    public class SequenceQueue : MonoBehaviour
    {
        private readonly Queue<RoboSequence> _sequenceQueue = new();
        public RoboSequence defaultSequence;
        private RoboSequence _currentSequence;
        
        public List<RoboSequence> sequences;

        private void Start()
        {
            _currentSequence = defaultSequence;
            foreach (var seq in sequences) _sequenceQueue.Enqueue(seq);
        }
        
        public void QueueSequence(RoboSequence sequence)
        {
            _sequenceQueue.Enqueue(sequence);
        }
        
        public global::RobotTasks NextTask()
        {
            if (_sequenceQueue.Count == 0) return defaultSequence.task;
            return _sequenceQueue.Peek().task;
        }
        
        public RoboSequence DequeueSequence()
        {
            // todo, handle empty sequences
            if (_sequenceQueue.Count == 0) return defaultSequence;
            _currentSequence = _sequenceQueue.Dequeue();
            return _currentSequence;
        }
    }
}