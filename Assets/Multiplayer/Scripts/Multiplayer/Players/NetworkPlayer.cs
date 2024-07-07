using System;
using FishNet.Object;
using Unity.Template.VR.Multiplayer.Server;
using UnityEngine;

[Serializable]
public class PlayerRig
{
    public GameObject playerModel;
    public Transform hmdTarget;
    public Transform leftHandTarget;
    public Transform rightHandTarget;
}
// blend shapes:
// boy = 0
// girl = 100

namespace Unity.Template.VR.Multiplayer
{
    public class NetworkPlayer : NetworkClientXRObject
    {
        [SerializeField] private PlayerRig localPlayer;
        [SerializeField] public PlayerRig ikTargetModel;
        [SerializeField] private Transform networkPlayerTarget;
        [SerializeField] private Transform networkHeadTarget;

        [SerializeField] private NetBallPossessionHandler leftBallIndex;
        [SerializeField] private NetBallPossessionHandler rightBallIndex;

        #region inputs

        private NetIKTargetHelper _netIKTargetHelper;

        private bool _gripPreformed;
        private Action _gripPerformedAction;

        private bool _gripCancelled;
        private Action _gripCancelledAction;

        public void GripPreform()
        {
            _gripPreformed = true;
        }

        public void GripCancel()
        {
            _gripCancelled = true;
        }

        public void SubscribeInput(Action gripPerformed, Action gripCancelled)
        {
            _gripPerformedAction = gripPerformed;
            _gripCancelledAction = gripCancelled;
        }

        public void UnsubscribeGrips()
        {
            _gripCancelledAction = null;
            _gripPerformedAction = null;
        }

        private void UpdateNetBallPossessions()
        {
            leftBallIndex.UpdatePossession(OwnerId);
            rightBallIndex.UpdatePossession(OwnerId);
        }

        #endregion

        private void OnDrawGizmos()
        {
            if (gameObject == null) return;
            Gizmos.color = IsOwner ? Color.green : Color.red;

            Gizmos.DrawSphere(ikTargetModel.rightHandTarget.position, 0.1f);
            Gizmos.DrawSphere(ikTargetModel.leftHandTarget.position, 0.1f);
            Gizmos.DrawSphere(ikTargetModel.hmdTarget.position, 0.1f);
        }

        private bool initialized;

        public override void OnStartClient()
        {
            base.OnStartClient();

            // this will boot strap the VR controller for the XR
            // rig if not on the server
            InitializeClient(gameObject);

            if (IsOwner)
            {
                localPlayer.playerModel.SetActive(true);
                ikTargetModel.playerModel.SetActive(false);
            }
            else
            {
                localPlayer.playerModel.SetActive(false);
                ikTargetModel.playerModel.SetActive(true);
                _netIKTargetHelper = ikTargetModel.playerModel.GetComponent<NetIKTargetHelper>();
            }

            initialized = true;
        }

        public void FixedUpdate()
        {
            // todo, I think this needs to be by !owner   
            // double check this when server is up
            // UpdateNetBallPossessions();

            if (!initialized || !IsOwner) return;
            UpdateHostNetModels();
        }

        // I don't think we need this if we use the hand controller. Dodgeballs should update
        private void InvokeActions()
        {
            if (_gripPreformed)
            {
                _gripPreformed = false;
                _gripPerformedAction?.Invoke();
            }

            if (_gripCancelled)
            {
                _gripCancelled = false;
                _gripCancelledAction?.Invoke();
            }
        }

        #region IKTargets

        private void UpdateHostNetModels()
        {
            UpdateLeftHand();
            UpdateRightHand();
            UpdateHead();
        }

        private void UpdateHead()
        {
            MoveNetIKTarget(networkHeadTarget.transform, localPlayer.hmdTarget.position);
            RotateNextIKTarget(networkHeadTarget, localPlayer.hmdTarget.rotation);
        }

        private void UpdateLeftHand()
        {
            var dxOffset = new Vector3(90, 90, -180);
            RotateNextIKTarget(ikTargetModel.leftHandTarget, localPlayer.leftHandTarget.rotation, dxOffset);
            MoveNetIKTarget(ikTargetModel.leftHandTarget, localPlayer.leftHandTarget.position);
        }

        private void UpdateRightHand()
        {
            var dzOffset = new Vector3(90, -90, 0);
            RotateNextIKTarget(ikTargetModel.rightHandTarget, localPlayer.rightHandTarget.rotation, dzOffset);
            MoveNetIKTarget(ikTargetModel.rightHandTarget, localPlayer.rightHandTarget.position);
        }

        private static void MoveNetIKTarget(Transform ikTransform, Vector3 playerTransform)
        {
            ikTransform.position =
                Vector3.Lerp(ikTransform.position, playerTransform, Time.deltaTime * 10);
        }

        private static void RotateNextIKTarget(Transform ikTransform, Quaternion rotation, Vector3 rotationOffset)
        {
            ikTransform.rotation =
                Quaternion.Lerp(ikTransform.rotation, rotation * Quaternion.Euler(rotationOffset),
                    Time.deltaTime * 10);
        }

        private static void RotateNextIKTarget(Transform ikTransform, Quaternion rotation)
        {
            ikTransform.rotation =
                Quaternion.Lerp(ikTransform.rotation, rotation,
                    Time.deltaTime * 10);
        }

        #endregion
    }
}