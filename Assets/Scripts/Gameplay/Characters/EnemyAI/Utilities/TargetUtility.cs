using System;
using Cysharp.Threading.Tasks;
using Hands.SinglePlayer.EnemyAI.Priority;
using RootMotion.FinalIK;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Hands.SinglePlayer.EnemyAI
{
    [Serializable]
    public class TargetUtilityArgs : UtilityArgs
    {
        public float headTurnSpeed = 2f;
        public float bodyTurnSpeed = 1f;
        public float fovAngle = 120f;
        public BipedIK ik;
    }

    public class TargetUtility : Utility<TargetUtilityArgs>, IUtility
    {
        public GameObject CurrentTarget { get; private set; }
        public Actor ActorTarget { get; private set; }
        public DodgeBall BallTarget { get; private set; }

        private readonly ActorTeam _enemyTeam;
        private readonly DodgeballPlayArea _playArea;
        private readonly DodgeballAI _ai;
        
        private bool _lerpingHeadWeight;
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
            ActorTarget = CurrentTarget.GetComponent<Actor>();
        }

        public override float Execute(DodgeballAI ai)
        {
            if (!CurrentTarget && !_ai.hasBall) CheckForNearbyDodgeballs();
            if (!CurrentTarget) CurrentTarget = FindBestTarget();
            if (!CurrentTarget)
            {
                _ai.ik.solvers.lookAt.SetLookAtWeight(0);
                _ai.ik.solvers.spine.SetIKPositionWeight(0);
                
                _ai.ik.solvers.rightHand.SetIKPositionWeight(0);
                _ai.ik.solvers.rightHand.SetIKRotationWeight(0);
                
                _ai.ik.solvers.leftHand.SetIKPositionWeight(0);
                _ai.ik.solvers.leftHand.SetIKRotationWeight(0);
                
                return -1f;
            }
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
                     (BallTarget._ballState != BallState.Dead ||
                      !BallTarget.gameObject.activeInHierarchy))
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
                if (ball._ballState != BallState.Dead) continue;
                
                CurrentTarget = ball.gameObject;
                BallTarget = ball;
                break;
            }
        }

        private GameObject FindBestTarget()
        {
            BallTarget = null;
            GameObject bestTarget = null;
            var bestScore = float.MinValue;

            foreach (var enemy in _enemyTeam.actors)
            {
                if (enemy == null || enemy.IsOutOfPlay()) continue;
                var score = CalculateTargetScore(enemy);

                if (score < bestScore) continue;
                bestScore = score;
                bestTarget = enemy.gameObject;
                ActorTarget = enemy;
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

            if (CurrentTarget == target.gameObject) score += GetPriority(PriorityType.Targeted);
            return score;
        }

        private bool _debug = true;
        private Color debugColor = new Color(Random.value, Random.value, Random.value);
        
        private float _lookWeightTarget = 1f;
        private float _headLookWeight;

        private void LookAtTarget(Vector3 targetPosition)
        {
            var direction = targetPosition - _ai.transform.position;
            if (direction.magnitude < 0.1f) return;
            
            var flatDirection = new Vector3(direction.x, 0, direction.z).normalized;
            var angleToTarget = Vector3.Angle(_ai.transform.forward, flatDirection);
            
            _lookWeightTarget = angleToTarget > args.fovAngle / 2 ? 0.4f : 1f;
            _headLookWeight = Mathf.Lerp(_headLookWeight, _lookWeightTarget, Time.deltaTime * args.headTurnSpeed * 30);

#if UNITY_EDITOR
            if (_debug)
            {
                Vector3 debugDirection = direction;
                debugDirection.y -= 0.4f;
                Vector3 headPos = _ai.transform.position;
                headPos.y += 1;
                Debug.DrawRay(headPos, debugDirection, debugColor);
            }
#endif

            args.ik.solvers.lookAt.target = ActorTarget.transform == CurrentTarget.transform ?
                ActorTarget.head : CurrentTarget.transform;
            
            args.ik.solvers.lookAt.SetLookAtWeight(_headLookWeight);
            if (flatDirection == Vector3.zero) return;
            if (_headLookWeight >= 0.7f)
            {
                var bodyTargetRotation = Quaternion.LookRotation(flatDirection);
                if (bodyTargetRotation.eulerAngles == Vector3.zero) return;
                _ai.transform.rotation = Quaternion.Slerp(_ai.transform.rotation, bodyTargetRotation,
                    Time.deltaTime * args.bodyTurnSpeed);
            }
            else
            {
                var headTargetRotation = Quaternion.LookRotation(flatDirection);
                if (headTargetRotation.eulerAngles == Vector3.zero) return;
                _ai.transform.rotation = Quaternion.Slerp(_ai.transform.rotation, headTargetRotation,
                    Time.deltaTime * args.bodyTurnSpeed / 2);
            }
        }

        private async UniTaskVoid StopLook()
        {
            var from = _headLookWeight;
            var elapsedTime = 0f;
            while (elapsedTime < 1f)
            {
                if (!_ai.ik) return;
                elapsedTime += Time.deltaTime;
                // can we make this time based?
                var t = Mathf.Clamp01(elapsedTime / args.headTurnSpeed);

                _headLookWeight = Mathf.Lerp(from, 0f, t);
                _ai.ik.solvers.lookAt.SetLookAtWeight(_headLookWeight);

                await UniTask.Yield();
            }

            _headLookWeight = 0;
            _ai.ik.solvers.lookAt.SetLookAtWeight(_headLookWeight);
            _lerpingHeadWeight = false;
        }

        public void ResetLookWeight()
        {
            if (_headLookWeight <= 0.01f || _lerpingHeadWeight) return;
            _lerpingHeadWeight = true;
            StopLook().Forget();
        }
    }
}