using UnityEngine;

public class MovingUpAndDown : MonoBehaviour
{
    [SerializeField] private float amplitude = 0.5f; // The height of the hover
    [SerializeField] private float frequency = 1f;   // The speed of the hover
    private float offset;

    void Start()
    {
        offset = Random.Range(0f, Mathf.PI * 2); // Random offset to avoid synchronized movement
    }

    void Update()
    {
        float time = Time.time;
        Vector3 newPosition = transform.position;
        newPosition.y += Mathf.Sin(time * frequency + offset) * amplitude * Time.deltaTime;
        transform.position = newPosition;
    }

    void OnValidate()
    {
        if (amplitude < 0)
        {
            amplitude = 0;
        }
        if (frequency < 0)
        {
            frequency = 0;
        }
    }
}