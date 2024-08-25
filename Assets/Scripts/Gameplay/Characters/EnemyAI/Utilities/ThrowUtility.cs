using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI.Utilities
{
    public class ThrowUtility : Utility<ThrowUtilityArgs>, IUtility
    {
        public ThrowUtility(ThrowUtilityArgs args) : base(args, AIState.Throw)
        {
        }

        public override float Execute(DodgeballAI ai)
        {
            ai.moveUtility.Execute(ai);
            return ShouldThrow(ai) ? 1f : 0f;
        }

        public override float Roll(DodgeballAI ai) => CalculateThrowUtility(ai);

        private float CalculateThrowUtility(DodgeballAI ai)
        {
            if (!ai.hasBall) return 0;
            var utility = ai.ballPossessionTime * args.possessionTimeWeight;
            if (ai.ActorTarget)
            {
                var distance = Vector3.Distance(ai.transform.position, ai.ActorTarget.transform.position);
                utility += (1.0f / distance) * args.maxThrowDistance;
            }

            if (IsTargetInLineOfSight(ai)) utility += args.lineOfSightWeight;
            utility += Random.value * ai.difficultyFactor;

            return utility;
        }

        private bool ShouldThrow(DodgeballAI ai)
        {
            if (!ai.hasBall) return false;
            if (!ai.CurrentTarget || !ai.ActorTarget) return false;

            var utility = args.possessionTimeWeight * ai.ballPossessionTime;
            var distance = Vector3.Distance(ai.transform.position,
                ai.ActorTarget.transform.position);
            
            utility += (1.0f / distance) * args.maxThrowDistance;

            // todo, add randomness here
            // todo priority for line of sight
            
            // todo, we have around 0.3ms physics without raycast and 0.6ms with raycast
            
            var throwing = utility > 4f;
            var mask = LayerMask.NameToLayer(ai.opposingTeam.layerName);
            var direction = (ai.ActorTarget.transform.position - ai.transform.position).normalized;

            if (throwing && Physics.RaycastNonAlloc(ai.transform.position, direction, _hits, distance, mask) > 0)
                return true;

            return false;
        }

        private readonly RaycastHit[] _hits = new RaycastHit[1];

        public Vector3 CalculateThrow(DodgeballAI dodgeballAI, Vector3 source, Vector3 target)
        {
            var direction = target - source;

            // do we need upward bias still?
            direction.y += args.upwardBias;
            direction.x += Random.Range(-args.aimRandomnessFactor, args.aimRandomnessFactor);
            direction.y += Random.Range(-args.aimRandomnessFactor, args.aimRandomnessFactor);
            direction.z += Random.Range(-args.aimRandomnessFactor, args.aimRandomnessFactor);

            var throwForce = direction.normalized * CalculateThrowForce(dodgeballAI, direction.magnitude);
            return throwForce;
        }

        private float CalculateThrowForce(DodgeballAI dodgeballAI, float distance)
        {
            var baseForce = args.testingThrowForce;

            var difficultyAdjustment = dodgeballAI.difficultyFactor * args.difficultyThrowForceMultiplier;
            var distanceAdjustment = Mathf.Clamp(distance / args.maxThrowDistance, 0.5f, 1.0f);
            var randomness = Random.Range(-args.throwForceRandomness, args.throwForceRandomness);
            var throwForce = baseForce + difficultyAdjustment + (distanceAdjustment * baseForce) + randomness;
            throwForce = Mathf.Clamp(throwForce, args.minThrowForce, args.maxThrowForce);

            return throwForce;
        }
    }
}