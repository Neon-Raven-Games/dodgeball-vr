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
            var direction = target - source;
            // can we dynamically adjust the y throw trajectory based on the difficulty score?
            // hard difficulty throws too low at even high difficulty scores
            direction.y += args.upwardBias;
            direction.x += Random.Range(-args.aimRandomnessFactor, args.aimRandomnessFactor);
            direction.y += Random.Range(-args.aimRandomnessFactor, args.aimRandomnessFactor);
            direction.z += Random.Range(-args.aimRandomnessFactor, args.aimRandomnessFactor);
            
            var throwForce = direction.normalized * CalculateThrowForce(dodgeballAI, direction.magnitude);
            return throwForce;
        }

        private float CalculateThrowForce(DodgeballAI dodgeballAI, float distance)
        {
            // Calculate the base throw force
            float baseForce = args.testingThrowForce;

            // Adjust throw force based on difficulty and distance
            float difficultyAdjustment = dodgeballAI.difficultyFactor * args.difficultyThrowForceMultiplier;
            float distanceAdjustment = Mathf.Clamp(distance / args.maxThrowDistance, 0.5f, 1.0f);

            // Introduce randomness for variability
            float randomness = Random.Range(-args.throwForceRandomness, args.throwForceRandomness);

            // Combine adjustments and add randomness
            float throwForce = baseForce + difficultyAdjustment + (distanceAdjustment * baseForce) + randomness;

            // Ensure throw force is within acceptable bounds
            throwForce = Mathf.Clamp(throwForce, args.minThrowForce, args.maxThrowForce);

            return throwForce;
        }
    }
}