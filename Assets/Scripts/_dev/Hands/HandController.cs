using CloudFine.ThrowLab;
using Fusion.Addons.Physics;
using Unity.Template.VR.Multiplayer;
using UnityEngine;
using UnityEngine.InputSystem;

// todo, after physics refactor, test the logical flow
// refactor for NetBallController may have to switch up logical flow a bit
public class HandController : MonoBehaviour
{
    [SerializeField] private NetworkPlayer networkPlayer;

    [SerializeField] private Transform grabTransform;
    [SerializeField] private InputActionAsset actionAsset;
    [SerializeField] private HandSide handSide;
    private LayerMask _ballLayer;
    internal GameObject _ball;
    private bool _grabbing;
    private InputAction _gripAction;
    private Animator _animator;
    private static readonly int _SState = Animator.StringToHash("State");
    private DevController _controller;
    private NetDodgeball _netDodgeball;

    private void OnDestroy()
    {
        if (networkPlayer != null)
        {
            _gripAction.performed -= NetRightGripPerform;
            _gripAction.canceled -= NetRightGripCancel;
            networkPlayer.UnsubscribeGrips();
        }
        else
        {
            _gripAction.performed -= SetGrab;
            _gripAction.canceled -= SetGrabReleased;
        }

        _gripAction = null;
    }

    private void Start()
    {
        _animator = GetComponentInChildren<Animator>();
        _controller = GetComponentInParent<DevController>();
        _ballLayer = LayerMask.NameToLayer("Ball");

        _gripAction =
            actionAsset.FindAction(
                handSide == HandSide.RIGHT ? "XRI RightHand Interaction/Select" : "XRI LeftHand Interaction/Select",
                true);
        if (networkPlayer != null)
        {
            // this does not account for both hand interactions
            // this will fire both of them.
            if (handSide == HandSide.RIGHT)
            {
                networkPlayer.SubscribeRightInput(SetGrab, SetGrabReleased);
                _gripAction.performed += NetRightGripPerform;
                _gripAction.canceled += NetRightGripCancel;
            }
        }
        else
        {
            _gripAction.performed += SetGrab;
            _gripAction.canceled += SetGrabReleased;
        }
    }

    private void SetGrabReleased()
    {
        SetGrabReleased(new InputAction.CallbackContext());
    }

    private void SetGrab()
    {
        SetGrab(new InputAction.CallbackContext());
    }

    private void NetRightGripCancel(InputAction.CallbackContext obj) =>
        networkPlayer.RightGripCancel();

    public void NetRightGripPerform(InputAction.CallbackContext e) =>
        networkPlayer.RightGripPerform();


    #region single player grabs

    private void SetGrab(InputAction.CallbackContext e)
    {
        if (_ball && !_grabbing)
        {
            _grabbing = true;
            _animator.SetInteger(_SState, 1);
            _ball.GetComponent<DodgeBall>().SetOwner(_controller);
            
            var rb = _ball.GetComponent<NetworkRigidbody3D>();
            if (!rb.Rigidbody.isKinematic) rb.Rigidbody.velocity = Vector3.zero;
            _grabbing = true;
            
            var throwHandle = _ball.GetComponent<ThrowHandle>();
            throwHandle.OnAttach(gameObject, gameObject);
            throwHandle.onFinalTrajectory += ThrowNetBallAfterTrajectory;
            
            if (!_netDodgeball) _netDodgeball = _ball.GetComponent<NetDodgeball>();
            if (!_netDodgeball) return;
            
            _netDodgeball.SetOwner(networkPlayer.Object, handSide == HandSide.RIGHT
                ? NetBallPossession.RightHand : NetBallPossession.LeftHand);
        }
    }

    private void SetGrabReleased(InputAction.CallbackContext e)
    {
        if (_grabbing)
        {
            _ball.GetComponent<DodgeBall>().SetLiveBall();
            _ball.GetComponent<ThrowHandle>().OnDetach();
        }

        _netDodgeball = null;
        _animator.SetInteger(_SState, 2);
        _grabbing = false;
    }

    #endregion

    private void OnTriggerEnter(Collider other)
    {
        if (!_ball && !_grabbing && other.gameObject.layer == _ballLayer)
            _ball = other.gameObject;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!networkPlayer && !_grabbing && other.gameObject.layer == _ballLayer)
            _ball = null;
    }

    private void LateUpdate()
    {
        if (!_grabbing || !_ball) return;
        if (!_netDodgeball) _netDodgeball = _ball.GetComponent<NetDodgeball>();
        if (!_netDodgeball) return;
        _netDodgeball.SetLocalOwnerPosition(grabTransform.position);
        
        _ball.transform.position = grabTransform.position;
        _ball.transform.rotation = grabTransform.rotation;
    }

    #region Network Dodgeball Methods

    private void ThrowNetBallAfterTrajectory(Vector3 velocity)
    {
        _ball.GetComponent<ThrowHandle>().onFinalTrajectory -= ThrowNetBallAfterTrajectory;
        var dodgeBall = _ball.GetComponent<NetDodgeball>();
        if (!dodgeBall)
        {
            Debug.LogError("Dodgeball not found");
        }

        Debug.Log("Throwing Ball!");
        dodgeBall.ThrowBall(transform.position, velocity);
        _ball = null;
        _netDodgeball = null;
    }

    #endregion
}