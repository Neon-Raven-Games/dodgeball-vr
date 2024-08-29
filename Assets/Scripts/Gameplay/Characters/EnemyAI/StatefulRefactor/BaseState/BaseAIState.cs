using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI.StatefulRefactor.BaseState
{
    public abstract class BaseAIState<TArgs> : IDerivedAIState where TArgs : UtilityArgs
    {
        public abstract void FixedUpdate();
        public abstract void ExitState();
        public abstract void UpdateState();
        public abstract void OnTriggerEnter(Collider collider);
        public abstract void OnTriggerExit(Collision col);
        public abstract int State { get; }
        protected readonly AIStateController controller;
        public DodgeballAI AI { get; }
        protected TArgs Args { get; }

        protected bool active;

        private readonly int _ballLayer = LayerMask.NameToLayer("Ball");

        private CancellationTokenSource _cancellationTokenSource;
        private readonly object _lock = new();

        protected BaseAIState(DodgeballAI ai, AIStateController controller, TArgs args)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            AI = ai;
            Args = args;
            this.controller = controller;
        }

        #region Helpers

        protected void ResetIKWeights()
        {
            if (!AI || !AI.ik) return;
            AI.ik.solvers.lookAt.SetLookAtWeight(0);
            AI.ik.solvers.rightHand.SetIKPositionWeight(0);
            AI.ik.solvers.leftHand.SetIKPositionWeight(0);
            AI.ik.solvers.rightHand.SetIKRotationWeight(0);
            AI.ik.solvers.leftHand.SetIKRotationWeight(0);
        }

        protected bool ColliderOnBallLayer(Collider collider) =>
            collider.gameObject.layer == _ballLayer;

        protected void ChangeState(int state) =>
            controller.ChangeState(state);

        #endregion

        #region Thread Safety

        /// <summary>
        /// Contains an async lock and cancellation token source for async routines. Should never be disposed here, but
        /// adding explicit disposal for thread safety. If we accidentally dispose it somewhere else, it won't break the
        /// game.
        /// </summary>
        public virtual void EnterState()
        {
        }

        private string StateString => GetType().Name;


        public void CleanUp()
        {
            lock (_lock)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }
        }

        public UtilityArgs GetArgs()
        {
            return Args;
        }

        /// <summary>
        /// Cancels the current task and creates a new cancellation token source for reuse.
        /// </summary>
        public void CancelTask()
        {

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

        private float _moveSpeed = 2.8f;



        protected void MoveTowardsTarget(DodgeballAI ai, Vector3 targetPosition)
        {
            targetPosition = ClampPositionToPlayArea(targetPosition, ai.playArea, ai.team);
            targetPosition.y = ai.transform.position.y;

            var distanceToTarget = Vector3.Distance(ai.transform.position, targetPosition);
            var stopDistance = controller.moveUtilityArgs.predictiveStopDistance;
            var speed = controller.moveUtilityArgs.moveSpeed;

            if (distanceToTarget < stopDistance)
            {
                speed *= Mathf.SmoothStep(0f, 1f, distanceToTarget / stopDistance);
            }

            var previousPosition = ai.transform.position;
            ai.transform.position = Vector3.MoveTowards(ai.transform.position, targetPosition, speed * Time.deltaTime);

            var movementDirection = ai.transform.InverseTransformDirection(ai.transform.position - previousPosition)
                .normalized;
            movementDirection *= speed * controller.moveUtilityArgs.blendMultiplier;

            _animBlendTreeAxis.x =
                Mathf.Lerp(_animBlendTreeAxis.x, movementDirection.x, controller.moveUtilityArgs.blendSpeed * Time.deltaTime);
            ai.animator.SetFloat(_SXAxis, Mathf.Clamp(_animBlendTreeAxis.x, -1f, 1f));

            _animBlendTreeAxis.y =
                Mathf.Lerp(_animBlendTreeAxis.y, movementDirection.z, controller.moveUtilityArgs.blendSpeed * Time.deltaTime);
            ai.animator.SetFloat(_SYAxis, Mathf.Clamp(_animBlendTreeAxis.y, -1f, 1f));
        }

        private Vector2 _animBlendTreeAxis;
        private float _nextMoveTime;
        protected static readonly int _SXAxis = Animator.StringToHash("xAxis");
        protected static readonly int _SYAxis = Animator.StringToHash("yAxis");
        #endregion
    }
}