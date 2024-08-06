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

// priority types for target utility:
    // EnemyPossession
    // LineOfSight
    // PossessedBall
    // Enemy
    // InsidePlayArea
    // OutsidePlayArea
    // Targeted
    // Freeball
    public class TargetUtility : Utility<TargetUtilityArgs>
    {
        public GameObject CurrentTarget { get; private set; }
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
            return 1f;
        }

        private float _pickupCheckTime;
        private float _pickupCheckStep = 1f;

        public override float Roll(DodgeballAI ai)
        {
            LookAtTarget(CurrentTarget.transform.position);
            if (CurrentTarget.activeInHierarchy && CurrentTarget.layer == LayerMask.NameToLayer("Ball"))
            {
                if (_lastBestTarget && Time.time < _pickupCheckTime) return 1f;
                if (CurrentTarget.GetComponent<DodgeBall>()._ballState == BallState.Dead) return 1f;
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

            // Use an exponential decay function for target switch probability
            _targetSwitchProbability = _minSwitchProbability + (_maxSwitchProbability - _minSwitchProbability) *
                (1 - Mathf.Exp(-_switchProbabilityIncreaseRate * timeSinceLastSwitch));

            if (CurrentTarget == null || !CurrentTarget.activeInHierarchy)
            {
                _lastBestTarget = FindBestTarget();
                CurrentTarget = _lastBestTarget;
            }

            // Adjust the switching logic to be less frequent
            if (Random.value < _targetSwitchProbability && Time.time >= _lastTargetChangeTime + _minimumSwitchTime)
            {
                SwitchTarget();
                ResetTargetSwitchProbability();
            }

            if (currentState != DodgeballAI.AIState.Possession &&
                currentState != DodgeballAI.AIState.Throw &&
                currentState != DodgeballAI.AIState.BackOff)
                CheckForNearbyDodgeballs();

            // if (CurrentTarget != null)
            // {
            //     // AI be lookin' 0_0
            //     LookAtTarget(CurrentTarget.transform.position);
            // }
        }

        private void CheckForNearbyDodgeballs()
        {
            foreach (var ball in _playArea.dodgeBalls)
            {
                if (!ball.activeInHierarchy) continue;
                if (BallProximity(ball, out var distanceToBall)) continue;
                if (CanNotOverride(distanceToBall)) continue;
                if (!IsInPlayArea(ball.transform.position, _ai.friendlyTeam.playArea, _ai.team)) continue;
                var ballstate = ball.GetComponent<DodgeBall>()._ballState;
                if (ballstate != BallState.Dead) continue;
                CurrentTarget = ball;
                ResetTargetSwitchProbability();
                break;
            }
        }

        private bool CanNotOverride(float distanceToBall)
        {
            var overrideProbability = (1 - (distanceToBall / _dodgeballProximityThreshold)) * _difficultyWeight;
            if (Random.value >= overrideProbability) return true;
            return false;
        }

        private bool BallProximity(GameObject ball, out float distanceToBall)
        {
            distanceToBall = Vector3.Distance(_ai.transform.position, ball.transform.position);
            return distanceToBall >= _dodgeballProximityThreshold &&
                   IsInPlayArea(ball.transform.position, _ai.friendlyTeam.playArea, _ai.team);
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
                        // todo, this is a hack. We need to fix this in the future
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

            return bestTarget;
        }


        // also todo, we need to have more sticky targetting. All the balls are confusing
        // our ai
        private float CalculateTargetScore(GameObject target)
        {
            var score = 0f;

            if (!target) return -500f;
            if (!target.activeInHierarchy) return -500f;

            // todo, this needs to change pre venue
            var maxDistance = 18f;

            // this distance score is not the best. They keep switching between balls
            var distance = Vector3.Distance(_ai.transform.position, target.transform.position);
            score -= distance / maxDistance;


            var headPos = _ai.transform.position;
            headPos.y += 1;
            var direction = target.transform.position - headPos;

            if (Physics.Raycast(headPos, direction, out var hit))
            {
                // line of sight score
                if (hit.collider.gameObject == target) score += GetPriority(PriorityType.LineOfSight); //0.15f;
                // ball unpossessed score
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Ball"))
                {
                    var ball = hit.collider.gameObject.GetComponent<DodgeBall>();
                    if (!_ai.hasBall) score += GetPriority(PriorityType.PossessedBall); // 0.4f;
                    if (_ai.hasBall) score -= GetPriority(PriorityType.PossessedBall); // 0.8f;
                    if (IsInPlayArea(target.transform.position, _ai.friendlyTeam.playArea, _ai.team))
                        score += GetPriority(PriorityType.InsidePlayArea); // 0.2f
                    else if (ball._ballState == BallState.Dead)
                        score += GetPriority(PriorityType.FreeBall); // 0.5f
                    else if (ball._ballState == BallState.Possessed && ball._team == _ai.team)
                        score -= GetPriority(PriorityType.Targeted);
                    else score -= GetPriority(PriorityType.OutsidePlayArea); // 5f
                    
                }
            }

            if (CurrentTarget != target && CurrentTarget.GetComponent<DodgeBall>() != null &&
                target.GetComponent<DodgeBall>() != null)
                score -= GetPriority(PriorityType.Targeted); // 0.5f;

            // enemy target score
            if (target.layer == LayerMask.NameToLayer(_enemyTeam.layerName))
            {
                // higher priority if has possession
                if (_ai.hasBall) score += GetPriority(PriorityType.PossessedBall); // 1.5f;

                // generally higher in priority
                score += GetPriority(PriorityType.Enemy); // 0.5f;
                var enemyAi = hit.collider.GetComponent<Actor>();

                // higher priority if enemy has possession
                if (enemyAi && enemyAi.hasBall) score += GetPriority(PriorityType.EnemyPossession); // 0.9f;
            }

            // randomness and sticky targeting
            score += Random.Range(-0.1f, 0.1f);
            if (CurrentTarget == target) score += GetPriority(PriorityType.Targeted); // 0.9f;
            if (target.GetComponent<DevController>() != null)
            {
                score += GetPriority(PriorityType.Targeted);
            }

            return score;
        }

        private bool _debug = true;
        private Color debugColor = new Color(Random.value, Random.value, Random.value);
        float lookWeightTarget = 1f;
        private float _headLookWeight;

        private void LookAtTarget(Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - _ai.transform.position;
            Vector3 flatDirection = direction;
            flatDirection.y = 0;

            float angleToTarget = Vector3.Angle(_ai.transform.forward, flatDirection);

            if (angleToTarget > args.fovAngle / 2) lookWeightTarget = 0.5f; 
            else lookWeightTarget = 1f;
            
            _headLookWeight = Mathf.Lerp(_headLookWeight, lookWeightTarget, Time.deltaTime * args.headTurnSpeed);

            if (_debug)
            {
                Vector3 debugDirection = direction;
                debugDirection.y -= 0.4f;
                Vector3 headPos = _ai.transform.position;
                headPos.y += 1;
                Debug.DrawRay(headPos, debugDirection, debugColor);
            }

            var actor = CurrentTarget.GetComponent<Actor>();
            args.ik.solvers.lookAt.target = actor ? actor.head : CurrentTarget.transform;

            args.ik.solvers.lookAt.SetLookAtWeight(_headLookWeight);

            if (_headLookWeight >= 0.5f)
            {
                Quaternion bodyTargetRotation = Quaternion.LookRotation(flatDirection, Vector3.up);
                _ai.transform.rotation = Quaternion.Slerp(_ai.transform.rotation, bodyTargetRotation,
                    Time.deltaTime * args.bodyTurnSpeed);
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