using System;
using Hands.SinglePlayer.EnemyAI.Abilities;
using Hands.SinglePlayer.EnemyAI.Priority;
using Hands.SinglePlayer.EnemyAI.StatefulRefactor;
using RNGNeeds;
using UnityEngine;
using UnityEngine.Serialization;

namespace Hands.SinglePlayer.EnemyAI
{
    public abstract class Utility<T> where T : UtilityArgs
    {
        protected T args;
        internal Bounds playAreaBounds;
        private RaycastHit hit;

        protected Utility(T args, AIState state)
        {
            this.args = args;
            State = state;
        }

        public AIState State { get; }

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
                    if (teammateAI != null && teammateAI.currentState == "PickUp" &&
                        teammateAI.IsTargetingBall(ball))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        protected bool IsInPlayArea(Vector3 position)
        {
            bool inBounds = playAreaBounds.Contains(position);
            return inBounds;
        }

        protected DodgeBall FindNearestBallInPlayArea(DodgeballPlayArea playArea, DodgeballAI ai, out float distance)
        {
            distance = 200;
            
            DodgeBall nearestBall = null;
            var nearestDistance = float.MaxValue;

            foreach (var ball in playArea.dodgeBalls.Keys)
            {
                if (!IsInPlayArea(ball.transform.position) || !ball.gameObject.activeInHierarchy) continue;
                
                var ballPos = ball.transform.position;
                ballPos.y = ai.transform.position.y;
                distance = Vector3.Distance(ai.transform.position, ballPos);

                if (distance > nearestDistance) continue;
                    
                nearestDistance = distance;
                nearestBall = ball;
            }

            return nearestBall;
        }

        protected static Vector3 ClampPositionToPlayArea(Vector3 position, DodgeballPlayArea playArea, Team team)
        {
            Bounds playAreaBounds;
            if (team == Team.TeamOne)
            {
                playAreaBounds = new Bounds(playArea.team1PlayArea.position,
                    new Vector3(playArea.team1PlayArea.localScale.x, 1, playArea.team1PlayArea.localScale.z));
            }
            else
            {
                playAreaBounds = new Bounds(playArea.team2PlayArea.position,
                    new Vector3(playArea.team2PlayArea.localScale.x, 1, playArea.team2PlayArea.localScale.z));
            }

            position.x = Mathf.Clamp(position.x, playAreaBounds.min.x, playAreaBounds.max.x);
            position.z = Mathf.Clamp(position.z, playAreaBounds.min.z, playAreaBounds.max.z);
            return position;
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

    public abstract class UtilityArgs
    {
        public abstract int state { get; }
    }

    [Serializable]
    public class FakeoutUtilityArgs : UtilityArgs
    {
        public GameObject entryEffect;
        public FakeoutBall fakeoutBall;
        public float rollIntervalMin;
        public float rollIntervalMax;
        
        public float entryDuration = 1f;
        public float nextRollTime;
        public ProbabilityList<float> probabilityList;
        public float throwSpeed;
        public override int state => NinjaStruct.FakeOut;
    }

    [Serializable]
    public class NinjaOutOfPlayArgs : UtilityArgs
    {
        public override int state => NinjaStruct.OutOfPlay;
        public float jumpHeight = 18f;
        public float jumpDuration = 0.4f;
        public GameObject trailRenderer;
        public float respawnTime = 1f;
    }
    
    [Serializable]
    public class SmokeBombUtilityArgs : UtilityArgs
    {
        public GameObject shadowStepEffect;
        public float despawnDelay;
        public ColorLerp colorLerp;
        public float playEffectDelay;
        public override int state => NinjaStruct.SmokeBomb;
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
        public override int state => StateStruct.Move;
    }

    [Serializable]
    public class DodgeUtilityArgs : UtilityArgs
    {
        public override int state { get; }
    }

    [Serializable]
    public class CatchUtilityArgs : UtilityArgs
    {
        public float FOVThreshold = 0.5f;
        public float catchRegisterDistance = 5f;
        public float utilityMultiplier = 0.5f;
        public override int state { get; }
    }

    [Serializable]
    public class PossessionArgs : UtilityArgs
    {
        public override int state => StateStruct.Possession;
    }

    [Serializable]
    public class PickUpUtilityArgs : UtilityArgs
    {
        public float pickupDistanceThreshold;
        public float ikDistanceThreshold;
        public float lerpBackSpeed = 3f;
        public float ballPickupHeight = -0.27f;
        public float ballIdleHeight = 0.11f;
        public float spineIKWeight = 0.063f;
        public float maintainRotationWeight = 0.4f;
        public PriorityData priorityData;
        public override int state => StateStruct.PickUp;
    }

    [Serializable]
    public class ThrowUtilityArgs : UtilityArgs
    {
        public ProbabilityList<float> throwProbability;
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
        public float ballThrowRecovery = 0.5f;
        public NetBallPossessionHandler leftBallIndex;
        public NetBallPossessionHandler rightBallIndex;
        public PriorityData priorityData;
        public override int state => StateStruct.Throw;
    }

    [Serializable]
    public class OutOfPlayUtilityArgs : UtilityArgs
    {
        public float outOfBoundsWaitTime = 3f;
        public override int state => StateStruct.OutOfPlay;
    }

    [Serializable]
    public class ShadowStepUtilityArgs : UtilityArgs
    {
        public ColorLerp colorLerp;
        
        public float rollChance = 60f;
        public float shadowStepCooldown = 10f;
        
        [Header("Movement Properties")]
        public float stepDistance = 5f;
        public float stepDuration;
        public Vector3 stepDirection = Vector3.forward;
        
        [Header("Entry Properties")] 
        public GameObject entryEffect;
        public GameObject floorSmoke;
        public AnimationClip introAnimationClip;
        public AnimationCurve entryCurve;
        public float introColorLerpValue;
        
        [Header("Exit Properties")]
        public AnimationCurve exitCurve;
        public float exitDuration;
        public GameObject exitEffect;
        public AnimationClip outroAnimationClip;
        public float outroColorLerpValue;
        public int outroColorFrame;
        public int outroThrowFrame;
        public override int state => NinjaStruct.ShadowStep;
    }
    
    [Serializable] 
    public class SubstitutionUtilityArgs : UtilityArgs
    {
        [Header("Avatar")]
        public Collider collider;
        
        [Header("Exit Effects")]
        public GameObject floorSmoke;
        public GameObject logEffect;
        
        [Header("Entry Effects")]
        public GameObject entryEffect;
        public AnimationCurve exitCurve;
        public ColorLerp colorLerp;
        public float rentryDuration;
        
        [Header("Flags")]
        public bool sequencePlaying;
        public bool ballInTrigger;

        [Header("Args")] 
        public float stepDistance = 5f;
        public float stepDuration;
        
        [Header("Debug and Runtime")]
        public Vector3 stepDirection = Vector3.forward;

        public override int state => NinjaStruct.Substitution;
    }

    [Serializable]
    public class NinjaHandSignUtilityArgs : UtilityArgs
    {
        public ProbabilityList<float> handSignProbability;
        public float handSignCooldown = 5f;
        public float handSignDebugRoll = 75f;
        public Transform handSignTarget;
        public Animator handAnimator;
        public Collider collider;
        public float nextHandSignTime;
        public override int state => NinjaStruct.HandSign;
    }
}