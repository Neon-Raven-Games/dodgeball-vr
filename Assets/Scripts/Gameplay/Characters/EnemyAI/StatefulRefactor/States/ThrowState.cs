using System.Collections;
using Hands.SinglePlayer.EnemyAI.StatefulRefactor.BaseState;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Hands.SinglePlayer.EnemyAI.StatefulRefactor.States
{
    public class ThrowState : BaseAIState<ThrowUtilityArgs>, IDerivedAIState
    {
        private static readonly int _SThrow = Animator.StringToHash("Throw");
        public override int State => StateStruct.Throw;

        public ThrowState(DodgeballAI ai, AIStateController controller, ThrowUtilityArgs args) : base(ai, controller,
            args)
        {
        }

        public override void EnterState()
        {
            controller.UnsubscribeRolling();
            AI.RemoveCallbacksForThrowBall();
            AI.AddCallbackForThrowBall(ThrowBall);
            AI.animator.ResetTrigger(_SThrow);
            AI.animator.SetTrigger(_SThrow);
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

            var ball = BallPool.GetBall(ballPos);
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
            if (Args.leftBallIndex._currentDodgeball) Args.leftBallIndex.SetBallType(BallType.None);
            else if (Args.rightBallIndex._currentDodgeball) Args.rightBallIndex.SetBallType(BallType.None);
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
        
        

        private IEnumerator BallThrowRecovery()
        {
            while (AI.animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.95f)
            {
                yield return null;
            }

            ChangeState(StateStruct.Move);
        }

        private Vector3 ballPos;

        public override void UpdateState()
        {
            ballPos = GetCurrentBallPosition();
            AI.RotateToTargetManually(AI.CurrentTarget.gameObject);
        }


        private Vector3 GetCurrentBallPosition()
        {
            if (Args.leftBallIndex._currentDodgeball)
                return Args.leftBallIndex.BallPosition;

            if (Args.rightBallIndex._currentDodgeball)
                return Args.rightBallIndex.BallPosition;

            return Vector3.zero;
        }

        public override void OnTriggerEnter(Collider collider)
        {
        }

        public override void OnTriggerExit(Collision col)
        {
        }

        private readonly RaycastHit[] _hits = new RaycastHit[1];

        public Vector3 CalculateThrow(DodgeballAI dodgeballAI, Vector3 source, Vector3 target)
        {
            var direction = target - source;

            // do we need upward bias still?
            direction.y += Args.upwardBias;
            direction.x += Random.Range(-Args.aimRandomnessFactor, Args.aimRandomnessFactor);
            direction.y += Random.Range(-Args.aimRandomnessFactor, Args.aimRandomnessFactor);
            direction.z += Random.Range(-Args.aimRandomnessFactor, Args.aimRandomnessFactor);

            var throwForce = direction.normalized * CalculateThrowForce(dodgeballAI, direction.magnitude);
            return throwForce;
        }

        private float CalculateThrowForce(DodgeballAI dodgeballAI, float distance)
        {
            var baseForce = Args.testingThrowForce;

            var difficultyAdjustment = dodgeballAI.difficultyFactor * Args.difficultyThrowForceMultiplier;
            var distanceAdjustment = Mathf.Clamp(distance / Args.maxThrowDistance, 0.5f, 1.0f);
            var randomness = Random.Range(-Args.throwForceRandomness, Args.throwForceRandomness);
            var throwForce = baseForce + difficultyAdjustment + (distanceAdjustment * baseForce) + randomness;
            throwForce = Mathf.Clamp(throwForce, Args.minThrowForce, Args.maxThrowForce);
            return throwForce;
        }
    }
}