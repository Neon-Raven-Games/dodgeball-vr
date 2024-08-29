using Cysharp.Threading.Tasks;
using Hands.SinglePlayer.EnemyAI.Priority;
using Hands.SinglePlayer.EnemyAI.StatefulRefactor;
using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI
{
    public class DodgeballTargetModule
    {
        private const float headTurnSpeed = 0.35f;

        // ninja values
        private const float bodyTurnSpeed = 6;
        private const float fovAngle = 60f;
        // dodgeball ai values
        // public const float bodyTurnSpeed = 5f;
        // public const float fovAngle = 160f;

        private const bool _debug = true;
        private readonly Color _debugColor = new Color(Random.value, Random.value, Random.value);

        private float _lookWeightTarget = 1f;
        private float _headLookWeight;

        public GameObject CurrentTarget { get; private set; }
        public Actor ActorTarget { get; private set; }
        public DodgeBall BallTarget { get; private set; }

        private readonly ActorTeam _enemyTeam;
        private readonly DodgeballPlayArea _playArea;
        private readonly DodgeballAI _ai;

        private bool _lerpingHeadWeight;
        private PriorityData _priorityData;

        private float GetPriority(PriorityType type)
        {
            if (!_priorityData) return 1f;
            return _priorityData.GetPriorityValue(type);
        }

        internal Bounds playAreaBounds;

        protected bool IsInPlayArea(Vector3 position)
        {
            bool inBounds = playAreaBounds.Contains(position);
            return inBounds;
        }

        public DodgeballTargetModule(DodgeballAI ai, PriorityData data)
        {
            _priorityData = ai.playArea.testingData;
            _ai = ai;
            _playArea = ai.playArea;
            _enemyTeam = ai.opposingTeam;
            CurrentTarget = _enemyTeam.actors[Random.Range(0, _enemyTeam.actors.Count)].gameObject;
            ActorTarget = CurrentTarget.GetComponent<Actor>();

            var friendlyArea = ai.friendlyTeam.playArea;
            playAreaBounds = new Bounds(friendlyArea.position,
                new Vector3(friendlyArea.localScale.x, 5, friendlyArea.localScale.z));
        }

        public void UpdateTarget()
        {
            if (_ai.state == StateStruct.Throw)
            {
                LookAtTarget(CurrentTarget.transform.position);
                return;
            }
            if (!_ai.hasBall && !ValidBall()) CheckForNearbyDodgeballs();
            if (!BallTarget || _ai.hasBall && !ValidActor()) CurrentTarget = FindBestTarget();
            if (!CurrentTarget) CurrentTarget = _ai.opposingTeam.playArea.gameObject;

            LookAtTarget(CurrentTarget.transform.position);
        }

        private bool ValidActor() =>
            ActorTarget &&
            ActorTarget.gameObject.activeInHierarchy &&
            !ActorTarget.IsOutOfPlay();

        private bool ValidBall() =>
            BallTarget &&
            BallTarget.gameObject.activeInHierarchy &&
            BallTarget._ballState == BallState.Dead &&
            IsInPlayArea(BallTarget.transform.position);

        private void CheckForNearbyDodgeballs()
        {
            foreach (var ball in _playArea.dodgeBalls.Keys)
            {
                if (!ball.gameObject.activeInHierarchy) continue;
                if (!IsInPlayArea(ball.transform.position)) continue;
                if (ball._ballState != BallState.Dead) continue;

                CurrentTarget = ball.gameObject;
                BallTarget = ball;
                ActorTarget = null;
                break;
            }
        }

        private GameObject FindBestTarget()
        {
            GameObject bestTarget = null;
            var bestScore = float.MinValue;
            
            foreach (var enemy in _enemyTeam.actors)
            {
                if (enemy == null || enemy.IsOutOfPlay() || !enemy.gameObject.activeInHierarchy) continue;
                var score = CalculateTargetScore(enemy);

                if (score < bestScore) continue;
                bestScore = score;
                bestTarget = enemy.gameObject;
                ActorTarget = enemy;
                BallTarget = null;
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
            if (target is DevController) score += 5f;
            return score;
        }

        private async UniTaskVoid StopLook()
        {
            var from = _headLookWeight;
            var elapsedTime = 0f;
            while (elapsedTime < 1f)
            {
                if (!_ai.ik) return;
                elapsedTime += Time.deltaTime;

                var t = Mathf.Clamp01(elapsedTime / headTurnSpeed);
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

        public void LookAtTarget(Vector3 targetPosition)
        {
            var direction = targetPosition - _ai.transform.position;
            if (direction.magnitude < 0.1f) return;

            var flatDirection = new Vector3(direction.x, 0, direction.z).normalized;
            var angleToTarget = Vector3.Angle(_ai.transform.forward, flatDirection);

            _lookWeightTarget = angleToTarget > fovAngle / 2 ? 0.4f : 1f;
            _headLookWeight = Mathf.Lerp(_headLookWeight, _lookWeightTarget, Time.deltaTime * headTurnSpeed * 30);

#if UNITY_EDITOR
            if (_debug)
            {
                Vector3 debugDirection = direction;
                debugDirection.y -= 0.4f;
                Vector3 headPos = _ai.transform.position;
                headPos.y += 1;
                Debug.DrawRay(headPos, debugDirection, _debugColor);
            }
#endif

            if (ActorTarget && ActorTarget.head) _ai.ik.solvers.lookAt.target = ActorTarget.head;
            else _ai.ik.solvers.lookAt.target = CurrentTarget.transform;

            _ai.ik.solvers.lookAt.SetLookAtWeight(_headLookWeight);
            if (flatDirection == Vector3.zero) return;
            if (_headLookWeight >= 0.7f)
            {
                var bodyTargetRotation = Quaternion.LookRotation(flatDirection);
                if (bodyTargetRotation.eulerAngles == Vector3.zero) return;
                _ai.transform.rotation = Quaternion.Slerp(_ai.transform.rotation, bodyTargetRotation,
                    Time.deltaTime * bodyTurnSpeed);
            }
            else
            {
                var headTargetRotation = Quaternion.LookRotation(flatDirection);
                if (headTargetRotation.eulerAngles == Vector3.zero) return;
                _ai.transform.rotation = Quaternion.Slerp(_ai.transform.rotation, headTargetRotation,
                    Time.deltaTime * bodyTurnSpeed / 2);
            }
        }
    }
}