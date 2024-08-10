﻿using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI.Utilities
{
    public class MoveUtility : Utility<MoveUtilityArgs>
    {
        private Vector3 currentTargetPosition;
        private float changeTargetCooldown;
        private float lastTargetChangeTime;
        private Vector3 noiseOffset;
        private float backoffDuration = .8f;
        private float backoffStartTime;

        public MoveUtility(MoveUtilityArgs args) : base(args)
        {
            noiseOffset = new Vector3(Random.value, 0, Random.value) * 10f;
            changeTargetCooldown = Random.Range(0.1f, 1.5f);
            ScheduleNextMove();
        }

        private void ScheduleNextMove()
        {
            _nextMoveTime = Time.time + Random.Range(args.moveIntervalMin, args.moveIntervalMax);
        }

        public bool BackOff(DodgeballAI ai)
        {
            // Check if the AI is already at the edge of its play area
            if (IsAtEdgeOfPlayArea(ai))
            {
                backoffStartTime = 0; // Reset backoff start time
                return false; // Exit backoff state
            }

            if (backoffStartTime == 0) backoffStartTime = Time.time;
            var direction = (ai.transform.position - ai.opposingTeam.playArea.transform.position).normalized;
            var backoffPosition = ai.transform.position + direction * 5f;
            backoffPosition = ClampPositionToPlayArea(backoffPosition, ai.playArea, ai.team);

            MoveTowards(ai, backoffPosition);

            if (Time.time < backoffStartTime + backoffDuration) return true;
            backoffStartTime = 0;
            return false;
        }

        private bool IsAtEdgeOfPlayArea(DodgeballAI ai)
        {
            var playAreaBounds = new Bounds(ai.friendlyTeam.playArea.position,
                new Vector3(ai.friendlyTeam.playArea.localScale.x, 1, ai.friendlyTeam.playArea.localScale.z));

            var position = ai.transform.position;
            position.y = playAreaBounds.center.y; 

            var threshold = 0.5f;
            var isAtEdgeX = Mathf.Abs(position.x - playAreaBounds.min.x) < threshold ||
                            Mathf.Abs(position.x - playAreaBounds.max.x) < threshold;
            var isAtEdgeZ = Mathf.Abs(position.z - playAreaBounds.min.z) < threshold ||
                            Mathf.Abs(position.z - playAreaBounds.max.z) < threshold;

            return isAtEdgeX || isAtEdgeZ;
        }


        private float _pickupCheckStep = 1f;
        private float _pickupCheckTime;

        internal bool PickupMove(DodgeballAI ai)
        {
            if (ai.targetUtility.CurrentTarget.layer != LayerMask.NameToLayer("Ball")) return false;
            if (ai.hasBall) return false;
            if (ai.targetUtility.CurrentTarget.GetComponent<DodgeBall>()._ballState == BallState.Possessed)
                return false;
            if (ai.targetUtility.CurrentTarget.transform.position.y > 1.6f) return false;
            if (!IsInPlayArea(ai.targetUtility.CurrentTarget.transform.position, ai.friendlyTeam.playArea, ai.team))
            {
                return false;
            }

            MoveTowards(ai, ai.targetUtility.CurrentTarget.transform.position);

            if (Time.time < _pickupCheckTime) return true;
            foreach (var player in ai.opposingTeam.actors)
            {
                var dbai = player.GetComponent<DodgeballAI>();
                if (dbai != null && dbai.CurrentTarget != null && dbai.CurrentTarget == ai.CurrentTarget)
                {
                    if (Vector3.Distance(dbai.transform.position, ai.CurrentTarget.transform.position) <
                        Vector3.Distance(ai.transform.position, ai.CurrentTarget.transform.position))
                    {
                        return false;
                    }
                }
            }

            _pickupCheckStep = Random.Range(0.5f, 1.5f);
            _pickupCheckTime = Time.time + _pickupCheckStep;
            return true;
        }

        // if the target is gone or we don't have a ball, return false to switch to idle
        internal bool PossessionMove(DodgeballAI ai)
        {
            var targetAI = ai.targetUtility.CurrentTarget.GetComponent<Actor>();
            if (!targetAI && !ai.CurrentTarget.GetComponentInParent<Actor>())
            {
                // we need to refresh the target for a better one
                ai.targetUtility.PossessedBall(ai);
                PickupMove(ai);
            }

            // implement better possession moving logic in possession utility
            FlockMove(ai);

            if (ai.hasBall) return true;
            return false;
        }

        private float _nextMoveTime;
        private static readonly int _SXAxis = Animator.StringToHash("xAxis");
        private static readonly int _SYAxis = Animator.StringToHash("yAxis");

        public override float Execute(DodgeballAI ai)
        {
            if (ai.currentState == DodgeballAI.AIState.OutOfPlay) return 0f;
            FlockMove(ai);
            if (Time.time >= _nextMoveTime) ScheduleNextMove();
            return 1f;
        }


        public override float Roll(DodgeballAI ai)
        {
            if (ai.currentState == DodgeballAI.AIState.OutOfPlay) return 0f;
            if (Time.time >= _nextMoveTime)
            {
                ScheduleNextMove();
                return 1f; // High priority when it's time to move
            }

            return 0.1f;
        }

        private void FlockMove(DodgeballAI ai)
        {
            if (Time.time > lastTargetChangeTime + changeTargetCooldown)
            {
                changeTargetCooldown = Random.Range(0.3f, 2.5f);
                lastTargetChangeTime = Time.time;
                Vector3 randomDirection = Random.insideUnitSphere;
                randomDirection.y = 0;
                currentTargetPosition = ai.transform.position + randomDirection * args.moveSpeed;
                currentTargetPosition = ClampPositionToPlayArea(currentTargetPosition, ai.playArea, ai.team);
            }

            var separation = Vector3.zero;
            var alignment = Vector3.zero;
            var cohesion = Vector3.zero;
            var centerAttraction = Vector3.zero;
            var neighborCount = 0;

            foreach (var teammate in ai.friendlyTeam.actors)
            {
                if (teammate == ai.gameObject) continue;

                var distance = Vector3.Distance(ai.transform.position, teammate.transform.position);
                if (distance < 0.1f) continue; // Prevent division by zero or extremely close neighbors
                if (distance < args.separationDistance)
                {
                    separation += (ai.transform.position - teammate.transform.position).normalized / distance;
                }

                alignment += teammate.transform.forward;
                cohesion += teammate.transform.position;
                neighborCount++;
            }

            if (neighborCount > 0)
            {
                alignment /= neighborCount;
                cohesion = (cohesion / neighborCount - ai.transform.position).normalized;

                var playAreaCenter = ai.friendlyTeam.playArea.position;
                centerAttraction = (playAreaCenter - ai.transform.position).normalized;

                var flockingMove = (separation * args.separationWeight + alignment * args.alignmentWeight +
                                    cohesion * args.cohesionWeight + (centerAttraction * args.centerAffinity))
                    .normalized;

                currentTargetPosition += flockingMove * args.moveSpeed * Time.deltaTime;
                currentTargetPosition = ClampPositionToPlayArea(currentTargetPosition, ai.playArea, ai.team);
            }

            // Add smooth random movement to avoid stagnation
            noiseOffset += new Vector3(Time.deltaTime, 0, Time.deltaTime);
            var noise = new Vector3(Mathf.PerlinNoise(noiseOffset.x, 0), 0, Mathf.PerlinNoise(0, noiseOffset.z)) -
                        Vector3.one * 0.5f;
            currentTargetPosition += noise * args.randomnessFactor;
            currentTargetPosition = ClampPositionToPlayArea(currentTargetPosition, ai.playArea, ai.team);

            // Smoothly move towards the target position with a slight slowdown as the AI gets closer
            MoveTowards(ai, currentTargetPosition);
        }

        private Vector2 _animBlendTreeAxis;
        private void MoveTowards(DodgeballAI ai, Vector3 targetPosition)
        {
            targetPosition = ClampPositionToPlayArea(targetPosition, ai.playArea, ai.team);
            targetPosition.y = ai.transform.position.y;

            float distanceToTarget = Vector3.Distance(ai.transform.position, targetPosition);
            float stopDistance = args.predictiveStopDistance;
            float speed = args.moveSpeed;

            if (distanceToTarget < stopDistance)
            {
                speed *= Mathf.SmoothStep(0f, 1f, distanceToTarget / stopDistance);
            }
            
            var previousPosition = ai.transform.position;
            ai.transform.position = Vector3.MoveTowards(ai.transform.position, targetPosition, speed * Time.deltaTime);

            var movementDirection = ai.transform.InverseTransformDirection(ai.transform.position - previousPosition).normalized;
            movementDirection *= speed * args.blendMultiplier;

            _animBlendTreeAxis.x = Mathf.Lerp(_animBlendTreeAxis.x, movementDirection.x, args.blendSpeed * Time.deltaTime);
            ai.animator.SetFloat(_SXAxis, Mathf.Clamp(_animBlendTreeAxis.x, -1f, 1f));

            _animBlendTreeAxis.y = Mathf.Lerp(_animBlendTreeAxis.y, movementDirection.z, args.blendSpeed * Time.deltaTime);
            ai.animator.SetFloat(_SYAxis, Mathf.Clamp(_animBlendTreeAxis.y, -1f, 1f));

        }


        private static Vector3 ClampPositionToPlayArea(Vector3 position, DodgeballPlayArea playArea, Team team)
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

        public void ResetBackOff()
        {
            backoffStartTime = 0;
        }
    }
}