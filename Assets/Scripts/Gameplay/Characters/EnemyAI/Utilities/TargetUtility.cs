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
    public class TargetUtility : Utility<TargetUtilityArgs>, IUtility
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
        private float _lastTargetChangeTime;
        private float _targetSwitchProbability;
        private readonly DodgeballAI _ai;

        private PriorityData _priorityData;

        private float GetPriority(PriorityType type) =>
            _priorityData.GetPriorityValue(type);

        public TargetUtility(TargetUtilityArgs arg, DodgeballAI ai, PriorityData data) : base(arg, AIState.Idle)
        {
            _priorityData = data;
            _ai = ai;
            _playArea = ai.playArea;
            _enemyTeam = ai.opposingTeam;
            CurrentTarget = _enemyTeam.actors[Random.Range(0, _enemyTeam.actors.Count)].gameObject;
            _lastBestTarget = CurrentTarget;
            ActorTarget = CurrentTarget.GetComponent<Actor>();

            _minSwitchProbability = arg.minSwitchProbability;
            _maxSwitchProbability = arg.maxSwitchProbability;
            _switchProbabilityIncreaseRate = arg.switchProbabilityIncreaseRate;
            _minimumSwitchTime = arg.minimumSwitchTime;
            _difficultyWeight = arg.difficultyWeight;
            ResetTargetSwitchProbability();
        }

        public override float Execute(DodgeballAI ai)
        {
            if (!CurrentTarget && _ai.hasBall) CheckForNearbyDodgeballs();
            if (!CurrentTarget) CurrentTarget = FindBestTarget();
            if (!CurrentTarget) return -1f;
            LookAtTarget(CurrentTarget.transform.position);
            return 1f;
        }

        public override float Roll(DodgeballAI ai)
        {
            if (_ai.hasBall)
            {
                CurrentTarget = FindBestTarget();
                return 1f;
            }

            if (!BallTarget) CheckForNearbyDodgeballs();
            
            else if (BallTarget &&
                     (BallTarget._ballState != BallState.Dead || !BallTarget.gameObject.activeInHierarchy))
            {
                CheckForNearbyDodgeballs();
            }
            if (!BallTarget) CurrentTarget = FindBestTarget();

            return 0f;
        }

        private void CheckForNearbyDodgeballs()
        {
            foreach (var ball in _playArea.dodgeBalls.Keys)
            {
                if (!ball.gameObject.activeInHierarchy) continue;
                if (!IsInPlayArea(ball.transform.position)) continue;

                var ballstate = ball._ballState;
                if (ballstate != BallState.Dead) continue;
                CurrentTarget = ball.gameObject;
                BallTarget = ball;
                break;
            }
        }

        public void ResetTargetSwitchProbability()
        {
            _targetSwitchProbability = _minSwitchProbability;
            _lastTargetChangeTime = Time.time;
        }

        private GameObject FindBestTarget()
        {
            BallTarget = null;
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
                        float score = CalculateTargetScore(enemyActor);
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestTarget = enemyActor.gameObject;
                            ActorTarget = enemyActor;
                        }
                    }
                }
            }

            return bestTarget;
        }

        private float CalculateTargetScore(Actor target)
        {
            var score = 0f;

            if (!target || !target.gameObject.activeInHierarchy) return -500f;

            var maxDistance = 18f;
            var distance = Vector3.Distance(_ai.transform.position, target.transform.position);
            score -= distance / maxDistance;

            if (_ai.hasBall) score += GetPriority(PriorityType.PossessedBall);
            score += GetPriority(PriorityType.Enemy);

            if (target && target.hasBall) score += GetPriority(PriorityType.EnemyPossession);

            score += Random.Range(-0.1f, 0.1f);
            
            if (CurrentTarget == target.gameObject)
                score += GetPriority(PriorityType.Targeted);
            
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

            // Determine look weight based on FOV; Full weight if inside FOV
            lookWeightTarget = angleToTarget > args.fovAngle / 2 ? 0.4f : 1f; 

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