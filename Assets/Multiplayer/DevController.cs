using System;
using System.Collections;
using System.Collections.Generic;
using Hands;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit;
using Vignette = UnityEngine.Rendering.PostProcessing.Vignette;

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
    private Vector2 _moveInput;
    private Vector2 _lookInput;
    [FormerlySerializedAs("postProcessVolume")] [SerializeField] Volume volume;

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
        if (IsOutOfPlay())
        {
            HandleOutOfPlay();
        }
        // MoveToCamera();
    }

    private Coroutine vignetteCoroutine;
    [SerializeField] private float vignetteIntensity = 0.3f;
    [SerializeField] private float vignetteEntryTime = 0.5f;

    private IEnumerator LerpVignetteIntensity(float startIntensity, float endIntensity)
    {
        volume.profile.TryGet<UnityEngine.Rendering.Universal.Vignette>(out var vignette);
        volume.profile.TryGet(out WhiteBalance whiteBalance);
        if (vignette == null || whiteBalance == null)
        {
            Debug.LogError($"Vignette or WhiteBalance not found in post process volume.\nVignette:{vignette}, WhiteBalance:{whiteBalance}");
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

        vignette.intensity.value = endIntensity;
    }

    [SerializeField] private List<XRDirectInteractor> handComponentsToDisable;
    [SerializeField] private List<HandController> handControllers;
    internal override void SetOutOfPlay(bool value)
    {
        base.SetOutOfPlay(value);
        if (!value)
        {
            handComponentsToDisable.ForEach(x => x.enabled = true);
            handControllers.ForEach(x => x.enabled = true);
            
            if (vignetteCoroutine != null) StopCoroutine(vignetteCoroutine);
            volume.profile.TryGet<UnityEngine.Rendering.Universal.Vignette>(out var vignette);
            vignette.intensity.value = 0f;
            Debug.Log("Setting out of play");
            // Disable hand controllers here
        }
        else
        {
            handComponentsToDisable.ForEach(x => x.enabled = false);
            handControllers.ForEach(x => x.enabled = false);
            if (vignetteCoroutine != null) StopCoroutine(vignetteCoroutine);
            vignetteCoroutine = StartCoroutine(LerpVignetteIntensity(vignetteIntensity, 0f));
            // enable hand controllers here
        }
    }

    private void FixedUpdate()
    {
        var gravity = Physics.gravity;
        controller.Move(gravity * Time.fixedDeltaTime);
    }

    // todo, validate this working gewd :D
    private void HandleRotation()
    {
        controller.transform.RotateAround(hmd.transform.position, Vector3.up,
            _lookInput.x * rotationSpeed * Time.fixedDeltaTime);
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