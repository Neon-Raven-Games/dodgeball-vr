using System;
using RootMotion.FinalIK;
using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI
{
    public abstract class Utility<T> where T : UtilityArgs
    {
        protected T args;

        private Bounds playAreaBounds;
        private RaycastHit hit;
        protected Utility(T args, DodgeballAI.AIState state)
        {
            this.args = args;
            State = state;
        }

        public DodgeballAI.AIState State { get;}

        // this will be called by the AI class in update loop
        public abstract float Execute(DodgeballAI ai);
        public abstract float Roll(DodgeballAI ai);

        // this will be overridden on the implementation if animation data present
        public virtual void Initialize(Transform playArea, Team team)
        {
            if (team == Team.TeamOne)
            {
                playAreaBounds = new Bounds(playArea.position,
                    new Vector3(playArea.localScale.x, 5, playArea.localScale.z));
            }
            else
            {
                playAreaBounds = new Bounds(playArea.position,
                    new Vector3(playArea.localScale.x, 5, playArea.localScale.z));
            }
        }

        protected bool IsTeammateInLineOfSight(Vector3 hitPoint, DodgeballAI ai)
        {
            foreach (var teammate in ai.friendlyTeam.actors)
            {
                if (teammate != ai.gameObject)
                {
                    if (Physics.Raycast(ai.transform.position, hitPoint - ai.transform.position, out hit))
                    {
                        if (hit.transform == teammate.transform)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        protected bool IsTeammateTargetingBall(GameObject ball, DodgeballPlayArea playArea, DodgeballAI ai)
        {
            // Implement logic to check if any teammates are targeting the same ball
            foreach (var teammate in playArea.team1Actors) // Adjust for the correct team
            {
                if (teammate != ai.gameObject)
                {
                    DodgeballAI teammateAI = teammate.GetComponent<DodgeballAI>();
                    if (teammateAI != null && teammateAI.currentState == DodgeballAI.AIState.PickUp &&
                        teammateAI.IsTargetingBall(ball))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        protected bool IsInPlayArea(Vector3 position) =>
            playAreaBounds.Contains(position);

        protected GameObject FindNearestBallInPlayArea(DodgeballPlayArea playArea, DodgeballAI ai, out float distance)
        {
            distance = 200;
            // find the nearest ball within the play area
            GameObject nearestBall = null;
            float nearestDistance = float.MaxValue;

            foreach (var ball in playArea.dodgeBalls)
            {
                if (IsInPlayArea(ball.transform.position))
                {
                    distance = Vector3.Distance(ai.transform.position, ball.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestBall = ball;
                    }
                }
            }

            return nearestBall;
        }

        protected bool IsTargetInLineOfSight(DodgeballAI ai)
        {
            if (ai.CurrentTarget == null)
                return false;

            if (!Physics.Raycast(ai.transform.position, ai.CurrentTarget.transform.position - ai.transform.position,
                    out hit)) return false;

            return hit.transform == ai.CurrentTarget.transform;
        }
    }

    public class UtilityArgs
    {
    }

    [Serializable]
    public class MoveUtilityArgs : UtilityArgs
    {
        public float moveSpeed;
        public float randomnessFactor = 0.1f;
        public float separationDistance = 2f;
        public float alignmentWeight = 1f;
        public float cohesionWeight = 1f; 
        public float separationWeight = 1.5f;
        public float moveIntervalMin = 2f;
        public float moveIntervalMax = 5f;
        public float centerAffinity = 2.5f;
        public float blendSpeed = 20;
        public float blendMultiplier = 1;
        public float predictiveStopDistance = 0.3f;
    }

    [Serializable]
    public class DodgeUtilityArgs : UtilityArgs
    {
    }

    [Serializable]
    public class CatchUtilityArgs : UtilityArgs
    {
        public float FOVThreshold = 0.5f;
        public float catchRegisterDistance = 5f;
        public float utilityMultiplier = 0.5f;
    }

    [Serializable]
    public class PickUpUtilityArgs : UtilityArgs
    {
        public float pickupDistanceThreshold;
        public BipedIK ik;
        public float lerpBackSpeed = 3f;
    }

    [Serializable]
    public class ThrowUtilityArgs : UtilityArgs
    {
        public float lineOfSightWeight = 1.0f;
        public float possessionTimeWeight = 1.0f;
        public float testingThrowForce;
        public float aimRandomnessFactor;
        public float upwardBias = 0.5f;
        public float difficultyThrowForceMultiplier = 1;
        public float maxThrowDistance = 40;
        public float throwForceRandomness;
        public float minThrowForce;
        public float maxThrowForce = 40;
    }

    [Serializable]
    public class OutOfPlayUtilityArgs : UtilityArgs
    {
        // == out of bounds ==
        // Time to wait when out of bounds
        public float outOfBoundsWaitTime = 3f;
    }
    
    [Serializable]
    public class ShadowStepUtilityArgs : UtilityArgs
    {
        public float stepDistance = 5f;
        public float stepDuration;
        public Vector3 stepDirection = Vector3.forward;
        public AnimationCurve entryCurve;
        public AnimationCurve exitCurve;
        public float entrySpeed;
        public float exitSpeed;
        public float exitDuration;
    }
}