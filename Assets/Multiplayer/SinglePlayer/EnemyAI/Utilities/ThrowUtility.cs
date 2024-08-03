using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI.Utilities
{
    public class ThrowUtility : Utility<ThrowUtilityArgs>
    {
        public ThrowUtility(ThrowUtilityArgs args) : base(args)
        {
        }
        
        public override float Execute(DodgeballAI ai)
        {
            return ShouldThrow(ai) ? 1f : 0f;
        }

        public override float Roll(DodgeballAI ai) => CalculateThrowUtility(ai);

        internal float CalculateThrowUtility(DodgeballAI ai)
        {
            if (!ai.hasBall) return 0;

            float utility = 0;

            // Calculate utility based on how long the AI has had the ball
            utility += ai.ballPossessionTime * args.possessionTimeWeight;

            // Calculate utility based on the distance to the target
            if (ai.CurrentTarget != null)
            {
                float distance = Vector3.Distance(ai.transform.position, ai.CurrentTarget.transform.position);
                utility += (1.0f / distance) * ai.distanceWeight;
            }

            // Calculate utility based on line of sight
            if (IsTargetInLineOfSight(ai))
            {
                utility += args.lineOfSightWeight;
            }

            // Add a random component based on the difficulty factor
            utility += Random.value * ai.difficultyFactor;

            return utility;
        }

        // what is your take on the throwing utility?
        private bool ShouldThrow(DodgeballAI ai)
        {
            if (!ai.CurrentTarget.GetComponent<Actor>())
            {
                Debug.Log($"Current target does not have an actor component{ai.CurrentTarget.name}");
                return false;
            }
            if (!ai.hasBall) return false;
            
            // not the most in love with this, but I think it's mostly because the movement utility is a bit
            // off from how you would expect someone with a dodgeball to behave
            var utility = args.possessionTimeWeight * ai.ballPossessionTime;
            if (ai.CurrentTarget != null)
            {
                var distance = Vector3.Distance(ai.transform.position, ai.CurrentTarget.transform.position);
                utility += (1.0f / distance) * ai.distanceWeight;
            }
            var throwing = utility > 0.5f;
            return throwing;
        }
        
        public Vector3 CalculateThrow(DodgeballAI dodgeballAI, Vector3 source, Vector3 target)
        {
            // is there a more robust way to calculate randomness to throw trajectory?
            // perhaps weighted off of our difficulty factor, and a small chance to flop the throw
            // we can also add a chance to generate more interesting trajectories/predict the target actor's movement
            // at a high roll. We can store a vector on the ai for their last move direction and use that to try to predict?
            var direction = target - source;
            direction.y += args.upwardBias;
            direction.x += Random.Range(-args.aimRandomnessFactor, args.aimRandomnessFactor);
            direction.y += Random.Range(-args.aimRandomnessFactor, args.aimRandomnessFactor);
            direction.z += Random.Range(-args.aimRandomnessFactor, args.aimRandomnessFactor);
            
            var throwForce = direction.normalized * CalculateThrowForce(dodgeballAI);
            return throwForce;
        }

        private float CalculateThrowForce(DodgeballAI dodgeballAI)
        {
            // this is testing purposes only
            // can we develop a more robust way to calculate throw force?
            // perhaps using the difficulty score and some randomness for
            // a low chance to flop the throw, and a chance to generate
            // more interesting trajectories
            return args.testingThrowForce;

        }
    }
}