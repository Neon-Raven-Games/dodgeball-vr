using CloudFine.ThrowLab;
using Fusion;
using Unity.Template.VR.Multiplayer;
using UnityEngine;
using UnityEngine.InputSystem;

public class HandController : MonoBehaviour
{
    [SerializeField] private NetworkPlayer networkPlayer;

    [SerializeField] private Transform grabTransform;
    [SerializeField] private InputActionAsset actionAsset;
    [SerializeField] private HandSide handSide;
    private LayerMask _ballLayer;
    private GameObject _ball;
    private bool _grabbing;
    private InputAction _gripAction;
    private Animator _animator;
    private static readonly int _SState = Animator.StringToHash("State");
    private DevController _controller;


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
            networkPlayer.SubscribeInput(SetNetGrab, LogGripRelease);
            _gripAction.performed += NetGripPerform;
            _gripAction.canceled += NetGripCancel;
        }
        else
        {
            _gripAction.performed += SetGrab;
            _gripAction.canceled += SetGrabReleased;
        }
    }

    private void NetGripCancel(InputAction.CallbackContext obj) =>
        networkPlayer.GripCancel();

    public void NetGripPerform(InputAction.CallbackContext e) =>
        networkPlayer.GripPerform();

    public void LogGripRelease() =>
        SetNetGrabReleased();


    #region single player grabs

    private void SetGrab(InputAction.CallbackContext e)
    {
        if (_ball && !_grabbing)
        {
            _grabbing = true;
            _animator.SetInteger(_SState, 1);
            _ball.GetComponent<DodgeBall>().SetOwner(_controller);
            var rb = _ball.GetComponent<Rigidbody>();
            if (!rb.isKinematic) rb.velocity = Vector3.zero;
            _grabbing = true;
        }
    }

    private void SetGrabReleased(InputAction.CallbackContext e)
    {
        if (_grabbing) _ball.GetComponent<DodgeBall>().SetLiveBall();
        _animator.SetInteger(_SState, 2);
        _ball = null;
        _grabbing = false;
    }

    #endregion

    private void SetNetGrab()
    {
        if (_ball && !_grabbing)
        {
            _grabbing = true;

            _animator = GetComponentInChildren<Animator>();
            _animator.SetInteger(_SState, 1);
            InitializeNetworkPossessedBall();
        }
    }

    private void InitializeNetworkPossessedBall()
    {
        var ball = _ball.GetComponent<NetDodgeball>();
        var ballIndex = ball.index;
        var ballType = ball.type;
        var ballTeam = ball.team;

        // set net possession, despawning original ball
        NetworkedPossession();

        // set the ball to a new ball and initialize it 
        var localBall = NetBallController.SpawnNewBall(grabTransform.position);
        
        _ball = localBall.gameObject;
        
        // set owner
        _ball.GetComponent<DodgeBall>().SetOwner(_controller);
        
        // disable host sync for local
        _ball.GetComponent<NetworkTransform>().enabled = false;
        
        // override the select enter to resume grab state

        // initialize the local values to extract on ThrowNetBallAfterTrajectory
        localBall.Initialize(ballType, Vector3.zero, ballIndex, ballTeam);

       var throwHandle = _ball.GetComponent<ThrowHandle>();
        throwHandle.OnAttach(gameObject, gameObject);
        
        // set it's velocity to zero if we are not kinematic
        var rb = _ball.GetComponent<Rigidbody>();
        if (!rb.isKinematic) rb.velocity = Vector3.zero;
        _ball = localBall.gameObject;
    }
 

    private void SetNetGrabReleased()
    {
        if (_grabbing)
        {
            _ball.GetComponent<DodgeBall>().SetLiveBall();
        }
        _animator.SetInteger(_SState, 2);
        _grabbing = false;
        if (_ball)
        {
            _ball.GetComponent<ThrowHandle>().onFinalTrajectory += ThrowNetBallAfterTrajectory;
            _ball.GetComponent<ThrowHandle>().OnDetach();
        }
    }

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
        _ball.transform.position = grabTransform.position;
        _ball.transform.rotation = grabTransform.rotation;
    }

    #region Network Dodgeball Methods

    private void ThrowNetBallAfterTrajectory(Vector3 velocity)
    {
        if (!_ball) return;
        _ball.GetComponent<ThrowHandle>().onFinalTrajectory -= ThrowNetBallAfterTrajectory;
        var dodgeBall = _ball.GetComponent<NetDodgeball>();
        if (!dodgeBall)
        {
            Debug.LogError("Dodgeball not found");
        }

        var dodgeballType = dodgeBall.type;
        var position = dodgeBall.transform.position;
        var possession = handSide == HandSide.RIGHT ? NetBallPossession.RightHand : NetBallPossession.LeftHand;
        networkPlayer.RPC_ThrownBall(dodgeballType, position, velocity, _controller.team, possession);
        
        Destroy(dodgeBall.gameObject);
        _ball = null;
    }
    
    private void NetworkedPossession()
    {
        var dodgeBall = _ball.GetComponent<NetDodgeball>();
        if (!dodgeBall)
        {
            Debug.LogError("Dodgeball not found");
        }

        networkPlayer.RPC_PossessBall(
            handSide == HandSide.RIGHT ? NetBallPossession.RightHand : NetBallPossession.LeftHand,
            dodgeBall.type, dodgeBall.GetComponent<NetDodgeball>().index);
    }

    #endregion
}