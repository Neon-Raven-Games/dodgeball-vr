using System;
using UnityEngine;
using UnityEngine.InputSystem;

// todo, we need to account for the irl space of the player
public class DevController : Actor
{
    [SerializeField] private InputActionAsset actionAsset;
    [SerializeField] private GameObject hmd;
    [SerializeField] private float analogThreshold = 0.2f;
    public CharacterController controller;
    public float speed = 5.0f;
    public float rotationSpeed = 100.0f;
    private InputAction _moveForwardAction;
    private InputAction _lookAction;
    private InputAction _runAction;
    private Vector2 _moveInput;
    private Vector2 _lookInput;

    public Vector2 GetMoveInput() => _moveInput;
    private void Awake()
    {
        _moveForwardAction = actionAsset.FindAction("XRI LeftHand Locomotion/Move", true);
        _lookAction = actionAsset.FindAction("XRI RightHand Locomotion/Turn", true);
        _runAction = actionAsset.FindAction("XRI LeftHand Locomotion/Run", true);

        _moveForwardAction.performed += ctx => _moveInput = ctx.ReadValue<Vector2>();
        _lookAction.performed += ctx => _lookInput = ctx.ReadValue<Vector2>();

        _moveForwardAction.canceled += _ => _moveInput = Vector2.zero;
        _lookAction.canceled += _ => _lookInput = Vector2.zero;
        
        _runAction.performed += ctx => speed = 8.5f;
        _runAction.canceled += ctx => speed = 5.0f;
    }

    private void OnEnable()
    {
        _moveForwardAction.Enable();
        _lookAction.Enable();
        PopulateTeamObjects();
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
        if (IsOutOfPlay()) HandleOutOfPlay();
        // MoveToCamera();
    }
    
    private void FixedUpdate()
    {
        var gravity = Physics.gravity;
        controller.Move(gravity * Time.fixedDeltaTime);
    }

    // todo, validate this working gewd :D
    private void HandleRotation()
    {
        controller.transform.RotateAround(hmd.transform.position, Vector3.up, _lookInput.x * rotationSpeed * Time.fixedDeltaTime);
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