using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Template.VR.Multiplayer;

namespace CloudFine.ThrowLab.UI
{
    public class UIThrowConfiguration : MonoBehaviour
    {
        public GameObject VSmoothRoot;
        public GameObject PhysicsRoot;
        public GameObject OtherRoot;
        
        private ThrowConfiguration currentConfig;
        private Color currentColor;
        public TextMeshProUGUI configLabel;

        [Header("Smoothing")] public Toggle smoothingToggle;
        public TMP_Dropdown smoothingAlgorithmDropdown;
        public TMP_Dropdown smoothingPeriodDropdown;
        public TMP_Dropdown smoothingTimeDropdown;
        public UIStepper smoothignSecondsStepper;
        public TMP_Dropdown smoothingPointDropdown;
        public UISmoothingVisual smoothingUI;

        [Header("Friction")] public Toggle frictionToggle;

        public Slider frictionFalloffSlider;

        // public Toggle frictionCustomCurveToggle;
        public UIStepper frictionSecondsStepper;
        public UICurveLine frictionCurveUI;

        [Header("Assist")] public Toggle assistToggle;
        public Toggle assistCustomCurveToggle;
        public Slider assistGravitySlider;
        public Slider assistWeightSlider;
        public Slider assistRangeSlider;
        public UICurveLine assistCurveUI;
        public TMP_Dropdown targetSelectionDropdown;

        [Header("Scale")] public Toggle scaleToggle;


        // public Toggle scaleCustomCurveToggle;
        public Slider scaleRampSlider;
        public UIStepper scaleStepper;
        public UIStepper scaleThresholdStepper;
        public UICurveLine scaleCurveUI;

        private void Awake()
        {
            smoothingAlgorithmDropdown.ClearOptions();
            smoothingAlgorithmDropdown.AddOptions(System.Enum.GetNames(typeof(ThrowConfiguration.EstimationAlgorithm))
                .ToList());

            smoothingPeriodDropdown.ClearOptions();
            smoothingPeriodDropdown.AddOptions(System.Enum.GetNames(typeof(ThrowConfiguration.PeriodMeasurement))
                .ToList());

            smoothingTimeDropdown.ClearOptions();
            smoothingTimeDropdown.AddOptions(System.Enum.GetNames(typeof(ThrowConfiguration.SampleTime)).ToList());

            smoothingPointDropdown.ClearOptions();
            smoothingPointDropdown.AddOptions(System.Enum.GetNames(typeof(ThrowConfiguration.VelocitySource)).ToList());

            targetSelectionDropdown.ClearOptions();
            targetSelectionDropdown.AddOptions(System.Enum.GetNames(typeof(ThrowConfiguration.AssistTargetMethod))
                .ToList());
        }

        public void InitializeVelocitySmoothing(ThrowConfiguration config)
        {
            if (!VSmoothRoot.activeInHierarchy) return;
            if (smoothingAlgorithmDropdown)
            {
                smoothingAlgorithmDropdown.value = (int) config.estimationFunction;
                smoothingAlgorithmDropdown.onValueChanged.Invoke((int) config.estimationFunction);
            }

            if (smoothingPeriodDropdown)
            {
                smoothingPeriodDropdown.value = (int) config.samplePeriodMeasurement;
                SetPeriodMeasurement(smoothingPeriodDropdown.value);
                smoothingPeriodDropdown.onValueChanged.Invoke((int) config.samplePeriodMeasurement);
            }

            if (smoothingPointDropdown) smoothingPointDropdown.value = (int) config.sampleSourceType;

            smoothingUI.SetFunc(config.GetWeights);
            smoothingUI.Refresh();
            if (smoothingToggle)
            {
                smoothingToggle.isOn = config.smoothingEnabled;
                smoothingToggle.interactable = enabled;
            }
        }

        public void InitializePhysics(ThrowConfiguration config)
        {
            if (!PhysicsRoot.activeInHierarchy) return;
            if (scaleStepper)
            {
                scaleStepper.value = config.scaleMultiplier;
            }

            if (scaleThresholdStepper)
            {
                scaleThresholdStepper.value = config.scaleThreshold;
            }

            if (scaleRampSlider)
            {
                scaleRampSlider.value = config.scaleRampExponent;
                scaleRampSlider.onValueChanged.Invoke(config.scaleRampExponent);
            }

            if (frictionFalloffSlider)
            {
                frictionFalloffSlider.value = config.frictionFalloffExponent;
                frictionFalloffSlider.onValueChanged.Invoke(config.frictionFalloffExponent);
            }

            if (frictionSecondsStepper)
            {
                frictionSecondsStepper.value = config.frictionFalloffSeconds;
            }

            if (frictionCurveUI)
            {
                frictionCurveUI.SetCurveFunc(config.SampleFrictionCurve);
            }

            if (frictionToggle)
            {
                frictionToggle.isOn = config.frictionEnabled;
                frictionToggle.interactable = enabled;
            }

            if (scaleToggle)
            {
                scaleToggle.isOn = config.scaleEnabled;
                scaleToggle.interactable = enabled;
            }
            
            if (scaleCurveUI)
            {
                scaleCurveUI.SetCurveFunc(config.SampleScalingCurve);
            }
        }

        public void InitializeOther(ThrowConfiguration config)
        {
            if (!OtherRoot.activeInHierarchy) return;
            if (assistGravitySlider)
            {
                assistGravitySlider.value = config.assistRampExponent;
                assistGravitySlider.onValueChanged.Invoke(config.assistRampExponent);
            }

            if (assistWeightSlider)
            {
                assistWeightSlider.value = config.assistWeight;
                assistWeightSlider.onValueChanged.Invoke(config.assistWeight);
            }

            if (assistRangeSlider)
            {
                assistRangeSlider.value = config.assistRangeDegrees;
                assistRangeSlider.onValueChanged.Invoke(config.assistRangeDegrees);
            }
            if (assistCurveUI)
            {
                assistCurveUI.SetCurveFunc(config.SampleAssistCurve);
            }

            if (assistCustomCurveToggle)
            {
                assistCustomCurveToggle.isOn = config.useAssistRampCustomCurve;
            }

            if (targetSelectionDropdown)
            {
                targetSelectionDropdown.value = (int) config.assistTargetMethod;
            }
            if (assistToggle)
            {
                assistToggle.isOn = config.assistEnabled;
                assistToggle.interactable = enabled;
            }
        }

        public void LoadConfig(ThrowConfiguration config, Color color, bool enabled)
        {
            //set values on ui
            currentConfig = config;
            configLabel.text = config.name;
            currentColor = color;
            // if (scaleCustomCurveToggle)
            // {
            //     scaleCustomCurveToggle.isOn = config.useScaleRampCustomCurve;
            // }

            //FRICTION

            // if (frictionCustomCurveToggle)
            // {
            //     frictionCustomCurveToggle.isOn = config.useFrictionFalloffCustomCurve;
            // }

            InitializeVelocitySmoothing(config);
            InitializePhysics(config);
            InitializeOther(config);

            SetAssistEnabled(config.assistEnabled, enabled);
            SetFrictionEnabled(config.frictionEnabled, enabled);
            SetSmoothingEnabled(config.smoothingEnabled, enabled);
            SetScalingEnabled(config.scaleEnabled, enabled);
        }


        public void SetAssistEnabled(bool enabled)
        {
            SetAssistEnabled(enabled, true);
        }

        public void SetAssistEnabled(bool enabled, bool configEnabled)
        {
            if (currentConfig) currentConfig.assistEnabled = enabled;
            // SetPanelEnabled(assistOptionsRoot, assistToggle.gameObject, enabled && configEnabled);
            SetAssistCustomCurve(currentConfig.useAssistRampCustomCurve);
        }

        public void SetFrictionEnabled(bool enabled)
        {
            SetFrictionEnabled(enabled, true);
        }

        public void SetFrictionEnabled(bool enabled, bool configEnabled)
        {
            if (currentConfig) currentConfig.frictionEnabled = enabled;
            // SetPanelEnabled(frictionOptionsRoot, frictionToggle.gameObject, enabled && configEnabled);
            SetFrictionCustomCurve(currentConfig.useFrictionFalloffCustomCurve);
        }

        public void SetSmoothingEnabled(bool enabled)
        {
            SetSmoothingEnabled(enabled, true);
        }

        public void SetSmoothingEnabled(bool enabled, bool configEnabled)
        {
            if (currentConfig) currentConfig.smoothingEnabled = enabled;
            // SetPanelEnabled(smoothingOptionsRoot, smoothingToggle.gameObject, enabled && configEnabled);
        }

        public void SetScalingEnabled(bool enabled)
        {
            SetScalingEnabled(enabled, true);
        }

        public void SetScalingEnabled(bool enabled, bool configEnabled)
        {
            if (currentConfig) currentConfig.scaleEnabled = enabled;
            // SetPanelEnabled(scaleOptionsRoot, scaleToggle.gameObject, enabled && configEnabled);
            SetScalingCustomCurve(currentConfig.useScaleRampCustomCurve);
        }

        private void SetChildrenColor(GameObject root, Color c)
        {
            foreach (Graphic select in root.GetComponentsInChildren<Graphic>(includeInactive: true))
            {
                if (select.GetComponent<UIColorMeTag>() != null)
                {
                    select.color = c;
                }
            }
        }


        public void SetEstimationAlgorithm(int value)
        {
            if (currentConfig) currentConfig.estimationFunction = (ThrowConfiguration.EstimationAlgorithm) value;
        }

        public void SetPeriodMeasurement(int value)
        {
            ThrowConfiguration.PeriodMeasurement period = (ThrowConfiguration.PeriodMeasurement) value;
            if (currentConfig) currentConfig.samplePeriodMeasurement = period;

            var stepper = smoothignSecondsStepper.GetComponent<UIStepper>();
            if (period == ThrowConfiguration.PeriodMeasurement.TIME)
            {
                stepper.step = 0.1f;
                seconds = true;
                smoothingTimeDropdown.gameObject.SetActive(true);
                stepper.value = currentConfig.periodSeconds;
                var valueText = stepper.GetComponentInChildren<UIValueText>();
                valueText._toStringPattern = "0.0";
                valueText._postDecorator = "s";
            }
            else
            {
                seconds = false;
                stepper.step = 1;
                smoothingTimeDropdown.gameObject.SetActive(false);
                stepper.value = currentConfig.periodFrames;
                var valueText = stepper.GetComponentInChildren<UIValueText>();
                valueText._toStringPattern = "0";
                valueText._postDecorator = "";
            }
        }


        public void SetSampleSource(int value)
        {
            if (currentConfig) currentConfig.sampleSourceType = (ThrowConfiguration.VelocitySource) value;
        }

        private bool seconds;
        public void SetSmoothingTime(float value)
        {
            if (!currentConfig) return;
            if (seconds) currentConfig.periodSeconds = value;
            else currentConfig.periodFrames = (int) value;
        }
        public void SetSmoothingSampleTime(int value)
        {
            if (currentConfig) currentConfig.sampleTime = (ThrowConfiguration.SampleTime) value;

        }

        public void SetSmoothingSeconds(float seconds)
        {
            if (currentConfig) currentConfig.periodSeconds = seconds;
        }

        public void SetSmoothingFrames(float frames)
        {
            if (currentConfig) currentConfig.periodFrames = (int) frames;
        }


        //ASSIST
        public void SetAssistRange(float range)
        {
            if (currentConfig) currentConfig.assistRangeDegrees = range;
        }

        public void SetAssistGravity(float gravity)
        {
            if (currentConfig)
            {
                currentConfig.assistRampExponent = gravity;
            }
        }

        public void SetAssistWeight(float weight)
        {
            if (currentConfig) currentConfig.assistWeight = weight;
        }

        public void SetAssistCustomCurve(bool value)
        {
            if (currentConfig) currentConfig.useAssistRampCustomCurve = value;
            if (assistGravitySlider) assistGravitySlider.interactable = !value;
            if (assistCurveUI) assistCurveUI.RefreshCurve();
        }

        public void SetTargetSelectionMethod(int value)
        {
            if (currentConfig) currentConfig.assistTargetMethod = (ThrowConfiguration.AssistTargetMethod) value;
        }


        //SCALING
        public void SetScalingMultiplier(float scale)
        {
            if (currentConfig) currentConfig.scaleMultiplier = scale;
        }

        public void SetScalingThreshold(float threshold)
        {
            if (currentConfig) currentConfig.scaleThreshold = threshold;
        }

        public void SetScalingRamp(float value)
        {
            if (currentConfig) currentConfig.scaleRampExponent = value;
        }

        public void SetScalingCustomCurve(bool value)
        {
            if (currentConfig) currentConfig.useScaleRampCustomCurve = value;
            if (scaleRampSlider) scaleRampSlider.interactable = !value;
            if (scaleCurveUI) scaleCurveUI.RefreshCurve();
        }


        //FRICTION
        public void SetFrictionDuration(float value)
        {
            if (currentConfig) currentConfig.frictionFalloffSeconds = value;
        }

        public void SetFrictionFalloff(float value)
        {
            if (currentConfig) currentConfig.frictionFalloffExponent = value;
        }

        public void SetFrictionCustomCurve(bool value)
        {
            if (currentConfig) currentConfig.useFrictionFalloffCustomCurve = value;
            if (frictionFalloffSlider) frictionFalloffSlider.interactable = !value;
            if (frictionCurveUI) frictionCurveUI.RefreshCurve();
        }
    }
}