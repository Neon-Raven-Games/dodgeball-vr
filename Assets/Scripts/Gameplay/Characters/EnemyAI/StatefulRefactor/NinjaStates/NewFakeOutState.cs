using System.Collections;
using Hands.SinglePlayer.EnemyAI.StatefulRefactor.BaseState;
using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI.StatefulRefactor.NinjaStates
{
    public class NewFakeOutState : BaseAIState<FakeoutUtilityArgs>
    {
        public override int State => NinjaStruct.FakeOut;
        private IEnumerator RunFakeOutAppearEffect()
        {
            AI.rightBallIndex.SetBallType(BallType.Dodgeball);
            var dodgeball = AI.rightBallIndex._currentDodgeball;
            dodgeball.transform.localScale = Vector3.zero;
            var time = 0f;

            while (time < 1)
            {
                dodgeball.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, time);
                time += Time.deltaTime / Args.entryDuration;
                yield return null;
            }

            dodgeball.transform.localScale = Vector3.one;
            Args.fakeoutBall.gameObject.SetActive(true);
            AI.PickUpBall(Args.fakeoutBall);
            AI.SetPossessedBall(Args.fakeoutBall);
            Args.fakeoutBall.gameObject.SetActive(false);
            Args.entryEffect.SetActive(false);
            yield return null;
            AI.animator.SetTrigger(_SThrow);
        }

        public NewFakeOutState(DodgeballAI ai, AIStateController controller, FakeoutUtilityArgs args) : base(ai,
            controller, args)
        {
        }
        private static readonly int _SThrow = Animator.StringToHash("Throw");

        public override void EnterState()
        {
            AI.hasBall = true;
            AI.animator.SetFloat(_SYAxis, 0);
            AI.animator.SetFloat(_SXAxis, 0);

            controller.UnsubscribeRolling();
            AI.RemoveCallbacksForThrowBall();
            AI.StartCoroutine(RunFakeOutAppearEffect());
            AI.AddCallbackForThrowBall(ThrowBall);
        }
        private void ThrowBall()
        {
            AI.RemoveCallbacksForThrowBall();

            var actor = AI.ActorTarget;
            Vector3 enemyHeadPos = Vector3.zero;
            if (actor != null)
            {
                if (actor.head) enemyHeadPos = actor.head.position;
                else
                {
                    enemyHeadPos = AI.CurrentTarget.transform.position;
                    enemyHeadPos.y += 1.5f;
                }
            }

            var ball = Args.fakeoutBall;
            ball.transform.position = ballPos;
            ball.SetOwner(AI);

            var velocity = CalculateThrow(AI, ballPos, enemyHeadPos);
            ball.transform.position += velocity.normalized;
            ball.gameObject.SetActive(true);
            var rb = ball.GetComponent<Rigidbody>();
            rb.velocity = velocity;
            

            ball.HandleThrowTrajectory(velocity);
            ball.SetLiveBall();
            ball.team = AI.team;

            AI.StartCoroutine(BallThrowRecovery());
            if (AI.leftBallIndex._currentDodgeball) AI.leftBallIndex.SetBallType(BallType.None);
            else if (AI.rightBallIndex._currentDodgeball) AI.rightBallIndex.SetBallType(BallType.None);
        }
        public Vector3 CalculateThrow(DodgeballAI ai, Vector3 pos, Vector3 enemyHeadPos)
        {
            var direction = (enemyHeadPos - pos).normalized;
            var distance = Vector3.Distance(pos, enemyHeadPos);
            var time = distance / Args.throwSpeed;
            var gravity = Physics.gravity.y;
            var velocity = direction * Args.throwSpeed + Vector3.up * 0.5f;
            return velocity;
        }
        
        private IEnumerator BallThrowRecovery()
        {
            while (AI.animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.95f)
            {
                yield return null;
            }

            ChangeState(StateStruct.Move);
        }
 
        public override void OnTriggerExit(Collision col)
        {
        }

        private Vector3 ballPos;
        private Vector3 GetCurrentBallPosition()
        {
            if (AI.leftBallIndex._currentDodgeball)
                return AI.leftBallIndex.BallPosition;

            if (AI.rightBallIndex._currentDodgeball)
                return AI.rightBallIndex.BallPosition;

            return Vector3.zero;
        }
        public override void UpdateState()
        {
            controller.moveState.FlockMove(AI);
            ballPos = GetCurrentBallPosition();
            if (!AI.ActorTarget) controller.targetModule.UpdateTarget();
            AI.RotateToTargetManually(AI.CurrentTarget.gameObject);
        }

        public override void FixedUpdate()
        {
        }

        public override void ExitState()
        {
            AI.animator.ResetTrigger(_SThrow);
            AI.RemoveCallbacksForThrowBall();
            controller.SubscribeRolling();
        }

        public override void OnTriggerEnter(Collider collider)
        {
        }
    }
}