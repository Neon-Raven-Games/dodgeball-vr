using System;
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
        [SerializeField] private Transform networkHeadTarget;

        [SerializeField] private NetBallPossessionHandler leftBallIndex;
        [SerializeField] private NetBallPossessionHandler rightBallIndex;

        #region inputs

        // todo, need movement animations hooked up here
        private NetIKTargetHelper _netIKTargetHelper;

        private bool _gripPreformed;
        private Action _gripPerformedAction;

        private bool _gripCancelled;
        private Action _gripCancelledAction;

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

            // todo, think this is obsolete
            InitializeClient(gameObject);

            if (IsOwner)
            {
                ServerOwnershipManager.AddPlayer(OwnerId);
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
            UpdateNetBallPossessions();

            if (!initialized || !IsOwner) return;
            UpdateHostNetModels();
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