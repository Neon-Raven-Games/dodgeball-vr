using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI.Utilities
{
    public class OutOfPlayUtility : Utility<OutOfPlayUtilityArgs>
    {
        private bool _finished;

        public OutOfPlayUtility(OutOfPlayUtilityArgs args) : base(args)
        {
        }

        // does not need to return a value
        public override float Execute(DodgeballAI ai)
        {
            HandleOutOfPlayState(ai);
            return 0f;
        }

        public override float Roll(DodgeballAI ai)
        {
            if (!_finished) return 0f;
            
            // will exit out of play state on ai
            _finished = false;
            return 1f;

        }

        private void HandleOutOfPlayState(Actor ai)
        {
            if (ai.IsInPlayArea(ai.transform.position))
            {
                Vector3 closestOutOfBounds = ai.friendlyTeam.outOfBounds.position;
                var direction = (closestOutOfBounds - ai.transform.position).normalized;
                if (direction != Vector3.zero) ai.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
                
                closestOutOfBounds.y = ai.transform.position.y;
                ai.transform.position = Vector3.MoveTowards(ai.transform.position, closestOutOfBounds,
                    Time.deltaTime * 3f); // Adjust the speed as needed
                ai.outOfBoundsEndTime = Time.time + args.outOfBoundsWaitTime;
            }
            else
            {
                var direction = (ai.friendlyTeam.playArea.position - ai.transform.position).normalized;
                if (direction != Vector3.zero) ai.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }
            
            if (Time.time >= ai.outOfBoundsEndTime) _finished = true;
        }
    }
}