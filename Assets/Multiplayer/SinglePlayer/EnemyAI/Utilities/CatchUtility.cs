using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI.Utilities
{
    public class CatchUtility : Utility<CatchUtilityArgs>
    {
        public CatchUtility(CatchUtilityArgs args) : base(args)
        {
        }

        public override float Execute(DodgeballAI ai)
        {
            return CalculateCatchUtility(ai);
        }

        public override float Roll(DodgeballAI ai) => CalculateCatchUtility(ai);

        public float CalculateCatchUtility(DodgeballAI ai)
             {
                 float utility = 0;
         
                 foreach (var trajectory in ai.liveBallTrajectories.Values)
                 {
                     RaycastHit hit;
                     if (Physics.Raycast(ai.transform.position, trajectory, out hit))
                     {
                         if (hit.transform != ai.transform) continue; 
         
                         float distance = Vector3.Distance(ai.transform.position, hit.point);
                         Vector3 directionToBall = (hit.point - ai.transform.position).normalized;
                         float dotProduct = Vector3.Dot(ai.transform.forward, directionToBall);
         
                         if (dotProduct > args.FOVThreshold) 
                         {
                             if (distance > args.catchRegisterDistance) 
                             {
                                 utility += (1.0f / distance) * ai.difficultyFactor;
         
                                 if (IsTeammateInLineOfSight(hit.point, ai))
                                 {
                                     utility *= args.utilityMultiplier; // Lower the utility if a teammate is in the way
                                 }
                             }
                         }
                     }
                 }
         
                 return utility;
             }
    }
}