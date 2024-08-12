using UnityEngine;

public class RotateBalls : MonoBehaviour
{
    [Tooltip("Prefab of the ball to instantiate")]
    public GameObject ballPrefab;

    [Tooltip("Radius of the circular path")]
    public float radius = 5f;

    [Tooltip("Speed of rotation in degrees per second")]
    public float rotationSpeed = 30f;

    [SerializeField] private float yPosition = 0.5f;
    private GameObject[] balls;
    private float[] angles;

    void Start()
    {
        balls = new GameObject[5];
        angles = new float[balls.Length];
        float angleStep = 360f / balls.Length;

        for (int i = 0; i < balls.Length; i++)
        {
            float angle = i * angleStep;
            angles[i] = angle;
            Vector3 position = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0, Mathf.Sin(angle * Mathf.Deg2Rad)) * radius;
            position.y = yPosition;
            balls[i] = Instantiate(ballPrefab, transform.position + position, Quaternion.identity);
            balls[i].transform.parent = transform;
            balls[i].GetComponent<Rigidbody>().isKinematic = true;
        }
    }

    void Update()
    {
        for (int i = 0; i < balls.Length; i++)
        {
            angles[i] += rotationSpeed * Time.deltaTime;
            if (angles[i] >= 360f) angles[i] -= 360f;

            float angleInRadians = angles[i] * Mathf.Deg2Rad;
            Vector3 position = new Vector3(Mathf.Cos(angleInRadians), 0, Mathf.Sin(angleInRadians)) * radius;
            position.y = yPosition;

            balls[i].transform.position = transform.position + position;
        }
    }
}