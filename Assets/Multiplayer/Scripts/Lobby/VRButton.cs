using UnityEngine;
using UnityEngine.Events;

public class VRButton : MonoBehaviour
{
    [SerializeField] private float minLocalY = 0.25f;
    [SerializeField] private float maxLocalY = 0.55f;
    [SerializeField] private float clickTolerance = 0.01f;
    [SerializeField] private AudioSource buttonUpEmitter;
    [SerializeField] private AudioSource buttonDownEmitter;
    [SerializeField] private UnityEvent onButtonDown;
    [SerializeField] private UnityEvent onButtonUp;

    private SpringJoint _joint;
    private bool _clickingDown;
    private Vector3 _buttonDownPosition;
    private Vector3 _buttonUpPosition;

    private void Start()
    {
        _joint = GetComponent<SpringJoint>();
        _buttonDownPosition = GetButtonDownPosition();
        _buttonUpPosition = GetButtonUpPosition();
        _joint.spring = 1500;
        transform.localPosition = new Vector3(transform.localPosition.x, maxLocalY, transform.localPosition.z);
    }
    
    private void Update()
    {
        if (transform.localPosition.y < minLocalY) transform.localPosition = _buttonDownPosition;
        else if (transform.localPosition.y > maxLocalY) transform.localPosition = _buttonUpPosition;

        var buttonDownDistance = transform.localPosition.y - _buttonDownPosition.y;
        if (buttonDownDistance <= clickTolerance && !_clickingDown) OnButtonDown();
        
        var buttonUpDistance = _buttonUpPosition.y - transform.localPosition.y;
        if (buttonUpDistance <= clickTolerance && _clickingDown) OnButtonUp();
    }

    private Vector3 GetButtonUpPosition() => 
        new(transform.localPosition.x, maxLocalY, transform.localPosition.z);

    private Vector3 GetButtonDownPosition() => 
        new(transform.localPosition.x, minLocalY, transform.localPosition.z);

    private void OnButtonDown()
    {
        _clickingDown = true;
        if (buttonDownEmitter) buttonDownEmitter.Play();
        onButtonDown?.Invoke();
    }

    private void OnButtonUp()
    {
        _clickingDown = false;
        if (buttonUpEmitter) buttonUpEmitter.Play();
        onButtonUp?.Invoke();
    }

    // jank translation on transform point for some reason
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;

        var upLocalPosition = new Vector3(0, maxLocalY, 0);
        var downLocalPosition = new Vector3(0, minLocalY, 0);
        var upPosition = transform.TransformPoint(upLocalPosition);
        var downPosition = transform.TransformPoint(downLocalPosition);

        var size = new Vector3(0.005f, 0.005f, 0.005f);

        Gizmos.DrawCube(upPosition, size);

        Gizmos.color = Color.yellow;
        Gizmos.DrawCube(downPosition, size);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(upPosition, downPosition);
    }
}