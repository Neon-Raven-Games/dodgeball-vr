using System;
using Cysharp.Threading.Tasks;
using Hands.SinglePlayer.EnemyAI.StatefulRefactor.BaseState;
using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI.StatefulRefactor.NinjaStates
{
    public class ShadowStepState : DerivedAIState<NinjaState, ShadowStepUtilityArgs>
    {
        private TeleportationPathHandler _teleportationPathHandler;
        private readonly float[] _preferredAngles = {-45f, 45f, -30f, 30f, -85f, 85f};
        private readonly Vector3[] _bestPoints = new Vector3[4];
        public override NinjaState State => NinjaState.ShadowStep;

        public ShadowStepState(DodgeballAI ai, DerivedAIStateController<NinjaState> controller,
            ShadowStepUtilityArgs args) : base(ai, controller, args)
        {
            _teleportationPathHandler = ai.GetComponent<TeleportationPathHandler>();
        }

        public override void EnterState()
        {
            AI.stayIdle = true;
            base.EnterState();
            InitializeTeleport();
            InvokeTeleport().Forget();
        }

        private async UniTaskVoid InvokeTeleport()
        {
            await UniTask.Yield();
            if (GetCancellationToken().IsCancellationRequested) return;
            
            _teleportationPathHandler.Teleport(TeleportationType.ShadowStep, Args.stepDirection,
                OnIntroPointReached, OnMovedToOutroPoint, OnFinishTeleport).Forget();
        }

        public override void ExitState()
        {
            AI.stayIdle = false;
            
            ResetIKWeights();
            CancelTask();
        }

        public override void UpdateState()
        {
        }

        public override void OnTriggerEnter(Collider collider)
        {
        }

        public override void OnTriggerExit(Collision col)
        {
        }

        public override void FixedUpdate()
        {
        }

        #region Ability Overrides

        public void ShadowStepIntroOverride()
        {
            InitializeTeleport();
            _teleportationPathHandler.Teleport(TeleportationType.ShadowStep, Args.stepDirection,
                () => AI.aiAvatar.SetActive(false), null, null).Forget();
        }

        public async UniTaskVoid ShadowStepOutroOverride()
        {
            Args.entryEffect.SetActive(false);
            Args.exitEffect.SetActive(true);
            AI.aiAvatar.SetActive(true);
            Args.floorSmoke.SetActive(false);

            AI.animator.Play("SmokeOutro");

            LerpColors(0, FrameToSeconds(Args.outroColorFrame, Args.outroAnimationClip),
                Args.outroAnimationClip, Args.outroColorLerpValue,
                0).Forget();
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
            AI.stayIdle = false;
            ChangeState(NinjaState.Default);
        }

        #endregion

        private void InitializeTeleport()
        {
            Args.stepDirection = CalculateValidShadowStep();
            if (Args.stepDirection == Vector3.zero)
            {
                ChangeState(NinjaState.Default);
                return;
            }
            AI.animator.SetTrigger(AIAnimationHelper.SSpecialOne);

            LerpColors(0, Args.introAnimationClip.length,
                Args.introAnimationClip, 0, Args.introColorLerpValue).Forget();

            Args.floorSmoke.transform.position = AI.transform.position + Args.stepDirection * (Args.stepDistance / 8);
            Args.floorSmoke.SetActive(true);
        }

        #region Behavior

        private void OnFinishTeleport() =>
            ExitTeleport().Forget();

        private void OnMovedToOutroPoint()
        {
            AI.ik.solvers.leftHand.SetIKPositionWeight(0);
            AI.ik.solvers.leftHand.SetIKRotationWeight(0);

            if (GetCancellationToken().IsCancellationRequested) return;
            EnterOutro();
        }

        private void EnterOutro()
        {
            Args.entryEffect.SetActive(false);
            Args.exitEffect.SetActive(true);
            AI.aiAvatar.SetActive(true);
            Args.floorSmoke.SetActive(false);

            AI.animator.Play(AIAnimationHelper.SSpecialOneExit);

            LerpColors(0, FrameToSeconds(Args.outroColorFrame, Args.outroAnimationClip),
                Args.outroAnimationClip, Args.outroColorLerpValue,
                0).Forget();

            InvokeAnimationEvent(Args.outroAnimationClip, Args.outroThrowFrame, AI.ThrowBall).Forget();
        }


        private void OnIntroPointReached()
        {

            ResetIKWeights();
            AI.aiAvatar.SetActive(false);
            if (GetCancellationToken().IsCancellationRequested) return;
            
            AI.SwitchBallSideToLeft();
            AI.SetOutOfPlay(false);
        }

        private async UniTask ExitTeleport()
        {
            ChangeState(NinjaState.Default);
            await UniTask.Yield();
        }

        #endregion

        #region Helpers

        private async UniTaskVoid InvokeAnimationEvent(AnimationClip clip, int frame, Action action)
        {
            await UniTask.Yield();
            const float tolerance = 0.01f;
            var executeDelay = FrameToSeconds(frame, clip);
            var curTime = 0f;

            while (curTime < executeDelay - tolerance)
            {
                AI.RotateToTargetManually(AI.CurrentTarget);
                var animState = AI.animator.GetCurrentAnimatorStateInfo(0);
                var normalizedTime = animState.normalizedTime % 1;
                curTime = normalizedTime * clip.length;
                await UniTask.Yield();
            }

            action.Invoke();
        }

        private void RandomizeAngles()
        {
            for (var i = _preferredAngles.Length - 1; i > 0; i--)
            {
                var j = UnityEngine.Random.Range(0, i + 1);
                (_preferredAngles[i], _preferredAngles[j]) = (_preferredAngles[j], _preferredAngles[i]);
            }
        }

        private Vector3 CalculateValidShadowStep()
        {
            Vector3 bestDirection = Vector3.zero;
            float maxDistance = 0f;
            RandomizeAngles();

            for (var i = 0; i < _preferredAngles.Length; i++)
            {
                Vector3 direction = Quaternion.Euler(0, _preferredAngles[i], 0) * AI.transform.forward;
                Debug.DrawRay(AI.transform.position, direction * Args.stepDistance, Color.red, 2.0f);

                Vector3 targetPosition = AI.transform.position + direction * Args.stepDistance;

                if (!IsWithinPlayArea(targetPosition, AI.friendlyTeam.playArea)) continue;

                var distance = Vector3.Distance(AI.transform.position, targetPosition);
                if (distance < maxDistance) continue;

                maxDistance = distance;
                bestDirection = direction;
            }

            if (bestDirection == Vector3.zero)
            {
                Debug.Log("no best dir");
                return Vector3.zero;
            }

            return bestDirection;
        }

        private bool IsWithinPlayArea(Vector3 position, Transform playArea)
        {
            var size = playArea.localScale;
            size.y = 5;
            Bounds bounds = new Bounds(playArea.position, size);
            Debug.DrawRay(position, Vector3.up, Color.cyan, 2.0f);

            return bounds.Contains(position);
        }

        private static float FrameToSeconds(int frameNumber, AnimationClip clip) =>
            frameNumber / clip.frameRate;

        // todo, this needs cancellation token flow
        private async UniTaskVoid LerpColors(float fromSeconds, float toSeconds, AnimationClip clip, float fromValue,
            float toValue)
        {
            const float tolerance = 0.01f;
            Args.colorLerp.lerpValue = fromValue;
            await UniTask.Yield();
            var curTime = 0f;
            while (curTime < fromSeconds && fromSeconds > 0 && AI.aiAvatar.activeInHierarchy)
            {
                var animState = AI.animator.GetCurrentAnimatorStateInfo(0);
                var normalizedTime = animState.normalizedTime % 1;
                curTime = normalizedTime * clip.length;

                fromSeconds /= AI.animator.speed;
                toSeconds /= AI.animator.speed;
                if (curTime >= fromSeconds) break;

                await UniTask.Yield();
            }

            var currentTime = 0f;
            while (Mathf.Abs(currentTime - toSeconds) > tolerance && AI.aiAvatar.activeInHierarchy)
            {
                var animState = AI.animator.GetCurrentAnimatorStateInfo(0);
                var normalizedTime = animState.normalizedTime % 1;
                currentTime = normalizedTime * clip.length;

                var t = Mathf.InverseLerp(fromSeconds, toSeconds, currentTime);
                Args.colorLerp.lerpValue = Mathf.Lerp(fromValue, toValue, t);

                await UniTask.Yield();
            }

            Args.colorLerp.lerpValue = 0;
        }

        #endregion
    }
}