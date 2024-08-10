using Hands.SinglePlayer.EnemyAI;
using UnityEngine;

public class DodgeUtility : Utility<DodgeUtilityArgs>, IUtility
{
    public DodgeUtility(DodgeUtilityArgs args) : base(args, DodgeballAI.AIState.Dodge)
    {
    }

    public override float Execute(DodgeballAI ai)
    {
        return CalculateDodgeUtility(ai);
    }

    public override float Roll(DodgeballAI ai) => CalculateDodgeUtility(ai);

    internal float CalculateDodgeUtility(DodgeballAI ai)
    {
        float utility = 0;

        foreach (var trajectory in ai.liveBallTrajectories.Values)
        {
            RaycastHit hit;
            if (Physics.Raycast(ai.transform.position, trajectory, out hit))
            {
                if (hit.transform != ai.transform) continue; // Skip if hit something else

                var distance = Vector3.Distance(ai.transform.position, hit.point);
                var directionToBall = (hit.point - ai.transform.position).normalized;
                var dotProduct = Vector3.Dot(ai.transform.forward, directionToBall);

                if (dotProduct > 0.5f) // Adjust FOV threshold as needed
                {
                    if (distance < 5f) // Adjust distance threshold as needed
                    {
                        utility += (1.0f / (distance + 1)) *
                                   ai.difficultyFactor; // Inverse distance, add 1 to avoid division by zero

                        if (IsTeammateInLineOfSight(hit.point, ai))
                        {
                            utility *= 0.5f; // Lower the utility if a teammate is in the way
                        }
                    }
                }
            }
        }

        return utility;
    }
}
