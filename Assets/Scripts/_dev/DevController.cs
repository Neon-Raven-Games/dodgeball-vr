using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DevController : MonoBehaviour
{
    [SerializeField] private InputActionAsset actionAsset;
    [SerializeField] private GameObject hmd;
    

    public CharacterController controller;
    public float speed = 5.0f;
    public float rotationSpeed = 100.0f;
    public Team team;

    private InputAction _moveForwardAction;
    private InputAction _lookAction;
    private Vector2 _moveInput;
    private Vector2 _lookInput;
    
    private void Awake()
    {
        _moveForwardAction = actionAsset.FindAction("XRI LeftHand Locomotion/Move", true);
        _lookAction = actionAsset.FindAction("XRI RightHand Locomotion/Turn", true);

        _moveForwardAction.performed += ctx => _moveInput = ctx.ReadValue<Vector2>();
        _lookAction.performed += ctx => _lookInput = ctx.ReadValue<Vector2>();

        _moveForwardAction.canceled += _ => _moveInput = Vector2.zero;
        _lookAction.canceled += _ => _lookInput = Vector2.zero;
    }

    private void OnEnable()
    {
        _moveForwardAction.Enable();
        _lookAction.Enable();
    }

    private void OnDisable()
    {
        _moveForwardAction.Disable();
        _lookAction.Disable();
    }

    private void Update()
    {
        HandleMovement();
        HandleRotation();
    }

    private void HandleRotation()
    {
        transform.Rotate(0, _lookInput.x * rotationSpeed * Time.fixedDeltaTime, 0);
    }

    private void HandleMovement()
    {
        var movement = new Vector3(_moveInput.x, 0, _moveInput.y).normalized;
        movement *= speed * Time.fixedDeltaTime;
        movement = hmd.transform.TransformDirection(movement);
        movement.y = 0;
        controller.Move(movement);
    }
}
