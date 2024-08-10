using System.Collections;
using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI.Utilities
{
    public class PickUpUtility : Utility<PickUpUtilityArgs>
    {
        private DodgeballAI _ai;
        public PickUpUtility(PickUpUtilityArgs args, DodgeballAI ai) : base(args)
        {
            _ai = ai;
        }

        public override float Execute(DodgeballAI ai)
        {
            // Check conditions for picking up the ball
            if (ai.CurrentTarget == null || ai.CurrentTarget.layer != LayerMask.NameToLayer("Ball") ||
                ai.hasBall || ai.IsOutOfPlay())
                return 0;

            if (IsTeammateCloserToBall(ai))
            {
                var pickupSuccess = Random.Range(0, 1f);
                if (pickupSuccess < 0.1f)
                {
                    ApproachBallToPickUp(ai.CurrentTarget.GetComponent<DodgeBall>(), ai);
                    return 0;
                }
                
            }

            if (Vector3.Distance(ai.transform.position, ai.CurrentTarget.transform.position) <
                args.pickupDistanceThreshold)
            {
                ai.PickUpBall(ai.CurrentTarget.GetComponent<DodgeBall>());
                return 1f;
            }

            ApproachBallToPickUp(ai.CurrentTarget.GetComponent<DodgeBall>(), ai);
            return CalculatePickUpUtility(ai);
        }

        private bool pickup;
        private bool isLerpingBackToIdle;

        internal void Update()
        {
            if (isLerpingBackToIdle) LerpBackToIdleUpdate(_ai);
        }
        private void LerpBackToIdleUpdate(DodgeballAI ai)
        {
            var newY = Mathf.Lerp(ai.transform.position.y, 0.11f, Time.deltaTime * args.lerpBackSpeed);
            var pos = ai.transform.position;
            pos.y = newY;
            ai.transform.position = pos;

            args.ik.solvers.spine.SetIKPositionWeight(Mathf.Lerp(args.ik.solvers.spine.GetIKPositionWeight(), 0, Time.deltaTime * args.lerpBackSpeed));
            args.ik.solvers.rightHand.SetIKPositionWeight(Mathf.Lerp(args.ik.solvers.rightHand.GetIKPositionWeight(), 0, Time.deltaTime * args.lerpBackSpeed));
            args.ik.solvers.rightHand.maintainRotationWeight = Mathf.Lerp(args.ik.solvers.spine.GetIKPositionWeight(), 0, Time.deltaTime * args.lerpBackSpeed);
    
          if (args.ik.solvers.rightHand.GetIKPositionWeight() <= 0 || args.ik.solvers.spine.GetIKPositionWeight() <= 0 || args.ik.solvers.rightHand.maintainRotationWeight <= 0)
            {
                args.ik.solvers.spine.SetIKPositionWeight(0);
                args.ik.solvers.rightHand.SetIKPositionWeight(0);
                args.ik.solvers.rightHand.maintainRotationWeight = 0;
                isLerpingBackToIdle = false; 
            }
        }
        public void StopPickup(DodgeballAI ai)
        {
            if (!pickup && args.ik.solvers.rightHand.GetIKPositionWeight() == 0) return;
            pickup = false;
            // ai.StartCoroutine(LerpBackToIdle(ai));
            isLerpingBackToIdle = true;
            // Debug.Log("Stop Pickup");
        }
        
        private IEnumerator LerpBackToIdle(DodgeballAI ai)
        {
            while (args.ik.solvers.rightHand.GetIKPositionWeight() > 0f)
            {
                var newY = Mathf.Lerp(ai.transform.position.y, 0.11f, Time.deltaTime * 3);
                var pos = ai.transform.position;
                pos.y = newY;
                ai.transform.position = pos;

                var spineSolver = args.ik.solvers.spine.GetIKPositionWeight();
                args.ik.solvers.spine.SetIKPositionWeight(Mathf.Lerp(spineSolver, 0, Time.deltaTime * 3));

                var rightHandSolver = args.ik.solvers.rightHand.GetIKPositionWeight();
                args.ik.solvers.rightHand.SetIKPositionWeight(Mathf.Lerp(rightHandSolver, 0, Time.deltaTime * 3));

                var spinePosWeight = args.ik.solvers.spine.GetIKPositionWeight();
                args.ik.solvers.rightHand.maintainRotationWeight = Mathf.Lerp(spinePosWeight, 0, Time.deltaTime * 3);
                yield return null;
            }

            args.ik.solvers.spine.SetIKPositionWeight(0);
            args.ik.solvers.rightHand.SetIKPositionWeight(0);
            args.ik.solvers.rightHand.maintainRotationWeight = 0;
        }

        private void ApproachBallToPickUp(DodgeBall dodgeBall, DodgeballAI ai)
        {
            pickup = true;
            var distance = Vector3.Distance(ai.transform.position, dodgeBall.transform.position);
            var lerpFactor = Mathf.Clamp01((distance / args.pickupDistanceThreshold) - 1);

            var position = ai.transform.position;
            position.y = Mathf.Lerp(-0.27f, 0.11f, lerpFactor);
            ai.transform.position = position;

            args.ik.solvers.spine.target = dodgeBall.transform;
            args.ik.solvers.spine.SetIKPositionWeight(Mathf.Lerp(0.063f, 0, lerpFactor));

            args.ik.solvers.rightHand.target = ai.CurrentTarget.transform;
            args.ik.solvers.rightHand.SetIKPositionWeight(Mathf.Lerp(1, 0, lerpFactor));
            args.ik.solvers.rightHand.maintainRotationWeight = Mathf.Lerp(0.4f, 0, lerpFactor);
        }

        public override float Roll(DodgeballAI ai) => CalculatePickUpUtility(ai);
        
        internal float CalculatePickUpUtility(DodgeballAI ai)
        {
            if (ai.CurrentTarget == null || ai.CurrentTarget.layer != LayerMask.NameToLayer("Ball") ||
                ai.hasBall || ai.IsOutOfPlay())
            {
                return 0;
            }

            GameObject nearestBall = FindNearestBallInPlayArea(ai.playArea, ai);
            if (nearestBall == null || !nearestBall.activeInHierarchy)
            {
                return 0;
            }

            var ball = nearestBall.GetComponent<DodgeBall>();

            if (!IsInPlayArea(nearestBall.transform.position, ai.friendlyTeam.playArea, ai.team))
            {
                return 0f;
            }

            float utility = 0;
            if (ball._ballState != BallState.Dead)
            {
                // todo, check trajectory for catch should handle this
                return 0f;
            }
            utility += 5f;

            var distance = Vector3.Distance(ai.transform.position, nearestBall.transform.position);
            utility += (1.0f / distance) * ai.distanceWeight;

            utility += Random.value * ai.difficultyFactor;

            return utility;
        }

        private bool IsTeammateCloserToBall(DodgeballAI ai)
        {
            foreach (var teammate in ai.friendlyTeam.actors)
            {
                if (teammate == ai.gameObject) continue;

                var teammateAI = teammate.GetComponent<DodgeballAI>();
                if (teammateAI == null) continue;

                if (teammateAI.CurrentTarget == ai.CurrentTarget &&
                    Vector3.Distance(teammate.transform.position, ai.CurrentTarget.transform.position) <
                    Vector3.Distance(ai.transform.position, ai.CurrentTarget.transform.position))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
