using UnityEngine;

public class AlienSpinnyThingy : MonoBehaviour
{
    [SerializeField] private float spinSpeed = 1.0f;
    [SerializeField] private float bobSpeed = 1.0f;
    [SerializeField] private float bobHeight = 0.5f;

    private Vector3 initialPosition;

    private void Start()
    {
        initialPosition = transform.position;
    }

    private void Update()
    {
        // Rotate around the local up axis
        gameObject.transform.Rotate(Vector3.up, spinSpeed);

        // Bobbing up and down
        var newY = initialPosition.y + Mathf.Sin(Time.unscaledTime * bobSpeed) * bobHeight;
        transform.position = new Vector3(initialPosition.x, newY, initialPosition.z);
    }
}