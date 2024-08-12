using System;
using UnityEngine;

namespace Unity.Template.VR.Multiplayer
{
    [Serializable]
    public class ThrowConfigurationSet
    {
        [SerializeField] private ThrowConfiguration[] _deviceConfigurations;
        public ThrowConfigurationSet(int deviceCount)
        {
            _deviceConfigurations = new ThrowConfiguration[deviceCount];
        }
        
        public ThrowConfiguration GetConfigForDevice(Device device)
        {
            if (_deviceConfigurations[(int)device] == null)
            {
                Debug.LogWarning("No ThrowConfiguration set for " + device.ToString());
                _deviceConfigurations[(int)device] = new ThrowConfiguration();
            }
            return _deviceConfigurations[(int)device];
        }

        public void SetConfigForDevice(Device device, ThrowConfiguration config)
        {
            _deviceConfigurations[(int)device] = config;
        }

        public void SetConfigs(ThrowConfiguration[] set)
        {
            _deviceConfigurations = set;
        }
    }
}
