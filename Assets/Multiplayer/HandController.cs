using FishNet.Object;
using Unity.Template.VR.Multiplayer;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hands
{
    public class HandController : MonoBehaviour
    {
        [SerializeField] private Transform grabTransform;
        [SerializeField] private InputActionAsset actionAsset;
        [SerializeField] internal HandSide handSide;
        [SerializeField] private float suckSnapDistance = 0.4f;
        [SerializeField] private float suckSnapSpeed = 5f;
        [SerializeField] private GameObject fxPrefab;
        private LayerMask _ballLayer;
        private GameObject _ball;
        private bool _grabbing;
        private InputAction _gripAction;
        private Animator _animator;

        private static readonly int _SState = Animator.StringToHash("State");

        // had to serialize it because the component for colliders needed to move, which is not parented
        [SerializeField] private DevController controller;


        public bool networked;


        private void Awake()
        {
            return;
            _animator = GetComponentInChildren<Animator>();
            _ballLayer = LayerMask.NameToLayer("Ball");

            _gripAction =
                actionAsset.FindAction(
                    handSide == HandSide.RIGHT ? "XRI RightHand Interaction/Select" : "XRI LeftHand Interaction/Select",
                    true);
        }

        private void OnEnable()
        {
            _gripAction.performed += SetGrab;
            _gripAction.canceled += SetGrabReleased;
        }

        #region single player grabs

        private int _ballIndex;

        private void SetGrab(InputAction.CallbackContext e)
        {
            if (controller.IsOutOfPlay()) return;
            
            fxPrefab.SetActive(true);
            if (_ball && _ball.layer != _ballLayer) return;
            if (_ball && !_grabbing)
            {
                controller.hasBall = true;
                _animator.SetInteger(_SState, 1);

                var dodgeBall = _ball.GetComponent<DodgeBall>();
                // dodgeBall.SetOwner(controller.team);
                dodgeBall.SetParticleActive(false);
                var rb = _ball.GetComponent<Rigidbody>();
                if (!rb.isKinematic) rb.velocity = Vector3.zero;
                _grabbing = true;
                var throwHandle = _ball.GetComponent<ThrowHandle>();
                throwHandle.OnAttach(gameObject, gameObject);

                if (networked)
                {
                    throwHandle.onFinalTrajectory += ThrowNetBallAfterTrajectory;

                    var netDodgeball = _ball.GetComponent<NetDodgeball>();
                    ServerOwnershipManager.RequestOwnershipFromServer(netDodgeball,
                        controller.GetComponentInParent<NetworkBehaviour>().LocalConnection, handSide);
                }
            }
        }

        private void SetGrabReleased(InputAction.CallbackContext e)
        {
            fxPrefab.SetActive(false);
            if (_grabbing)
            {
                var dodgeBall = _ball.GetComponent<DodgeBall>();
                dodgeBall.SetLiveBall();
                dodgeBall.SetParticleActive(true);
                _ball.GetComponent<ThrowHandle>().OnDetach();
                controller.hasBall = false;

                // this is set after ownership on net
                if (!networked) _ball = null;
            }

            // this is set after ownership on net
            if (!networked) _grabbing = false;
            _animator.SetInteger(_SState, 2);
        }

        #endregion

        private void OnTriggerEnter(Collider other)
        {
            if (!_ball && 
                (!_grabbing || fxPrefab.activeInHierarchy) && 
                other.gameObject.layer == _ballLayer)
            {
                _ball = other.gameObject;
            }
            
        }

        private void OnTriggerExit(Collider other)
        {
            if (!_grabbing && other.gameObject.layer == _ballLayer)
                _ball = null;
        }

        private void Update()
        {
            
            if (!_grabbing || !_ball || !fxPrefab.activeInHierarchy) return;
            if (fxPrefab.activeInHierarchy &&
                Vector3.Distance(_ball.transform.position, grabTransform.position) > suckSnapDistance)
            {
                _ball.transform.position = Vector3.Lerp(_ball.transform.position, grabTransform.position,
                    Time.fixedDeltaTime * suckSnapSpeed);
                _ball.transform.rotation = Quaternion.Lerp(_ball.transform.rotation, grabTransform.rotation,
                    Time.fixedDeltaTime * suckSnapSpeed);
            }
            else
            {
                SetGrab(default);
                fxPrefab.SetActive(false);
            }
        }

        private void LateUpdate()
        {
            if (!_ball || !_grabbing) return;
            _ball.transform.position = grabTransform.position;
            _ball.transform.rotation = grabTransform.rotation;
        }

        #region Network Dodgeball Methods

        private void ThrowNetBallAfterTrajectory(Vector3 velocity)
        {
            if (!_ball) return;

            _grabbing = false;
            _ball.GetComponent<ThrowHandle>().onFinalTrajectory -= ThrowNetBallAfterTrajectory;
            _ball.GetComponent<NetDodgeball>()
                .ApplyThrowVelocityServerRpc(velocity, _ball.transform.position, handSide);

            Debug.Log($"Final server trajectory: {velocity}, position: {_ball.transform.position}");
            _ball = null;
        }

        #endregion
    }
}