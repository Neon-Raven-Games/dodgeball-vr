using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI.Abilities
{
    public class BallMovement : MonoBehaviour
    {
        private Transform planeTransform;
        private float travelTime = 2f;

        public void Initialize(Transform planeTransform, float travelTime = 2f)
        {
            this.travelTime = travelTime;
            this.planeTransform = planeTransform;
            MoveBall().Forget();
        }

        private async UniTaskVoid MoveBall()
        {
            Vector3 startPosition = transform.position;
            Vector3 endPosition = startPosition + planeTransform.forward * 50f; 
            float elapsedTime = 0f;

            Vector3 initialControlPointPosition = (startPosition + endPosition) / 2;
            
            while (elapsedTime < travelTime)
            {
                float t = elapsedTime / travelTime;

                float distance = Vector3.Distance(transform.position, planeTransform.position);
                Vector3 controlPointOffset = (planeTransform.position - transform.position).normalized * distance * 0.5f; // Scale influence
                Vector3 dynamicControlPointPosition = initialControlPointPosition + controlPointOffset;

                Vector3 bezierPoint = BezierCurve(startPosition, dynamicControlPointPosition, endPosition, t);
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