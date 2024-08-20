using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Hands.SinglePlayer.EnemyAI.Abilities
{
    public class BallMovement : MonoBehaviour
    {
        private Transform planeTransform;
        private float travelTime = 2f;
        private float centerInflucence = 5f;
        private float distance;
        private Rigidbody rb;
        private Collider collider;
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            collider = GetComponent<Collider>();
        }
        
        
        
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

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("TeamOne") ||
                other.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                rb.isKinematic = false;
                rb.velocity = Vector3.Reflect(rb.velocity * 5f, transform.forward);
                collider.isTrigger = false;
            }
        }

        private async UniTaskVoid MoveBall()
        {
            rb.isKinematic = true;
            collider.isTrigger = true;
            
            var startPosition = transform.position;
            var endPosition = startPosition + planeTransform.forward * distance; 
            var elapsedTime = 0f;

            var initialControlPointPosition = (startPosition + endPosition) / 2;
            var pointDistance = Vector3.Distance(transform.position, planeTransform.position);

            // Pick a random point above the plane position for the control point
            var randomHeight = Random.Range(.8f, 1.5f); // Adjust the range to control how high the arc is
            var controlPointOffset = new Vector3(initialControlPointPosition.x, planeTransform.position.y + randomHeight, initialControlPointPosition.z);
            controlPointOffset *= pointDistance * centerInflucence;
            
            while (elapsedTime < travelTime && rb.isKinematic)
            {
                float t = elapsedTime / travelTime;
                Vector3 bezierPoint = BezierCurve(startPosition, controlPointOffset, endPosition, t);
                transform.LookAt(bezierPoint);
                transform.position = bezierPoint;
                elapsedTime += Time.deltaTime;

                await UniTask.Yield();
                if (!rb.isKinematic) return;
            }

            rb.isKinematic = false;
            collider.isTrigger = false;
        }

        private Vector3 BezierCurve(Vector3 start, Vector3 control, Vector3 end, float t)
        {
            return Mathf.Pow(1 - t, 2) * start + 2 * (1 - t) * t * control + Mathf.Pow(t, 2) * end;
        }
    }
}