using System.Collections.Generic;
using System.Linq;
using _dev.Dodgeballs;
using CloudFine.ThrowLab;
using CloudFine.ThrowLab.UI;
using Unity.Template.VR.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

public class DodgeballLab : MonoBehaviour
{
    [Header("Ball Handling")] public List<BallSpawner> ballSpawners;
    [Header("UI")] public UIThrowConfiguration configurationUI;
    public DeviceDetectorUI deviceDetector;
    public Image[] tabFills;
    public GameObject warningNoConfigs;

    private Device _device = Device.UNSPECIFIED;
    private Dictionary<ThrowConfiguration, ThrowConfiguration[]> _tempConfigVariants = new();
    private int _currentConfigIndex;
    private ThrowConfiguration[] _configSet;
    private ThrowConfiguration _original;
    private readonly bool[] _configEnabled = {true, false, false};

    private readonly Color[] _colorSet =
    {
        Color.cyan,
        Color.magenta,
        Color.yellow,
    };

    private void Awake()
    {
        deviceDetector.OnDeviceDetected += SetDevice;
    }
    
    public void SetThrowableConfig(ThrowHandle throwablePrefab)
    {
        _original = throwablePrefab.GetConfigForDevice(_device);
        ThrowConfiguration[] variants;
        if (_tempConfigVariants.ContainsKey(_original))
        {
            variants = _tempConfigVariants[_original];
        }
        else
        {
            variants = new ThrowConfiguration[3]
            {
                _original.Clone(),
                _original.Clone(),
                _original.Clone()
            };
            variants[0].name = _original.name + " A";
            variants[1].name = _original.name + " B";
            variants[2].name = _original.name + " C";

            _tempConfigVariants.Add(_original, variants);
        }
        _configSet = variants;

        if (useUi) LoadConfig(_currentConfigIndex);
    }

    public bool useUi;
    private void Start()
    {
        for (int i = 0; i < _configEnabled.Length; i++)
            SetConfigEnabled(i, _configEnabled[i]);
    }

    private void SetDevice(Device device)
    {
        _device = device;
        configurationUI.LoadConfig(_configSet[_currentConfigIndex], _colorSet[_currentConfigIndex],
            _configEnabled[_currentConfigIndex]);
    }

    public void LoadConfig(int i)
    {
        _currentConfigIndex = i;
        SetCurrentConfigEnabled(_configEnabled[i]);
    }
    
    public void LoadCurrentConfig(int i)
    {
        _currentConfigIndex = i;
        SetConfigEnabled(_currentConfigIndex, true);
    }
    
    public void SetCurrentConfigEnabled(bool enable)
    {
        SetConfigEnabled(_currentConfigIndex, enable);
        ReloadCurrentConfig();
    }

    private void SetConfigEnabled(int i, bool enable)
    {
        _configEnabled[i] = enable;
        if (tabFills.Length < i) tabFills[i].enabled = enable;

        var activeConfig = _configEnabled.Aggregate(false, (current, isEnabled) => current || isEnabled);
        if (warningNoConfigs) warningNoConfigs.SetActive(!activeConfig);
    }

    private void ReloadCurrentConfig()
    {
        configurationUI.LoadConfig(_configSet[_currentConfigIndex], _colorSet[_currentConfigIndex],
            _configEnabled[_currentConfigIndex]);
    }

    public void SaveCurrentConfig()
    {
        _configSet[_currentConfigIndex].CopyTo(_original);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(_original);
#else
        _original.SaveToJSON();
#endif
    }

    public void ResetCurrentConfig()
    {
        _original.CopyTo(_configSet[_currentConfigIndex]);
        LoadConfig(_currentConfigIndex);
    }
}