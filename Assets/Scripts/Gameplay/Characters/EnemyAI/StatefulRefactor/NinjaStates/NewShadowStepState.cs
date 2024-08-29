using System;
using System.Collections;
using Hands.SinglePlayer.EnemyAI.StatefulRefactor.BaseState;
using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI.StatefulRefactor.NinjaStates
{
    public class NewShadowStepState: BaseAIState<ShadowStepUtilityArgs>
    {
        private TeleportationPathHandler _teleportationPathHandler;
        public override int State => NinjaStruct.ShadowStep;
        private readonly float[] _preferredAngles = {-45f, 45f, -30f, 30f, -85f, 85f};
        public NewShadowStepState(DodgeballAI ai, AIStateController controller, ShadowStepUtilityArgs args) : base(ai, controller, args)
        {
            _teleportationPathHandler = ai.GetComponent<TeleportationPathHandler>();
        }
        private static readonly int _SThrow = Animator.StringToHash("Throw");

        private void InitializeTeleport()
        {
            Args.stepDirection = CalculateValidShadowStep();
            if (Args.stepDirection == Vector3.zero)
            {
                ChangeState(StateStruct.Move);
                return;
            }
            AI.animator.SetTrigger(AIAnimationHelper.SSpecialOne);

            AI.StartCoroutine(LerpColors(0, Args.introAnimationClip.length,
                Args.introAnimationClip, 0, Args.introColorLerpValue));

            Args.floorSmoke.transform.position = AI.transform.position + Args.stepDirection * (Args.stepDistance / 8);
            Args.floorSmoke.SetActive(true);
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
        private IEnumerator LerpColors(float fromSeconds, float toSeconds, AnimationClip clip, float fromValue,
            float toValue)
        {
            const float tolerance = 0.01f;
            Args.colorLerp.lerpValue = fromValue;
            yield return null;
            var curTime = 0f;
            while (curTime < fromSeconds && fromSeconds > 0 && AI.aiAvatar.activeInHierarchy)
            {
                var animState = AI.animator.GetCurrentAnimatorStateInfo(0);
                var normalizedTime = animState.normalizedTime % 1;
                curTime = normalizedTime * clip.length;

                fromSeconds /= AI.animator.speed;
                toSeconds /= AI.animator.speed;
                if (curTime >= fromSeconds) break;

                yield return null;
            }

            var currentTime = 0f;
            while (Mathf.Abs(currentTime - toSeconds) > tolerance && AI.aiAvatar.activeInHierarchy)
            {
                var animState = AI.animator.GetCurrentAnimatorStateInfo(0);
                var normalizedTime = animState.normalizedTime % 1;
                currentTime = normalizedTime * clip.length;

                var t = Mathf.InverseLerp(fromSeconds, toSeconds, currentTime);
                Args.colorLerp.lerpValue = Mathf.Lerp(fromValue, toValue, t);

                yield return null;
            }

            Args.colorLerp.lerpValue = 0;
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
        public override void OnTriggerExit(Collision col)
        {
        }
        private void OnMovedToOutroPoint()
        {
            AI.ik.solvers.leftHand.SetIKPositionWeight(0);
            AI.ik.solvers.leftHand.SetIKRotationWeight(0);

            EnterOutro();
        }
        private void EnterOutro()
        {
            Args.entryEffect.SetActive(false);
            Args.exitEffect.SetActive(true);
            AI.aiAvatar.SetActive(true);
            Args.floorSmoke.SetActive(false);

            AI.animator.Play(AIAnimationHelper.SSpecialOneExit);

            AI.StartCoroutine(LerpColors(0, FrameToSeconds(Args.outroColorFrame, Args.outroAnimationClip),
                Args.outroAnimationClip, Args.outroColorLerpValue,
                0));

            AI.StartCoroutine(InvokeAnimationEvent(Args.outroAnimationClip, Args.outroThrowFrame, AI.ThrowBall));
        }

        private IEnumerator InvokeAnimationEvent(AnimationClip clip, int frame, Action action)
        {
            yield return null;
            const float tolerance = 0.01f;
            var executeDelay = FrameToSeconds(frame, clip);
            var curTime = 0f;

            while (curTime < executeDelay - tolerance)
            {
                var animState = AI.animator.GetCurrentAnimatorStateInfo(0);
                var normalizedTime = animState.normalizedTime % 1;
                curTime = normalizedTime * clip.length;
                yield return null;
            }

            action.Invoke();
        }
        private void OnIntroPointReached()
        {
            ResetIKWeights();
            AI.aiAvatar.SetActive(false);
            AI.SwitchBallSideToLeft();
            AI.SetOutOfPlay(false);
        }

        public override void EnterState()
        {
            ChangeState(StateStruct.Throw);
            AI.animator.ResetTrigger(_SThrow);
            InitializeTeleport();
            _teleportationPathHandler.Teleport(TeleportationType.ShadowStep, Args.stepDirection,
                OnIntroPointReached, OnMovedToOutroPoint, OnFinishTeleport).Forget();
        }

        private void OnFinishTeleport()
        {
            ChangeState(StateStruct.Move);
        }

        public override void FixedUpdate()
        {
        }

        public override void ExitState()
        {
        }

        public override void UpdateState()
        {
            AI.RotateToTargetManually(AI.CurrentTarget);
        }

        public override void OnTriggerEnter(Collider collider)
        {
        }
    }
}