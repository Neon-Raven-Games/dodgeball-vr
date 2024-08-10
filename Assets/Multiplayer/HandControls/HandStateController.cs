using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public enum HandState
{
    Idle = 0,
    Grabbing = 1,
    Throwing = 2,
    Sucking = 3,
    Laser = 4
}

public class HandStateController : MonoBehaviour
{
    public Actor actor;
    private readonly Dictionary<HandState, BaseHandState> _handStates = new();
    public HandState State => _currentHandState.State;
    
    [Header("Throw Settings")]
    internal float throwStateTime = 0.5f;
    
    [Header("Grabbing Settings")] [SerializeField]
    internal Transform grabTransform;

    [SerializeField] internal Collider grabTriggerCollider;

    [Header("Sucking Settings")] [SerializeField]
    internal float suckingSnapDistance = 0.4f;

    [SerializeField] internal float suckingForce = 5f;
    [SerializeField] internal GameObject fxPrefab;

    [Header("Input Settings")] [SerializeField]
    private InputActionAsset actionAsset;

    [SerializeField] internal HandSide handSide;
    internal Animator animator;

    [Header("Laser Settings")]
    [SerializeField]
    internal VRUILaserSetup laserSetup;

    // States
    internal static readonly int SState = Animator.StringToHash("State");
    private BaseHandState _currentHandState;

    // Input
    private InputAction _gripAction;
    private InputAction _menuAction;
    private InputAction _menuSelectAction;
    internal GameObject ball;
    internal bool ui;
    public bool inPlay;
    
    public event Action<HandSide> uITrigger;

    public void SetInPlay(bool play)
    {
        inPlay = play;
        if (!inPlay && State == HandState.Grabbing) ChangeState(HandState.Throwing);
        if (!inPlay) fxPrefab.SetActive(false);
    }
    #region States

    private void Update() =>
        _currentHandState.OnStateUpdate();

    private void LateUpdate() =>
        _currentHandState.OnStateLateUpdate();
    
    private void OnTriggerEnter(Collider other) =>
        _currentHandState.OnTriggerEnter(other);
    
    private void OnTriggerStay(Collider other) =>
        _currentHandState.OnTriggerStay(other);
    
    private void OnTriggerExit(Collider other) =>
        _currentHandState.OnTriggerExit(other);
    
    public void ChangeState(HandState newState)
    {
        _currentHandState.OnStateExit();
        _currentHandState = _handStates[newState];
        _currentHandState.OnStateEnter();
    }

    #endregion

    #region initialization

    private void Start()
    {
        _handStates.Values.ToList().ForEach(state => state.OnStateStart());
        _currentHandState.OnStateEnter();
    }

    private void Awake()
    {
        PopulateInput();
        laserSetup = GetComponentInChildren<VRUILaserSetup>();
        if (!laserSetup) Debug.LogWarning($"No laser found on {handSide} hand controller.");
        animator = GetComponentInChildren<Animator>();
        
        InitializeStates();
    }

    private void InitializeStates()
    {
        _handStates.Add(HandState.Idle, new IdleHandState(this));
        _handStates.Add(HandState.Sucking, new SuckingHandState(this));
        _handStates.Add(HandState.Grabbing, new GrabbingHandState(this));
        _handStates.Add(HandState.Throwing, new ThrowingHandState(this));
        _handStates.Add(HandState.Laser, new LaserHandState(this));

        foreach (var state in _handStates.Values) state.OnStateAwake();
        _currentHandState = _handStates[HandState.Idle];
    }

    private void OnDisable()
    {
        _gripAction.performed -= GripPerformedAction;
        _gripAction.canceled -= GripCancelledAction;
        _menuSelectAction.performed -= TriggerPerformAction;
        _menuSelectAction.canceled -= TriggerReleasedAction;
        _gripAction.Disable();
        _menuSelectAction.Disable();
        
        if (handSide == HandSide.LEFT)
        {
            _menuAction.performed -= MenuPerformAction;
            _menuAction.Disable();
        }
    }

    private void OnEnable()
    {
        _gripAction.performed += GripPerformedAction;
        _gripAction.canceled += GripCancelledAction;
        _menuSelectAction.performed += TriggerPerformAction;
        _menuSelectAction.canceled += TriggerReleasedAction;
        _gripAction.Enable();
        _menuSelectAction.Enable();
        
        if (handSide == HandSide.LEFT)
        {
            _menuAction.performed += MenuPerformAction;
            _menuAction.Enable();
        }
        laserSetup.gameObject.SetActive(false);
    }

    private void PopulateInput()
    {
        var handSideString = "LeftHand";
        if (handSide == HandSide.RIGHT) handSideString = "RightHand";

        _menuSelectAction = actionAsset.FindAction($"XRI {handSideString} Interaction/UI Press");
        _menuAction = actionAsset.FindAction($"XRI {handSideString}/Menu");
        _gripAction = actionAsset.FindAction($"XRI {handSideString} Interaction/Select", true);
    }

    #endregion

    #region inputs

    private void GripPerformedAction(InputAction.CallbackContext obj)
    {
        if (!inPlay) return;
        _currentHandState.OnGrab();
    }

    private void GripCancelledAction(InputAction.CallbackContext obj)
    {
        if (!inPlay) return;
        _currentHandState.OnGrabRelease();
    }

    // make sure the menu state does not go out of sync with the actual menu
    private void MenuPerformAction(InputAction.CallbackContext obj)
    {
        _currentHandState.MenuButton();
    }

    private void TriggerPerformAction(InputAction.CallbackContext obj)
    {
        if (!ui) return;
        
        uITrigger?.Invoke(handSide);
        _currentHandState.OnUITrigger();
        
        if (_currentHandState.State != HandState.Laser) return;
        laserSetup.OnUITrigger();
    }

    private void TriggerReleasedAction(InputAction.CallbackContext obj)
    {
        if (!ui) return;
        _currentHandState.OnUITriggerRelease();
        if (_currentHandState.State != HandState.Laser) return;
        laserSetup.OnUITriggerRelease();
    }

    #endregion

    public Camera LaserCamera() =>
        laserSetup.laserCamera;
}