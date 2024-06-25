using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class DevController : MonoBehaviour
{
    [SerializeField] private InputActionAsset actionAsset;
    [SerializeField] private GameObject hmd;
    [SerializeField] private float analogThreshold = 0.2f;
    public CharacterController controller;
    public float speed = 5.0f;
    public float rotationSpeed = 100.0f;
    public Team team;

    private InputAction _moveForwardAction;
    private InputAction _lookAction;
    private Vector2 _moveInput;
    private Vector2 _lookInput;
    public NetworkPlayer networkPlayer;

    public Vector2 GetMoveInput() => _moveInput;
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

        // MoveToCamera();
    }

    private void FixedUpdate()
    {
        var gravity = Physics.gravity;
        controller.Move(gravity * Time.fixedDeltaTime);
    }

    private void HandleRotation()
    {
        controller.transform.Rotate(0, _lookInput.x * rotationSpeed * Time.fixedDeltaTime, 0);
    }

    private void HandleMovement()
    {
        if (Math.Abs(_moveInput.x) <= analogThreshold) _moveInput.x = 0;
        if (Math.Abs(_moveInput.y) <= analogThreshold) _moveInput.y = 0;
        var movement = new Vector3(_moveInput.x, 0, _moveInput.y).normalized;
        movement *= speed * Time.fixedDeltaTime;
        movement = hmd.transform.TransformDirection(movement);
        movement.y = 0;
        controller.Move(movement);
    }
}