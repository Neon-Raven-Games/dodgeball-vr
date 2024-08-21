using System.Threading;
using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI.StatefulRefactor.BaseState
{
    public interface IDerivedAIState
    {
        DodgeballAI AI { get; }
        void EnterState();
        void ExitState();
        void UpdateState();
        void OnTriggerEnter(Collider collider);
        void OnTriggerExit(Collision col);

        void FixedUpdate();
    }

    public abstract class DerivedAIState<TEnum, TArgs> : IDerivedAIState
        where TEnum : System.Enum
        where TArgs : UtilityArgs
    {
        public abstract TEnum State { get; }
        public abstract void FixedUpdate();
        public abstract void ExitState();
        public abstract void UpdateState();
        public abstract void OnTriggerEnter(Collider collider);
        public abstract void OnTriggerExit(Collision col);

        private readonly DerivedAIStateController<TEnum> _controller;
        public DodgeballAI AI { get; }
        protected TArgs Args { get; }

        protected bool active;

        private readonly int _ballLayer = LayerMask.NameToLayer("Ball");

        private CancellationTokenSource _cancellationTokenSource;
        private readonly object _lock = new();

        protected DerivedAIState(DodgeballAI ai, DerivedAIStateController<TEnum> controller, TArgs args)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            AI = ai;
            Args = args;
            _controller = controller;
        }

        #region Helpers

        // ReSharper disable once InconsistentlySynchronizedField
        protected CancellationToken GetCancellationToken() =>
            _cancellationTokenSource.Token;

        protected bool ColliderOnBallLayer(Collider collider) =>
            collider.gameObject.layer == _ballLayer;

        protected void ChangeState(TEnum state) =>
            _controller.ChangeState(state);

        #endregion

        #region Thread Safety

        /// <summary>
        /// Contains an async lock and cancellation token source for async routines. Should never be disposed here, but
        /// adding explicit disposal for thread safety. If we accidentally dispose it somewhere else, it won't break the
        /// game.
        /// </summary>
        public virtual void EnterState()
        {
            lock (_lock)
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource.Dispose();
                    _cancellationTokenSource = new CancellationTokenSource();
                    Debug.LogWarning($"[{AI.gameObject.name}] Cancellation token Warning! Entered a state with a " +
                                     $"cancelled token source. Creating a new one.");
                }
            }

            active = true;
        }

        /// <summary>
        /// Cancels the current task and creates a new cancellation token source for reuse.
        /// </summary>
        protected void CancelTask()
        {
            if (!active) return;

            lock (_lock)
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource.Dispose();
                }

                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();
            }
        }

        #endregion
    }
}