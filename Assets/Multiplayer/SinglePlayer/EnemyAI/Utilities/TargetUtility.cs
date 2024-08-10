using System;
using System.Collections;
using Hands.SinglePlayer.EnemyAI.Priority;
using RootMotion.FinalIK;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Hands.SinglePlayer.EnemyAI
{
    [Serializable]
    public class TargetUtilityArgs : UtilityArgs
    {
        public float minSwitchProbability = 0.1f;
        public float maxSwitchProbability = 0.9f;
        public float switchProbabilityIncreaseRate = 0.02f;
        public float minimumSwitchTime = 5f;
        public float difficultyWeight = 0.5f;
        public float dodgeballProximityThreshold = 5f;
        public float headTurnSpeed = 2f;
        public float bodyTurnSpeed = 1f;
        public float fovAngle = 120f;
        public BipedIK ik;
    }

    // Priority types for target utility
    public class TargetUtility : Utility<TargetUtilityArgs>
    {
        public GameObject CurrentTarget { get; private set; }
        public Actor ActorTarget { get; private set; }
        public DodgeBall BallTarget { get; private set; }
        private GameObject _lastBestTarget;

        private readonly float _minSwitchProbability;
        private readonly float _maxSwitchProbability;
        private readonly float _switchProbabilityIncreaseRate;
        private readonly float _minimumSwitchTime;
        private readonly ActorTeam _enemyTeam;
        private readonly DodgeballPlayArea _playArea;
        private readonly float _difficultyWeight;
        private float _dodgeballProximityThreshold;
        private float _lastTargetChangeTime;
        private float _targetSwitchProbability;
        private readonly DodgeballAI _ai;

        private PriorityData _priorityData;

        private float GetPriority(PriorityType type) =>
            _priorityData.GetPriorityValue(type);

        public TargetUtility(TargetUtilityArgs arg, DodgeballAI ai, PriorityData data) : base(arg)
        {
            _priorityData = data;
            _ai = ai;
            _playArea = ai.playArea;
            _enemyTeam = ai.opposingTeam;
            CurrentTarget = _enemyTeam.actors[Random.Range(0, _enemyTeam.actors.Count)];
            _lastBestTarget = CurrentTarget;
            ActorTarget = CurrentTarget.GetComponent<Actor>();

            _minSwitchProbability = arg.minSwitchProbability;
            _maxSwitchProbability = arg.maxSwitchProbability;
            _switchProbabilityIncreaseRate = arg.switchProbabilityIncreaseRate;
            _minimumSwitchTime = arg.minimumSwitchTime;
            _difficultyWeight = arg.difficultyWeight;
            _dodgeballProximityThreshold = arg.dodgeballProximityThreshold;
            ResetTargetSwitchProbability();
        }

        public override float Execute(DodgeballAI ai)
        {
            LookAtTarget(CurrentTarget.transform.position);
            return 1f;
        }

        private float _pickupCheckTime;
        private float _pickupCheckStep = 0.4f;

        public override float Roll(DodgeballAI ai)
        {
            LookAtTarget(CurrentTarget.transform.position);

            if (CurrentTarget.activeInHierarchy && CurrentTarget.layer == LayerMask.NameToLayer("Ball"))
            {
                if (!BallTarget) BallTarget = CurrentTarget.GetComponent<DodgeBall>();
                if (BallTarget)
                {
                    if (!IsInPlayArea(BallTarget.transform.position))
                    {
                        ai._pickUpUtility.StopPickup(ai);
                        _lastBestTarget = FindBestTarget();
                        return CalculateTargetScore(_lastBestTarget);
                    }

                    if (BallTarget._ballState == BallState.Dead) return 1f;
                }
            }
            else
            {
                BallTarget = null;
            }


            if (!CurrentTarget.activeInHierarchy)
            {
                ai._pickUpUtility.StopPickup(ai);
                _lastBestTarget = FindBestTarget();
            }

            if (_lastBestTarget && Time.time < _pickupCheckTime) return CalculateTargetScore(_lastBestTarget);
            IncrementPickupStepCheck();
            _lastBestTarget = FindBestTarget();
            return CalculateTargetScore(_lastBestTarget);
        }

        public void PossessedBall(DodgeballAI ai)
        {
            IncrementPickupStepCheck();
            _lastBestTarget = FindBestTarget();
            SwitchTarget();
        }

        private void IncrementPickupStepCheck()
        {
            _pickupCheckStep = Random.Range(0.2f, 0.4f);
            _pickupCheckTime = Time.time + _pickupCheckStep;
        }

        public void UpdateTarget(DodgeballAI.AIState currentState)
        {
            if (currentState == DodgeballAI.AIState.OutOfPlay) return;
            var timeSinceLastSwitch = Time.time - _lastTargetChangeTime;

            _targetSwitchProbability = _minSwitchProbability + (_maxSwitchProbability - _minSwitchProbability) *
                (1 - Mathf.Exp(-_switchProbabilityIncreaseRate * timeSinceLastSwitch));

            if (CurrentTarget == null || !CurrentTarget.activeInHierarchy)
            {
                _lastBestTarget = FindBestTarget();
                CurrentTarget = _lastBestTarget;
            }

            if (Random.value < _targetSwitchProbability && Time.time >= _lastTargetChangeTime + _minimumSwitchTime)
            {
                SwitchTarget();
                ResetTargetSwitchProbability();
            }


            if (currentState != DodgeballAI.AIState.Possession &&
                currentState != DodgeballAI.AIState.Throw &&
                currentState != DodgeballAI.AIState.BackOff)
                CheckForNearbyDodgeballs();
        }

        private void CheckForNearbyDodgeballs()
        {
            foreach (var ball in _playArea.dodgeBalls)
            {
                if (!ball.activeInHierarchy) continue;
                if (BallProximity(ball, out var distanceToBall)) continue;
                if (CanNotOverride(distanceToBall)) continue;
                if (!IsInPlayArea(ball.transform.position)) continue;
                var ballstate = ball.GetComponent<DodgeBall>()._ballState;
                if (ballstate != BallState.Dead) continue;
                CurrentTarget = ball;
                ResetTargetSwitchProbability();
                break;
            }
        }

        private bool CanNotOverride(float distanceToBall)
        {
            var overrideProbability = (1 - (distanceToBall / args.dodgeballProximityThreshold)) * _difficultyWeight;
            return Random.value >= overrideProbability;
        }

        private bool BallProximity(GameObject ball, out float distanceToBall)
        {
            distanceToBall = Vector3.Distance(_ai.transform.position, ball.transform.position);
            return distanceToBall >= args.dodgeballProximityThreshold &&
                   IsInPlayArea(ball.transform.position);
        }

        public void ResetTargetSwitchProbability()
        {
            _targetSwitchProbability = _minSwitchProbability;
            _lastTargetChangeTime = Time.time;
        }

        public void SwitchTarget()
        {
            if (_lastBestTarget == null) return;
            CurrentTarget = _lastBestTarget;
        }

        private GameObject FindBestTarget()
        {
            GameObject bestTarget = null;
            float bestScore = float.MinValue;

            foreach (var enemy in _enemyTeam.actors)
            {
                if (enemy != null)
                {
                    var enemyActor = enemy.GetComponent<Actor>();
                    if (!enemyActor)
                    {
                        enemyActor = enemy.transform.GetChild(0).GetComponent<Actor>();
                    }

                    if (enemyActor != null && !enemyActor.IsOutOfPlay())
                    {
                        float score = CalculateTargetScore(enemyActor.gameObject);
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestTarget = enemyActor.gameObject;
                        }
                    }
                }
            }

            if (_ai.hasBall) return bestTarget;
            foreach (var ball in _playArea.dodgeBalls)
            {
                if (ball.activeInHierarchy && ball.GetComponent<DodgeBall>()._ballState == BallState.Dead &&
                    IsInPlayArea(ball.transform.position))
                {
                    float score = CalculateTargetScore(ball);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestTarget = ball;
                    }
                }
            }

            return bestTarget;
        }

        private float CalculateTargetScore(GameObject target)
        {
            var score = 0f;

            if (!target || !target.activeInHierarchy) return -500f;

            var maxDistance = 18f;
            var distance = Vector3.Distance(_ai.transform.position, target.transform.position);
            score -= distance / maxDistance;

            var headPos = _ai.transform.position;
            headPos.y += 1;
            var direction = target.transform.position - headPos;

            if (Physics.Raycast(headPos, direction, out var hit))
            {
                if (hit.collider.gameObject == target) score += GetPriority(PriorityType.LineOfSight);
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Ball"))
                {
                    var ball = hit.collider.gameObject.GetComponent<DodgeBall>();
                    if (!_ai.hasBall) score += GetPriority(PriorityType.PossessedBall);
                    if (_ai.hasBall) score -= GetPriority(PriorityType.PossessedBall);
                    if (IsInPlayArea(target.transform.position))
                    {
                        score += GetPriority(PriorityType.InsidePlayArea);
                        if (ball._ballState == BallState.Dead)
                            score += GetPriority(PriorityType.FreeBall);
                        else if (ball._ballState == BallState.Possessed)
                            score -= GetPriority(PriorityType.Targeted);
                        else score -= GetPriority(PriorityType.OutsidePlayArea);
                    }
                    else score -= GetPriority(PriorityType.OutsidePlayArea);
                }
            }

            if (CurrentTarget != target &&  BallTarget && target.GetComponent<DodgeBall>())
                score -= GetPriority(PriorityType.Targeted);

            if (target.layer == LayerMask.NameToLayer(_enemyTeam.layerName))
            {
                if (_ai.hasBall) score += GetPriority(PriorityType.PossessedBall);
                score += GetPriority(PriorityType.Enemy);
                var enemyAi = hit.collider.GetComponent<Actor>();
                if (enemyAi && enemyAi.hasBall) score += GetPriority(PriorityType.EnemyPossession);
            }

            score += Random.Range(-0.1f, 0.1f);
            if (CurrentTarget == target) score += GetPriority(PriorityType.Targeted);
            return score;
        }

        private bool _debug = true;
        private Color debugColor = new Color(Random.value, Random.value, Random.value);
        float lookWeightTarget = 1f;
        private float _headLookWeight;

        private void LookAtTarget(Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - _ai.transform.position;
            Vector3 flatDirection = new Vector3(direction.x, 0, direction.z).normalized;

            // Calculate the angle to the target
            float angleToTarget = Vector3.Angle(_ai.transform.forward, flatDirection);

            // Determine look weight based on FOV
            if (angleToTarget > args.fovAngle / 2)
            {
                lookWeightTarget = 0.85f; // Lower weight if outside FOV
            }
            else
            {
                lookWeightTarget = 1f; // Full weight if inside FOV
            }

            // Smoothly interpolate the head look weight
            _headLookWeight = Mathf.Lerp(_headLookWeight, lookWeightTarget, Time.deltaTime * args.headTurnSpeed);

            if (_debug)
            {
                Vector3 debugDirection = direction;
                debugDirection.y -= 0.4f;
                Vector3 headPos = _ai.transform.position;
                headPos.y += 1;
                Debug.DrawRay(headPos, debugDirection, debugColor);
            }

            // Set the look target for the IK solver
            var actor = CurrentTarget.GetComponent<Actor>();
            args.ik.solvers.lookAt.target = actor ? actor.head : CurrentTarget.transform;
            args.ik.solvers.lookAt.SetLookAtWeight(_headLookWeight);

            // Rotate the body if the head look weight is high enough
            if (_headLookWeight >= 0.5f)
            {
                Quaternion bodyTargetRotation = Quaternion.LookRotation(flatDirection);
                _ai.transform.rotation = Quaternion.Slerp(_ai.transform.rotation, bodyTargetRotation,
                    Time.deltaTime * args.bodyTurnSpeed);
            }
            else
            {
                var headTargetRotation = Quaternion.LookRotation(flatDirection);
                _ai.transform.rotation = Quaternion.Slerp(_ai.transform.rotation, headTargetRotation,
                    Time.deltaTime * args.bodyTurnSpeed / 2);
            }
        }

        private IEnumerator StopLook()
        {
            while (_headLookWeight > 0.01f)
            {
                _headLookWeight = Mathf.Lerp(_headLookWeight, 0f, Time.deltaTime * args.headTurnSpeed);
                args.ik.solvers.lookAt.SetLookAtWeight(0f);
                yield return null;
            }
        }

        public void ResetLookWeight()
        {
            if (_headLookWeight > 0.01f)
            {
                _ai.StartCoroutine(StopLook());
            }
        }
    }
}