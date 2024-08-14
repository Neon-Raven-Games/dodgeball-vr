using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum CannonState
{
    Idle,
    Sucking,
    Shooting,
}
public class HandCannon : MonoBehaviour
{
    internal List<DodgeBall> dodgeBallAmmo = new();
    [SerializeField] private InputActionAsset actionAsset;
    public Transform barrelTransform;
    
    [Header("Shooting Settings")]
    public LineRenderer trajectoryLineRenderer;
    public float launchForce = 20f;
    public int trajectoryPoints = 8;
    
    [Header("Sucking Settings")]
    public float suctionForce = 10f;
    public float swirlRadius = 1f;
    public float swirlSpeed = 2f;
    public float suctionCooldown = 1f;
    public float suctionDuration = 2f; // Added suction duration
    public float ballEndScale = 0.4f;

    private Dictionary<CannonState, BaseHandCanonState> _states;
    private BaseHandCanonState _currentState;
    private InputAction _gripAction;
    private InputAction _triggerAction;
    private void Start()
    {
        _states = new Dictionary<CannonState, BaseHandCanonState>
        {
            {CannonState.Sucking, new SuckingState(this)},
            {CannonState.Shooting, new ShootingState(this)},
            {CannonState.Idle, new IdleState(this)}
        };
        _currentState = _states[CannonState.Idle];
        _currentState.EnterState();
    }
    private void PopulateInput()
    {
        // var handSideString = "LeftHand";
        // if (handSide == HandSide.RIGHT) 
           
        var handSideString = "RightHand";

        _triggerAction = actionAsset.FindAction($"XRI {handSideString} Interaction/UI Press");
        _gripAction = actionAsset.FindAction($"XRI {handSideString} Interaction/Select", true);

        _triggerAction.performed += GripPerformedAction;
        _triggerAction.canceled += GripReleasedAction;
        
        _gripAction.performed += TriggerPerformedAction;
        _gripAction.canceled += TriggerReleasedAction;
    }
    public void AddDodgeBall(DodgeBall dodgeBall)
    {
        dodgeBallAmmo.Add(dodgeBall);
    }
    
    public void ChangeState(CannonState state)
    {
        _currentState?.ExitState();
        _currentState = _states[state];
        _currentState.EnterState();
    }

    public void GripPerformedAction(InputAction.CallbackContext obj)
    {
        _currentState?.GripAction();
    }

    public void GripReleasedAction(InputAction.CallbackContext obj)
    {
        _currentState?.GripReleaseAction();
    }

    public void TriggerPerformedAction(InputAction.CallbackContext obj)
    {
        _currentState?.FireAction();
    }
    
    private void TriggerReleasedAction(InputAction.CallbackContext obj)
    {
        _currentState?.FireReleaseAction();
    }
    
    private void Update()
    {
        _currentState?.Update();
    }
    
    private void OnEnable()
    {
        PopulateInput();
        _triggerAction.Enable();
        _gripAction.Enable();
    }
    
    private void OnDisable()
    {
        _triggerAction.performed -= GripPerformedAction;
        _triggerAction.canceled -= GripReleasedAction;
        _triggerAction.Disable();
        _gripAction.Disable();
    }


    private void OnTriggerEnter(Collider other)
    {
        _currentState?.OnTriggerEnter(other);
    }
    
    private void OnTriggerExit(Collider other)
    {
        _currentState?.OnTriggerExit(other);
    }

    private void OnTriggerStay(Collider other)
    {
        _currentState?.OnTriggerStay(other);
    }

#if UNITY_EDITOR
    
    private void OnDrawGizmos()
    {
        _currentState?.OnDrawGizmos();
    }
    
    #endif
}
