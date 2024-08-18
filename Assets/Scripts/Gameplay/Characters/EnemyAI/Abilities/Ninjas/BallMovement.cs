using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI.Abilities
{
    public class BallMovement : MonoBehaviour
    {
        private Transform planeTransform;
        private float travelTime = 2f;
        private float centerInflucence = 5f;
        private float distance;
        public void Initialize(Transform planeTransform, float travelTime, float centerInfluence, float distance)
        {
            this.centerInflucence = centerInfluence;
            this.distance = distance;
            this.travelTime = travelTime;
            this.planeTransform = planeTransform;
        }

        public void StartBallRoutine()
        {
            MoveBall().Forget();
        }

        private async UniTaskVoid MoveBall()
        {
            var startPosition = transform.position;
            var endPosition = startPosition + planeTransform.forward * distance; 
            var elapsedTime = 0f;

            var initialControlPointPosition = (startPosition + endPosition) / 2;
            var pointDistance = Vector3.Distance(transform.position, planeTransform.position);

            // Pick a random point above the plane position for the control point
            var randomHeight = Random.Range(.8f, 1.5f); // Adjust the range to control how high the arc is
            var controlPointOffset = new Vector3(initialControlPointPosition.x, planeTransform.position.y + randomHeight, initialControlPointPosition.z);
            controlPointOffset *= pointDistance * centerInflucence;
            
            while (elapsedTime < travelTime)
            {
                float t = elapsedTime / travelTime;
                Vector3 bezierPoint = BezierCurve(startPosition, controlPointOffset, endPosition, t);
                transform.position = bezierPoint;
                elapsedTime += Time.deltaTime;

                await UniTask.Yield();
            }

            transform.position = endPosition; 
        }

        private Vector3 BezierCurve(Vector3 start, Vector3 control, Vector3 end, float t)
        {
            return Mathf.Pow(1 - t, 2) * start + 2 * (1 - t) * t * control + Mathf.Pow(t, 2) * end;
        }
    }
}