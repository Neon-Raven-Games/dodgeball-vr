using System.Threading.Tasks;
using Hands.SinglePlayer.EnemyAI.StatefulRefactor.BaseState;
using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI.StatefulRefactor.States
{
    public class MoveState : BaseAIState<MoveUtilityArgs>
    {
        public override int State => StateStruct.Move;

        private Vector3 _separation = Vector3.zero;
        private Vector3 _alignment = Vector3.zero;
        private Vector3 _cohesion = Vector3.zero;
        private Vector3 _centerAttraction = Vector3.zero;
        private int _neighborCount = 0;
        private Vector3 _noise = Vector3.zero;
        private float _distance;
        private Vector3 _currentTargetPosition;
        private float _changeTargetCooldown;
        private float _lastTargetChangeTime;
        private Vector3 _noiseOffset;
        private float _backoffDuration = .8f;
        private float _backoffStartTime;
        private Vector2 _animBlendTreeAxis;
        private float _nextMoveTime;
        private static readonly int _SXAxis = Animator.StringToHash("xAxis");
        private static readonly int _SYAxis = Animator.StringToHash("yAxis");

        public MoveState(DodgeballAI ai, AIStateController controller, MoveUtilityArgs args) : base(ai, controller,
            args)
        {
        }

        public override void FixedUpdate()
        {
        }

        public override void ExitState()
        {
        }

        public override void UpdateState()
        {
            controller.targetModule.UpdateTarget();
            FlockMove(AI);
        }

        public override void OnTriggerEnter(Collider collider)
        {
        }

        public override void OnTriggerExit(Collision col)
        {
        }

        internal bool PickupMove(DodgeballAI ai)
        {

            return true;
        }

        private bool IsInPlayArea(Vector3 position)
        {
            var playArea = AI.friendlyTeam.playArea;
            var playAreaBounds = new Bounds(playArea.position,
                new Vector3(playArea.localScale.x, 1, playArea.localScale.z));
            return playAreaBounds.Contains(position);
        }

        public void FlockMove(DodgeballAI ai)
        {
            if (Time.time > _lastTargetChangeTime + _changeTargetCooldown)
            {
                _changeTargetCooldown = Random.Range(0.3f, 2.5f);
                _lastTargetChangeTime = Time.time;
                Vector3 randomDirection = Random.insideUnitSphere;
                randomDirection.y = 0;
                _currentTargetPosition = ai.transform.position + randomDirection * Args.moveSpeed;
                _currentTargetPosition = ClampPositionToPlayArea(_currentTargetPosition, ai.playArea, ai.team);
            }

            _separation = Vector3.zero;
            _alignment = Vector3.zero;
            _cohesion = Vector3.zero;
            _centerAttraction = Vector3.zero;
            _neighborCount = 0;
            foreach (var teammate in ai.friendlyTeam.actors)
            {
                if (teammate == ai.gameObject) continue;

                _distance = Vector3.Distance(ai.transform.position, teammate.transform.position);
                if (_distance < 0.1f) continue; // Prevent division by zero or extremely close neighbors
                if (_distance < Args.separationDistance)
                {
                    _separation += (ai.transform.position - teammate.transform.position).normalized / _distance;
                }

                _alignment += teammate.transform.forward;
                _cohesion += teammate.transform.position;
                _neighborCount++;
            }

            if (_neighborCount > 0)
            {
                _alignment /= _neighborCount;
                _cohesion = (_cohesion / _neighborCount - ai.transform.position).normalized;

                var playAreaCenter = ai.friendlyTeam.playArea.position;
                _centerAttraction = (playAreaCenter - ai.transform.position).normalized;

                var flockingMove = (_separation * Args.separationWeight + _alignment * Args.alignmentWeight +
                                    _cohesion * Args.cohesionWeight + (_centerAttraction * Args.centerAffinity))
                    .normalized;

                _currentTargetPosition += flockingMove * Args.moveSpeed * Time.deltaTime;
                _currentTargetPosition = ClampPositionToPlayArea(_currentTargetPosition, ai.playArea, ai.team);
            }

            // Add smooth random movement to avoid stagnation
            _noiseOffset += new Vector3(Time.deltaTime, 0, Time.deltaTime);
            _noise = new Vector3(Mathf.PerlinNoise(_noiseOffset.x, 0), 0, Mathf.PerlinNoise(0, _noiseOffset.z)) -
                     Vector3.one * 0.5f;
            _currentTargetPosition += _noise * Args.randomnessFactor;
            MoveTowards(ai, _currentTargetPosition);
        }

        private void MoveTowards(DodgeballAI ai, Vector3 targetPosition)
        {
            targetPosition = ClampPositionToPlayArea(targetPosition, ai.playArea, ai.team);
            targetPosition.y = 0.11f;

            var distanceToTarget = Vector3.Distance(ai.transform.position, targetPosition);
            var stopDistance = Args.predictiveStopDistance;
            var speed = Args.moveSpeed;

            if (distanceToTarget < stopDistance)
            {
                speed *= Mathf.SmoothStep(0f, 1f, distanceToTarget / stopDistance);
            }

            var previousPosition = ai.transform.position;
            previousPosition.y = 0.11f;
            ai.transform.position = Vector3.MoveTowards(previousPosition, targetPosition, speed * Time.deltaTime);

            var movementDirection = ai.transform.InverseTransformDirection(ai.transform.position - previousPosition)
                .normalized;
            movementDirection *= speed * Args.blendMultiplier;

            _animBlendTreeAxis.x =
                Mathf.Lerp(_animBlendTreeAxis.x, movementDirection.x, Args.blendSpeed * Time.deltaTime);
            ai.animator.SetFloat(_SXAxis, Mathf.Clamp(_animBlendTreeAxis.x, -1f, 1f));

            _animBlendTreeAxis.y =
                Mathf.Lerp(_animBlendTreeAxis.y, movementDirection.z, Args.blendSpeed * Time.deltaTime);
            ai.animator.SetFloat(_SYAxis, Mathf.Clamp(_animBlendTreeAxis.y, -1f, 1f));
        }
    }
}