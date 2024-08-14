using UnityEngine;

public class Cannon : MonoBehaviour
{
    [SerializeField] private GameObject dodgeballPrefab;
    [SerializeField] private Transform barrelTransform;
    [SerializeField] private LineRenderer trajectoryLineRenderer;
    [SerializeField] private int trajectoryPoints = 30;
    [SerializeField] private float launchForce = 10f;

    private void Update()
    {
        DrawTrajectory();
    }

    
    public void LaunchDodgeball()
    {
        GameObject dodgeball = Instantiate(dodgeballPrefab, barrelTransform.position, barrelTransform.rotation);
        Rigidbody rb = dodgeball.GetComponent<Rigidbody>();
        rb.AddForce(barrelTransform.forward * launchForce, ForceMode.Impulse);
    }

    private void DrawTrajectory()
    {
        Vector3[] points = new Vector3[trajectoryPoints];
        Vector3 startPosition = barrelTransform.position;
        Vector3 velocity = barrelTransform.forward * launchForce;

        for (int i = 0; i < trajectoryPoints; i++)
        {
            float time = i * 0.1f;
            points[i] = startPosition + velocity * time + Physics.gravity * time * time / 2f;
        }

        trajectoryLineRenderer.positionCount = trajectoryPoints;
        trajectoryLineRenderer.SetPositions(points);
    }
}