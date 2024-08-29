using Cysharp.Threading.Tasks;
using UnityEngine;

public class NinjaJump : MonoBehaviour
{
    public Transform treeSpawnPoint;
    [SerializeField] private Transform playArea;
    [SerializeField] private float jumpHeight = 5f;
    [SerializeField] private float jumpDuration = 1f;
    [SerializeField] private float respawnTime = 1f;

    private Vector3[] controlPoints;

    private Vector3 GetRandomPointInBounds()
    {
        var bounds = new Bounds(playArea.position,
            new Vector3(playArea.localScale.x, 5, playArea.localScale.z));
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

    private async UniTaskVoid MoveAlongBezierCurve(Transform ninjaTransform)
    {
        var t = 0f;
        ninjaTransform.gameObject.SetActive(false);
        await UniTask.WaitForSeconds(respawnTime);
        ninjaTransform.gameObject.SetActive(true);

        while (t < 1f)
        {
            t += Time.deltaTime / jumpDuration;
            ninjaTransform.transform.position =
                CalculateBezierPoint(t, controlPoints[0], controlPoints[1], controlPoints[2]);
            await UniTask.Yield();
        }

        ninjaTransform.GetComponent<NinjaAgent>().stayIdle = false;
    }


    public void QueueJump(Transform ninja)
    {
        lock (ninja)
        {
            var targetPoint = GetRandomPointInBounds();
            var startPoint = treeSpawnPoint.position;
            targetPoint.y = 0.11f;
            var apexPoint = new Vector3(targetPoint.x, jumpHeight, targetPoint.z);
            controlPoints = new[] {startPoint, apexPoint, targetPoint};
            MoveAlongBezierCurve(ninja).Forget();
        }
    }
}