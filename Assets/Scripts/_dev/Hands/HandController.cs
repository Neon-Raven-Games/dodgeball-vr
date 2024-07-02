using CloudFine.ThrowLab;
using Fusion;
using Fusion.Addons.Physics;
using Unity.Template.VR.Multiplayer;
using UnityEngine;
using UnityEngine.InputSystem;

public struct GrabData : INetworkInput
{
    public int ballIndex;
    public Vector3 position;
    public Vector3 velocity;
    public NetBallPossession possession;
}
// todo, after physics refactor, test the logical flow
// refactor for NetBallController may have to switch up logical flow a bit
public class HandController : MonoBehaviour
{
    [SerializeField] private NetworkPlayer networkPlayer;

    [SerializeField] private Transform grabTransform;
    [SerializeField] private InputActionAsset actionAsset;
    [SerializeField] internal HandSide handSide;
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
            _gripAction.performed -= NetGripPerform;
            _gripAction.canceled -= NetGripCancel;
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
            networkPlayer.SubscribeInput(SetGrab, SetGrabReleased);
            _gripAction.performed += NetGripPerform;
            _gripAction.canceled += NetGripCancel;
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

    private void NetGripCancel(InputAction.CallbackContext obj) =>
        networkPlayer.GripCancel();

    public void NetGripPerform(InputAction.CallbackContext e) =>
        networkPlayer.GripPreform();


    #region single player grabs

    public void SetGrabData(Vector3 velocity, NetBallPossession possession)
    {
        networkPlayer.SetGrabData(
        new GrabData
        {
            ballIndex = _ballIndex,
            position = grabTransform.position,
            velocity = velocity,
            possession = possession
        });
    }

    private int _ballIndex;
    private void SetGrab(InputAction.CallbackContext e)
    {
        if (_ball && !_grabbing)
        {
            _ballIndex = _ball.GetComponent<NetDodgeball>().index;
            
            Debug.Log("Setting ball with index: " + _ballIndex);
            
            _animator.SetInteger(_SState, 1);
            _ball.GetComponent<DodgeBall>().SetOwner(_controller);

            var rb = _ball.GetComponent<NetworkRigidbody3D>();
            rb.RBIsKinematic = true;
            if (!rb.Rigidbody.isKinematic) rb.Rigidbody.velocity = Vector3.zero;
            rb.Teleport(grabTransform.position);
            _grabbing = true;
            SetGrabData(Vector3.zero, handSide == HandSide.RIGHT
                            ? NetBallPossession.RightHand
                            : NetBallPossession.LeftHand);
            var throwHandle = _ball.GetComponent<ThrowHandle>();
            throwHandle.OnAttach(gameObject, gameObject);
            throwHandle.onFinalTrajectory += ThrowNetBallAfterTrajectory;

            if (!_netDodgeball) _netDodgeball = _ball.GetComponent<NetDodgeball>();
            if (!_netDodgeball) return;

            _netDodgeball.SetOwner(networkPlayer.Object, handSide == HandSide.RIGHT
                ? NetBallPossession.RightHand
                : NetBallPossession.LeftHand);
        }
    }

    private void SetGrabReleased(InputAction.CallbackContext e)
    {
        if (_grabbing)
        {
            _ball.GetComponent<NetworkRigidbody3D>().RBIsKinematic = false;
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
        
        SetGrabData(Vector3.zero, handSide == HandSide.RIGHT
            ? NetBallPossession.RightHand
            : NetBallPossession.LeftHand);
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

        SetGrabData(velocity, NetBallPossession.None);
        _ballIndex = -1;
        _netDodgeball = null;
        _ball = null;
    }

    #endregion
}