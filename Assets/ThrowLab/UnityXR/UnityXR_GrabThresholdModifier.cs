using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

namespace CloudFine.ThrowLab.UnityXR
{
    [RequireComponent(typeof(XRBaseController))]
    public class UnityXR_GrabThresholdModifier : GrabThresholdModifier
    {
        float val = 0;
        private XRBaseController _controller;
        private XRController _xrController;
        private ActionBasedController _actionController;

        InputBinding myinput = new InputBinding();

        private float _grabThreshold;

        private void Awake()
        {
            _controller = GetComponent<XRBaseController>();
            _xrController = _controller as XRController;
            _actionController = _controller as ActionBasedController;
        }

        public override float GripValue()
        {
            if (_xrController)
            {
                switch (_xrController.selectUsage)
                {
                    case InputHelpers.Button.Grip:
                        _xrController.inputDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip, out val);
                        break;
                    case InputHelpers.Button.Trigger:
                        _xrController.inputDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out val);
                        break;
                    default:
                        Debug.LogWarning("XRController SelectUsage " + _xrController.selectUsage.ToString() + " not supported.", this);
                        break;
                }
            }
            if (_actionController)
            {
                val = _actionController.selectActionValue.reference.action.ReadValue<float>();
                //TODO
            }
            return val;
        }

        public override void SetGrabThreshold(float grip)
        {
            if (_xrController)
            {
                _xrController.axisToPressThreshold = grip;
            }
            if (_actionController)
            {
                myinput.overrideInteractions = string.Format("press(pressPoint={0})", grip);
                _actionController.selectAction.action.ApplyBindingOverride(myinput);
            }
            _grabThreshold = grip;
        }

        public override void SetReleaseThreshold(float grip)
        {
            if (_xrController)
            {
                //_xrController.axisToPressThreshold = grip;
            }
            if (_actionController)
            {
                InputSystem.settings.buttonReleaseThreshold = grip / _grabThreshold;
            }
        }
    }
}