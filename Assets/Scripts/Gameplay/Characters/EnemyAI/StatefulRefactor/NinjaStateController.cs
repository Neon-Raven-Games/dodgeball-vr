using System.Collections;
using Gameplay.Util;
using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI.StatefulRefactor
{
    public class NinjaStateController : AIStateController
    {
        private NinjaHandSignUtilityArgs _handSignUtilityArgs;
        private FakeoutUtilityArgs _fakeoutUtilityArgs;
        private SmokeBombUtilityArgs _smokeBombUtilityArgs;

        private Coroutine _handSignCoroutine;

        private enum HandSignState
        {
            Inactive,
            Preparing,
            Active,
            Cooldown
        }
        private bool _isFakeoutCooldownActive;
        private float _nextHandSignTime;
        private HandSignState _handSignState;

        protected override void OnCalculationComplete(int state)
        {
            if (State == NinjaStruct.Substitution || State == NinjaStruct.ShadowStep ||
                State == StateStruct.Throw || State == NinjaStruct.FakeOut) return;
            
            switch (_handSignState)
            {
                case HandSignState.Inactive:
                    if (Time.time > _nextHandSignTime && _handSignUtilityArgs.handSignProbability.PickValue() > 0)
                        StartHandSign();
                    break;
                case HandSignState.Preparing:
                    if (state == StateStruct.OutOfPlay)
                        ResetWeights();
                    break;
                case HandSignState.Active:
                    HandleActiveHandSign(state);
                    break;
                case HandSignState.Cooldown:
                    if (Time.time > _nextHandSignTime) TransitionToState(HandSignState.Inactive);
                    break;
            }

            if (CanTriggerFakeout()) StartFakeoutRoutine();
            if (State == NinjaStruct.FakeOut) return;
            base.OnCalculationComplete(state);
        }

        private void HandleActiveHandSign(int state)
        {
            if (state == StateStruct.Throw)
            {
                ResetWeights();
                ChangeState(NinjaStruct.ShadowStep);
            }
            else if (state == StateStruct.OutOfPlay && _handSignState == HandSignState.Active)
            {
                ResetWeights();
                ChangeState(NinjaStruct.Substitution);
            }
        }
        
        public override void ChangeState(int newState)
        {
            if (newState == StateStruct.OutOfPlay)
            {
                ResetWeights();
                newState = NinjaStruct.OutOfPlay;
            }

            if (State == NinjaStruct.OutOfPlay) return;
            base.ChangeState(newState);
        }

        private void StartHandSign()
        {
            if (_handSignCoroutine != null)
            {
                AI.StopCoroutine(_handSignCoroutine);
            }

            _nextHandSignTime = Time.time + _handSignUtilityArgs.handSignCooldown;
            _handSignCoroutine = AI.StartCoroutine(LerpToHandSignPositionAndRotation(0.5f, 0, 1f));
            _handSignState = HandSignState.Preparing;
        }

        private IEnumerator LerpToHandSignPositionAndRotation(float duration, float fromValue, float toValue)
        {
            var elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                var t = Mathf.Clamp01(elapsedTime / duration);
                SetHandSignIKWeights(Mathf.Lerp(fromValue, toValue, t));
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            _handSignState = HandSignState.Active;

            while (_handSignState == HandSignState.Active)
            {
                MaintainHandSignPosition();
                yield return null;
            }

            Debug.Log("Resetting weights");
            ResetWeights();
        }

        private void SetHandSignIKWeights(float weight)
        {
            AI.ik.solvers.leftHand.SetIKPosition(_handSignUtilityArgs.handSignTarget.position);
            AI.ik.solvers.leftHand.SetIKRotation(_handSignUtilityArgs.handSignTarget.rotation);
            AI.ik.solvers.leftHand.SetIKPositionWeight(weight);
            AI.ik.solvers.leftHand.SetIKRotationWeight(weight);
        }

        private void MaintainHandSignPosition()
        {
            AI.ik.solvers.leftHand.SetIKPosition(_handSignUtilityArgs.handSignTarget.position);
            AI.ik.solvers.leftHand.SetIKRotation(_handSignUtilityArgs.handSignTarget.rotation);
        }

        private void ResetWeights()
        {
            if (_handSignCoroutine != null)
            {
                AI.StopCoroutine(_handSignCoroutine);
            }

            TransitionToState(HandSignState.Cooldown);
            AI.ik.solvers.leftHand.SetIKPositionWeight(0);
            AI.ik.solvers.leftHand.SetIKRotationWeight(0);
            TransitionToState(HandSignState.Cooldown);
        }

        private void TransitionToState(HandSignState newState)
        {
            if (newState == HandSignState.Cooldown)
            {
                _nextHandSignTime = Time.time + _handSignUtilityArgs.handSignCooldown;
            }

            _handSignState = newState;
        }

        public void SetNinja(NinjaHandSignUtilityArgs handSignUtilityArgs, FakeoutUtilityArgs fakeoutUtilityArgs,
            SmokeBombUtilityArgs smokeBombUtilityArgs)
        {
            _handSignUtilityArgs = handSignUtilityArgs;
            _fakeoutUtilityArgs = fakeoutUtilityArgs;
            _smokeBombUtilityArgs = smokeBombUtilityArgs;
            TimerManager.AddTimer(Random.Range(_fakeoutUtilityArgs.rollIntervalMin, _fakeoutUtilityArgs.rollIntervalMax), SetFakeoutCooldown);
        }

        public bool IsHandSigning()
        {
            HandleActiveHandSign(StateStruct.OutOfPlay);
            return _handSignState == HandSignState.Active;
        }
        private bool CanTriggerFakeout()
        {
            // Check if handsign is active or if fakeout cooldown is active
            if (IsHandSigning() || _isFakeoutCooldownActive) return false;
            return _fakeoutUtilityArgs.probabilityList.PickValue() > 0;
        }
        private void StartFakeoutRoutine()
        {
            _isFakeoutCooldownActive = true;
            TimerManager.AddTimer(Random.Range(_fakeoutUtilityArgs.rollIntervalMin, _fakeoutUtilityArgs.rollIntervalMax), SetFakeoutCooldown);
            ChangeState(NinjaStruct.FakeOut);
        }
        
        private void SetFakeoutCooldown()
        {
            _isFakeoutCooldownActive = false;
        }
        

    }
}