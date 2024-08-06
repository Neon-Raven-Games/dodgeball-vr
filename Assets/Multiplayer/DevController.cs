using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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
    [SerializeField] Volume volume;

    public Vector2 GetMoveInput() => _moveInput;

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
        leftHandStateController.uITrigger -= SetLastHandSideUi;
        rightHandStateController.uITrigger -= SetLastHandSideUi;
        _moveForwardAction.Disable();
        _lookAction.Disable();
    }

    private void Update()
    {
        HandleRotation();
        HandleMovement();
        SetHasBall();
        if (IsOutOfPlay()) HandleOutOfPlay();
    }

    private void SetHasBall() =>
        hasBall = leftHandStateController.State == HandState.Grabbing
                  || rightHandStateController.State == HandState.Grabbing;


    private Coroutine vignetteCoroutine;
    [SerializeField] private float vignetteIntensity = 0.3f;
    [SerializeField] private float vignetteEntryTime = 0.5f;

    private IEnumerator LerpVignetteIntensity(float startIntensity, float endIntensity)
    {
        volume.profile.TryGet<Vignette>(out var vignette);
        volume.profile.TryGet(out WhiteBalance whiteBalance);
        if (vignette == null || whiteBalance == null)
        {
            Debug.LogError(
                $"Vignette or WhiteBalance not found in post process volume.\nVignette:{vignette}, WhiteBalance:{whiteBalance}");
            yield break;
        }

        var elapsed = 0f;
        while (elapsed < vignetteEntryTime)
        {
            vignette.intensity.value = Mathf.Lerp(endIntensity, startIntensity, elapsed / vignetteEntryTime);
            elapsed += Time.deltaTime;
        }

        elapsed = 0f;
        outOfBoundsEndTime = Time.time + outOfBoundsWaitTime;
        while (elapsed < 1)
        {
            elapsed = 1f - Mathf.Clamp01((outOfBoundsEndTime - Time.time) / outOfBoundsWaitTime);
            vignette.intensity.value = Mathf.Lerp(startIntensity, endIntensity, elapsed);
            whiteBalance.temperature.value = Mathf.Lerp(-80, 0, elapsed);
            yield return null;
        }

        vignette.intensity.value = 0;
    }

    internal override void SetOutOfPlay(bool value)
    {
        base.SetOutOfPlay(value);
        Debug.Log($"setting out of play, hand state in play: {!value}");
        leftHandStateController.SetInPlay(!value);
        rightHandStateController.SetInPlay(!value);
        if (!value)
        {
            speed += 0.5f;

            if (vignetteCoroutine != null) StopCoroutine(vignetteCoroutine);
            volume.profile.TryGet<Vignette>(out var vignette);
            vignette.intensity.value = 0f;
        }
        else
        {
            speed -= 0.5f;
            if (vignetteCoroutine != null) StopCoroutine(vignetteCoroutine);
            vignetteCoroutine = StartCoroutine(LerpVignetteIntensity(vignetteIntensity, 0.2f));
        }
    }

    private GameObject pivot;

    private void HandleRotation()
    {
        pivot.transform.position = hmd.position;

        pivot.transform.position = hmd.position;
        pivot.transform.Rotate(Vector3.up, _lookInput.x * rotationSpeed * Time.fixedDeltaTime);
        Vector3 offset = controller.transform.position - hmd.position;

        controller.transform.position = pivot.transform.position + pivot.transform.rotation * offset;
        controller.transform.rotation = Quaternion.Euler(0, pivot.transform.rotation.eulerAngles.y, 0);
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