using System;
using CloudFine.ThrowLab;
using UnityEngine;
using UnityEngine.InputSystem;

public class HandController : MonoBehaviour
{
    [SerializeField] private Transform grabTransform;
    [SerializeField] private InputActionAsset actionAsset;
    [SerializeField] private HandSide handSide;
    private LayerMask _ballLayer;
    private GameObject _ball;
    private bool _grabbing;
    private InputAction _gripAction;
    private bool _grip;
    private Animator _animator;
    private static readonly int _SState = Animator.StringToHash("State");
    private DevController _controller;

    private void Start()
    {
        _controller = GetComponentInParent<DevController>();
        _animator = GetComponentInChildren<Animator>();
        _ballLayer = LayerMask.NameToLayer("Ball");
        _gripAction =
            actionAsset.FindAction(
                handSide == HandSide.RIGHT ? "XRI RightHand Interaction/Select" : "XRI LeftHand Interaction/Select",
                true);

        _gripAction.performed += _ => SetGrab();
        _gripAction.canceled += _ => SetGrabReleased();
    }

    private void SetGrab()
    {
        if (_ball && !_grabbing)
        {
            _animator.SetInteger(_SState, 1);
            _ball.GetComponent<DodgeBall>().SetOwner(_controller);
            _ball.GetComponent<Rigidbody>().velocity = Vector3.zero;
            _grabbing = true;
        }
        _grip = true;
    }

    private void SetGrabReleased()
    {
        if (_ball && _grabbing) _ball.GetComponent<DodgeBall>().SetLiveBall();
        _animator.SetInteger(_SState, 2);
        _ball = null;
        _grip = false;
        _grabbing = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_grabbing && other.gameObject.layer == _ballLayer)
            _ball = other.gameObject;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == _ballLayer) _ball = null;
    }

    private void LateUpdate()
    {
        if (!_grabbing || !_ball) return;
        _ball.transform.position = grabTransform.position;
        _ball.transform.rotation = grabTransform.rotation;
    }
}