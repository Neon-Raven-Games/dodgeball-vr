using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// todo, we need to account for the irl space of the player
public class DevController : Actor
{
    [SerializeField] private InputActionAsset actionAsset;
    [SerializeField] private Transform hmd;
    [SerializeField] private float analogThreshold = 0.2f;
    public CharacterController controller;
    public float speed = 5.0f;
    public float rotationSpeed = 100.0f;
    private InputAction _moveForwardAction;
    private InputAction _lookAction;
    private Vector2 _moveInput;
    private Vector2 _lookInput;
    [SerializeField] private AudioSource whistleSound;
    [SerializeField] private GameObject outOfBoundsArea;

    private void Start()
    {
        ConfigurationManager.throwConfigIndex = 
            SceneManager.GetActiveScene().buildIndex != 0 ? 1 : 0;
    }

    private void Awake()
    {
        _moveForwardAction = actionAsset.FindAction("XRI LeftHand Locomotion/Move", true);
        _lookAction = actionAsset.FindAction("XRI RightHand Interaction/Rotate Anchor", true);

        _moveForwardAction.performed += ctx => _moveInput = ctx.ReadValue<Vector2>();
        _lookAction.performed += ctx => _lookInput = ctx.ReadValue<Vector2>();

        _moveForwardAction.canceled += _ => _moveInput = Vector2.zero;
        _lookAction.canceled += _ => _lookInput = Vector2.zero;
        pivot = new GameObject("RotationPivot");
        pivot.transform.position = hmd.position;
    }

    private HandSide lastHandSideUi = HandSide.RIGHT;
    [SerializeField] private HandStateController leftHandStateController;
    [SerializeField] private HandStateController rightHandStateController;

    private void OnEnable()
    {
        _moveForwardAction.Enable();
        _lookAction.Enable();
        PopulateTeamObjects();
        return;
        leftHandStateController.SetInPlay(true);
        rightHandStateController.SetInPlay(true);
        leftHandStateController.uITrigger += SetLastHandSideUi;
        rightHandStateController.uITrigger += SetLastHandSideUi;
    }

    private void SetLastHandSideUi(HandSide obj)
    {
        if (lastHandSideUi != obj)
        {
            if (lastHandSideUi == HandSide.LEFT)
            {
                Debug.Log("Set last hand side ui to right");
                leftHandStateController.ChangeState(HandState.Idle);
                rightHandStateController.ChangeState(HandState.Laser);
            }
            else
            {
                Debug.Log("Set last hand side ui to left");
                rightHandStateController.ChangeState(HandState.Idle);
                leftHandStateController.ChangeState(HandState.Laser);
            }
        }

        lastHandSideUi = obj;
    }

    private void OnDisable()
    {
        // leftHandStateController.uITrigger -= SetLastHandSideUi;
        // rightHandStateController.uITrigger -= SetLastHandSideUi;
        _moveForwardAction.Disable();
        _lookAction.Disable();
    }

    private void Update()
    {
        HandleRotation();
        HandleMovement();
        // SetHasBall();
        if (IsOutOfPlay()) HandleOutOfPlay();
    }

    private void SetHasBall() =>
        hasBall = leftHandStateController.State == HandState.Grabbing
                  || rightHandStateController.State == HandState.Grabbing;


    internal override void SetOutOfPlay(bool value)
    {
        base.SetOutOfPlay(value);
        outOfBoundsArea.SetActive(value);
        if (!value) whistleSound.Play();
        
        // todo, this is not worth the performance hit
        if (!value)
        {
            // speed += 0.5f;
            // if (vignetteCoroutine != null) StopCoroutine(vignetteCoroutine);
            // volume.profile.TryGet<Vignette>(out var vignette);
            // vignette.intensity.value = 0f;
        }
        else
        {
            // speed -= 0.5f;
            // if (vignetteCoroutine != null) StopCoroutine(vignetteCoroutine);
            // vignetteCoroutine = StartCoroutine(LerpVignetteIntensity(vignetteIntensity, 0.2f));
        }
    }

    private GameObject pivot;
    public Transform cameraOffset;

    private void HandleRotation()
    {
        cameraOffset.RotateAround(hmd.position, Vector3.up, _lookInput.x * rotationSpeed * Time.fixedDeltaTime);
    }

    private void HandleMovement()
    {
        if (Math.Abs(_moveInput.x) <= analogThreshold) _moveInput.x = 0;
        if (Math.Abs(_moveInput.y) <= analogThreshold) _moveInput.y = 0;
        var movement = new Vector3(_moveInput.x, 0, _moveInput.y).normalized;
        movement *= speed * Time.fixedDeltaTime;
        movement = hmd.transform.TransformDirection(movement);

        var gravity = Physics.gravity;
        movement.y = gravity.y * Time.fixedDeltaTime;
        controller.Move(movement);
    }
}