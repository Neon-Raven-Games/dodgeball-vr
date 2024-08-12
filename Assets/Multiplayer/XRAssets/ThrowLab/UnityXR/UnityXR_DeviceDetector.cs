using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR;

namespace CloudFine.ThrowLab.UnityXR
{
    [RequireComponent(typeof(XRBaseController))]
    public class UnityXR_DeviceDetector : DeviceDetector
    {
        private Device _device = Device.UNSPECIFIED;
        private bool _deviceLoaded = false;


        private void Update()
        {
            if (_deviceLoaded) return;

            InputDevice inputDevice = InputDevices.GetDeviceAtXRNode(Side == HandSide.LEFT ? XRNode.LeftHand : XRNode.RightHand);

            if (inputDevice == null) return;
            if (inputDevice.name == null) return;

            string deviceString = inputDevice.name;
            _device = GetDeviceFromName(deviceString);

            OnControllerTypeDetermined(_device);
            _deviceLoaded = true;
        }

        private Device GetDeviceFromName(string deviceString)
        {
            if (deviceString.IndexOf("vive", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return Device.VIVE;
            }
            if (deviceString.IndexOf("knuckles", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return Device.KNUCKLES;
            }
            if (deviceString.IndexOf("oculus", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return Device.OCULUS_TOUCH;
            }
            if (deviceString.IndexOf("windows", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return Device.WINDOWS_MR;
            }

            return Device.UNSPECIFIED;
        }
       
    }
}
