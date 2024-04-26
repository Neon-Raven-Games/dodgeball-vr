using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DevController : MonoBehaviour
{
    [SerializeField] private InputActionAsset actionAsset;
    [SerializeField] private GameObject hmd;
    
    private InputAction moveForwardAction;
    private InputAction lookAction;
    private Vector2 moveInput;
    private Vector2 lookInput;

    public CharacterController controller;
    public float speed = 5.0f;
    public float rotationSpeed = 100.0f;
    public Team team;

    private void Awake()
    {
        moveForwardAction = actionAsset.FindAction("XRI LeftHand Locomotion/Move", true);
        lookAction = actionAsset.FindAction("XRI RightHand Locomotion/Turn", true);

        moveForwardAction.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        lookAction.performed += ctx => lookInput = ctx.ReadValue<Vector2>();

        moveForwardAction.canceled += _ => moveInput = Vector2.zero;
        lookAction.canceled += _ => lookInput = Vector2.zero;
    }

    private void OnEnable()
    {
        moveForwardAction.Enable();
        lookAction.Enable();
    }

    private void OnDisable()
    {
        moveForwardAction.Disable();
        lookAction.Disable();
    }

    private void Update()
    {
        HandleMovement();
        HandleRotation();
    }

    private void HandleRotation()
    {
        transform.Rotate(0, lookInput.x * rotationSpeed * Time.fixedDeltaTime, 0);
    }

    private void HandleMovement()
    {
        var movement = new Vector3(moveInput.x, 0, moveInput.y).normalized;
        movement *= speed * Time.fixedDeltaTime;
        movement = hmd.transform.TransformDirection(movement);
        movement.y = 0;
        controller.Move(movement);
    }
}
