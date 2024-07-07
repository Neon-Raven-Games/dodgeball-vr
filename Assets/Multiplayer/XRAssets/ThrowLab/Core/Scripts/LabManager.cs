using CloudFine.ThrowLab.UI;
using System.Collections.Generic;
using System.Linq;
using Unity.Template.VR.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

namespace CloudFine.ThrowLab
{
    public class LabManager : MonoBehaviour
    {
        [Header("Spawn")] public List<ThrowHandle> _throwablePrefabs;
        public Transform _spawnPoint;
        public ParticleSystem _spawnEffect;
        private ThrowHandle _throwablePrefab;
        public ThrowTracker _trackerPrefab;

        [Header("UI")] public UIThrowConfiguration _configurationUI;
        public DeviceDetectorUI _deviceDetector;
        public RectTransform _trackerUIListRoot;
        public Text _throwableLabel;

        [Header("Lines")] public Texture2D[] _lineTextures;

        public Color[] _lineColors = new Color[]
        {
            Color.cyan,
            Color.magenta,
            Color.yellow
        };

        [Header("Variants")] public GameObject variantPanelRoot;
        public Image[] tabFills;
        public Button variantResetButton;
        public Button variantSaveButton;
        public GameObject warningNoConfigs;
        public Toggle variantEnabledToggle;
        public Toggle variantLineEnabledToggle;
        public Toggle variantSamplesEnabledToggle;

        private int _throwableIndex = -1;
        private Device _device = Device.UNSPECIFIED;
        private List<ThrowTracker> _trackers = new List<ThrowTracker>();

        private Dictionary<ThrowConfiguration, ThrowConfiguration[]> _tempConfigVariants =
            new Dictionary<ThrowConfiguration, ThrowConfiguration[]>();

        private ThrowHandle _currentSpawn;

        private int currentConfigIndex;
        private ThrowConfiguration[] configSet;
        private ThrowConfiguration original;

        private Color[] colorSet = new Color[]
        {
            Color.cyan,
            Color.magenta,
            Color.yellow,
        };

        private bool[] configEnabled = new bool[3]
        {
            true, false, false
        };

        private bool[] showSamples = new bool[3]
        {
            true, false, false
        };

        private bool[] showLine = new bool[3]
        {
            true, true, true
        };

        private void Awake()
        {
            _deviceDetector.OnDeviceDetected += SetDevice;
        }

        private void Start()
        {
            if (_throwablePrefabs.Count > 0)
            {
                SelectThrowable(0);
            }
            else
            {
                if (_throwableLabel)
                {
                    _throwableLabel.text = "(none)";
                }
            }

            for (int i = 0; i < configEnabled.Length; i++)
            {
                SetConfigEnabled(i, configEnabled[i]);
            }
        }

        private void Update()
        {
            if (_currentSpawn)
            {
                if (Vector3.Distance(_currentSpawn.transform.position, _spawnPoint.position) > .3f)
                {
                    SpawnTrackedThrowable();
                }
            }
            else
            {
                SpawnTrackedThrowable();
            }
        }


        private void SetDevice(Device device)
        {
            _device = device;
            SelectThrowable(_throwableIndex);
        }

        public void SpawnTrackedThrowable()
        {
            if (_throwablePrefab)
            {
                List<ThrowHandle> throwableSet = new List<ThrowHandle>();
                ThrowHandle primaryHandle = null;

                ThrowConfiguration[] configVariants = _tempConfigVariants[_throwablePrefab.GetConfigForDevice(_device)];


                for (int i = 0; i < 3; i++)
                {
                    if (!configEnabled[i]) continue;


                    ThrowHandle handle = GameObject.Instantiate(_throwablePrefab);
                    throwableSet.Add(handle);


                    handle.SetConfigForDevice(_device, configVariants[i]);

                    if (primaryHandle == null)
                    {
                        primaryHandle = handle;
                        primaryHandle.transform.position = _spawnPoint.position;
                    }
                    else
                    {
                        handle.transform.SetParent(primaryHandle.transform);
                        handle.transform.localPosition = Vector3.zero;
                        handle.transform.localRotation = Quaternion.identity;
                        handle.name = handle.name + "_" + i;

                        handle.SetPhysicsEnabled(false);

                        primaryHandle.onDetachFromHand += (handle.OnDetach);
                        primaryHandle.onPickUp += (handle.OnAttach);
                    }

                    if (_trackerPrefab)
                    {
                        ThrowTracker tracker = Instantiate(_trackerPrefab);
                        
                        tracker.SetColor(colorSet[i]);
                        tracker.TrackThrowable(handle);
                        tracker.SetLineAppearance(_lineTextures[i], _lineColors[i]);
                        tracker.ShowHideLine(showLine[i]);
                        tracker.ShowHideSamples(showSamples[i]);
                        tracker.AttachUIToRoot(_trackerUIListRoot);
                        
                        _trackers.Add(tracker);
                    }
                }

                // todo, isolate this logic and create a no collision object
                //Each of these throwables will have a rigidbody, so make sure they will ignore eachother.
                for (int i = 0; i < throwableSet.Count; i++)
                {
                    for (int j = 0; j < throwableSet.Count; j++)
                    {
                        if (i == j) continue;
                        if (throwableSet[i] == null || throwableSet[j] == null) continue;

                        throwableSet[i].IgnoreCollisionWithOther(throwableSet[j].gameObject, true);
                    }
                }


                if (_spawnEffect) _spawnEffect.Play();

                _currentSpawn = primaryHandle;
            }
        }

        private void RespawnThrowable()
        {
            if (_currentSpawn != null) Destroy(_currentSpawn.gameObject);

            SpawnTrackedThrowable();
        }

        public void SetCurrentConfigEnabled(bool enable)
        {
            SetConfigEnabled(currentConfigIndex, enable);
            variantLineEnabledToggle.interactable = enable;
            variantSamplesEnabledToggle.interactable = enable;
            ReloadCurrentConfig();
            RespawnThrowable();
        }

        private void SetConfigEnabled(int i, bool enable)
        {
            configEnabled[i] = enable;
            tabFills[i].enabled = enable;

            var activeConfig = configEnabled
                .Aggregate(false, (current, t) => current || t);

            warningNoConfigs.SetActive(!activeConfig);
        }

        public void SetCurrentLineEnabled(bool enable) =>
            SetLineEnabled(currentConfigIndex, enable);

        private void SetLineEnabled(int i, bool enable) =>
            showLine[i] = enable;

        public void SetCurrentSampleVisEnabled(bool enable) =>
            SetSampleVisualizationEnabled(currentConfigIndex, enable);

        private void SetSampleVisualizationEnabled(int i, bool enable) =>
            showSamples[i] = enable;

        public void SaveCurrentConfig()
        {
            configSet[currentConfigIndex].CopyTo(original);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(original);
#else
            original.SaveToJSON();
#endif
        }

        public void ResetCurrentConfig()
        {
            original.CopyTo(configSet[currentConfigIndex]);
            LoadConfig(currentConfigIndex);
        }

        public void ClearAll()
        {
            foreach (var tracker in _trackers) tracker.Cleanup();
            _trackers.Clear();
        }

        public void Reset() =>
            SpawnTrackedThrowable();

        public void CycleThrowableRight()
        {
            if (_throwableIndex < 0) return;
            
            _throwableIndex++;
            
            if (_throwableIndex >= _throwablePrefabs.Count) _throwableIndex = 0;
            SelectThrowable(_throwableIndex);
        }

        public void CycleThrowableLeft()
        {
            if (_throwableIndex < 0) return;
            
            _throwableIndex--;
            if (_throwableIndex < 0) _throwableIndex = _throwablePrefabs.Count - 1;

            SelectThrowable(_throwableIndex);
        }

        private void SelectThrowable(int i)
        {
            if (i < 0 || i >= _throwablePrefabs.Count) return;

            _throwableIndex = i;
            _throwablePrefab = _throwablePrefabs[i];

            original = _throwablePrefab.GetConfigForDevice(_device);
            ThrowConfiguration[] variants;
            if (_tempConfigVariants.ContainsKey(original))
            {
                variants = _tempConfigVariants[original];
            }
            else
            {
                variants = new ThrowConfiguration[3]
                {
                    original.Clone(),
                    original.Clone(),
                    original.Clone()
                };
                variants[0].name = original.name + " A";
                variants[1].name = original.name + " B";
                variants[2].name = original.name + " C";

                _tempConfigVariants.Add(original, variants);
            }

            configSet = variants;
            LoadConfig(currentConfigIndex);

            if (_throwableLabel)
            {
                _throwableLabel.text = _throwablePrefab.name;
            }

            RespawnThrowable();
        }

        public void LoadConfig(int i)
        {
            currentConfigIndex = i;
            ReloadCurrentConfig();
            variantEnabledToggle.isOn = configEnabled[i];
            SetCurrentConfigEnabled(configEnabled[i]);
            variantLineEnabledToggle.isOn = showLine[i];
            SetCurrentLineEnabled(showLine[i]);
            variantSamplesEnabledToggle.isOn = (showSamples[i]);
            SetCurrentSampleVisEnabled(showSamples[i]);
        }

        public void ReloadCurrentConfig()
        {
            _configurationUI.LoadConfig(configSet[currentConfigIndex], colorSet[currentConfigIndex],
                configEnabled[currentConfigIndex]);
        }
    }
}