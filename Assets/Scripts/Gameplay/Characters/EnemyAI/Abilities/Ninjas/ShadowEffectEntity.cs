using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Hands.SinglePlayer.EnemyAI.Abilities
{
    public class ShadowEffectEntity : MonoBehaviour
    {
        private Vector3[] controlPoints;
        [SerializeField] private float moveDuration = 3f;

        public void SetBezierCurve(Vector3[] controlPoints)
        {
            this.controlPoints = controlPoints;
            MoveAlongBezierCurve().Forget();
        }

        private async UniTaskVoid MoveAlongBezierCurve()
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / moveDuration;
                transform.position = CalculateBezierPoint(t, controlPoints[0], controlPoints[1], controlPoints[2]);
                await UniTask.Yield();
            }
            gameObject.SetActive(false);
        }
        
        Vector3 point = new Vector3();

        private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
        
            point = uu * p0; // u^2 * p0
            point += 2 * u * t * p1; // 2 * u * t * p1
            point += tt * p2; // t^2 * p2

            return point;
        }
    }
}