using System.Collections;
using Gameplay.Util;
using Hands.SinglePlayer.EnemyAI.StatefulRefactor.BaseState;
using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI.StatefulRefactor.NinjaStates
{
    public class NinjaOutOfPlayState : BaseAIState<NinjaOutOfPlayArgs>
    {
        public override int State => NinjaStruct.OutOfPlay;

        public NinjaOutOfPlayState(DodgeballAI ai, AIStateController controller, NinjaOutOfPlayArgs args) : base(ai,
            controller, args)
        {
        }

        public override void OnTriggerExit(Collision col)
        {
        }

        private Vector3[] controlPoints;

        private Vector3 GetRandomPointInBounds()
        {
            var bounds = new Bounds(AI.friendlyTeam.playArea.position,
                new Vector3(AI.friendlyTeam.playArea.localScale.x, 5,
                    AI.friendlyTeam.playArea.localScale.z));
            
            Vector3 randomPoint;
            do
            {
                randomPoint = new Vector3(
                    Random.Range(bounds.min.x, bounds.max.x),
                    Random.Range(bounds.min.y, bounds.max.y),
                    Random.Range(bounds.min.z, bounds.max.z)
                );
            } while (!bounds.Contains(randomPoint));

            return randomPoint;
        }

        Vector3 point = new();

        private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            var u = 1 - t;
            var tt = t * t;
            var uu = u * u;

            point = uu * p0;
            point += 2 * u * t * p1;
            point += tt * p2;

            return point;
        }

        private IEnumerator MoveAlongBezierCurve(Transform ninjaTransform)
        {
            var t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Args.jumpDuration;
                ninjaTransform.transform.position =
                    CalculateBezierPoint(t, controlPoints[0], controlPoints[1], controlPoints[2]);
                yield return null;
            }

            AI.transform.position = controlPoints[2];
            yield return new WaitForSeconds(0.3f);
            Args.trailRenderer.SetActive(false);
            AI.stayIdle = false;
            AI.SetOutOfPlay(false);
            AI.animator.Rebind();
            controller.SubscribeRolling();
            controller.Rebind();
        }


        public void QueueJump(Transform ninja)
        {
            var targetPoint = GetRandomPointInBounds();
            targetPoint.y = 0.11f;
            var apexPoint = new Vector3(targetPoint.x, Args.jumpHeight, targetPoint.z);
            controlPoints = new[] {AI.transform.position, apexPoint, targetPoint};
            Args.trailRenderer.SetActive(true);
            AI.StartCoroutine(MoveAlongBezierCurve(ninja));
        }

        public override void EnterState()
        {
            AI.stayIdle = true;
            controller.UnsubscribeRolling();
            if (AI.hasBall) AI.DropBall();
            AI.ik.solvers.rightHand.SetIKPositionWeight(0);
            AI.ik.solvers.rightHand.SetIKRotationWeight(0);
            AI.ik.solvers.lookAt.SetLookAtWeight(0);
            AI.animator.SetFloat(DodgeballAI._SXAxis, 0);
            AI.animator.SetFloat(DodgeballAI._SYAxis, 0);
            TimerManager.AddTimer(Args.respawnTime, SpawnIn);
        }

        private void SpawnIn()
        {
            AI.gameObject.SetActive(false);
            AI.transform.position = AI.spawnInPos[Random.Range(0, AI.spawnInPos.Count)].position;
            AI.gameObject.SetActive(true);
            AI.animator.Rebind();
            
            QueueJump(AI.transform);
        }

        public override void FixedUpdate()
        {
        }

        public override void ExitState()
        {
        }

        public override void UpdateState()
        {
        }

        public override void OnTriggerEnter(Collider collider)
        {
        }
    }
}