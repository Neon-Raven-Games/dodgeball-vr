using System;
using System.Linq;
using System.Reflection;
using Gameplay.InGameEvents;
using Gameplay.InGameEvents.Ninjas;
using Gameplay.Util;
using Gameplay.Util.Editor;
using TPUModelerEditor;
using UnityEditor;
using UnityEngine;

public class TimerManagerEditor : EditorWindow
{
    private Vector2 scrollPosition;
    private CustomTreeView treeView;
    private object splitterState;
    private bool autoRefresh = false;
    private float refreshInterval = 0.1f;
    private float lastRefreshTime;

    private PhaseManager _phaseManager;
    private EnemyEventData _selectedEnemyEventData;
    private PhaseEvent _selectedPhaseEvent;

    private BalanceCurve _tempBalanceCurve;
    private PhaseCurve _phaseCurve;

    private string _balanceCurveName = "NewBalanceCurve";
    private object _selectedTarget;
    private bool _lackeyPlayground;
    private bool _bossPlayground;
    private bool _phasePlayground;

    private int _startLackeyEventLevel;
    private int _endLackeyEventLevel;
    private float _startLackeyCooldown;
    private float _endLackeyCooldown;
    private float _startLackeyDuration;
    private float _endLackeyDuration;
    private float _endLackeyIntensity;
    private float _startLackeyIntensity;

    private int _phaseEventLevel;
    private int _phaseEndEventLevel;
    private int _phaseTeamOneMaxLives;
    private int _phaseTeamTwoMaxLives;
    private int _phaseOneMinLives;
    private int _phaseTeamTwoMinLives;

    [MenuItem("Window/Timers")]
    public static void OpenWindow()
    {
        GetWindow<TimerManagerEditor>("Timers").Show();
    }

    private void OnEnable()
    {
        splitterState = SplitterGUILayout.CreateSplitterState(new float[] {75f, 25f}, new int[] {32, 32}, null);
        treeView = new CustomTreeView();
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        lastRefreshTime = 0f;
    }

    private void OnGUI()
    {
        RenderHeadPanel();

        SplitterGUILayout.BeginVerticalSplit(this.splitterState);
        {
            RenderTable();
            RenderDetailsPanel();
        }
        SplitterGUILayout.EndVerticalSplit();

        if (CustomTreeView.SelectedTrackedItem != null)
        {
            HandleSelectionChange(CustomTreeView.SelectedTrackedItem);
        }
    }

    private void HandleSelectionChange(TrackedItem selectedItem)
    {
        Debug.Log($"Selected item: {selectedItem.Type}");
        Repaint();
    }

    private void RenderHeadPanel()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.ExpandWidth(true);

        _balanceCurveName = EditorGUILayout.TextField(_balanceCurveName, GUILayout.Width(200));
        if (GUILayout.Button("Save Phase Curve"))
            SavePhaseCurve();
        if (GUILayout.Button("Save Enemy Curve"))
            SaveEnemyCurve();

        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton) || autoRefresh)
        {
            CustomTreeView.GetTrackedEvents();
            treeView.Reload();
        }
        else
        {
            lastRefreshTime = Time.time;
        }

        autoRefresh = GUILayout.Toggle(autoRefresh, "Auto Refresh", GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();
    }

    private void SavePhaseCurve()
    {
        if (string.IsNullOrEmpty(_balanceCurveName))
        {
            Debug.LogWarning("Balance curve name cannot be empty.");
            return;
        }

        var newBalanceCurve = CreateInstance<PhaseCurve>();
        newBalanceCurve.teamOneLives = _phaseCurve.teamOneLives;
        newBalanceCurve.teamTwoLives = _phaseCurve.teamTwoLives;
        
        var assetPath = $"Assets/BalanceCurves/PhaseCurve/{_balanceCurveName}.asset";

        AssetDatabase.CreateAsset(newBalanceCurve, assetPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"Balance curve saved at {assetPath}");
    }

    private void SaveEnemyCurve()
    {
        if (string.IsNullOrEmpty(_balanceCurveName))
        {
            Debug.LogWarning("Balance curve name cannot be empty.");
            return;
        }

        var newBalanceCurve = CreateInstance<BalanceCurve>();
        newBalanceCurve.cooldownCurve = _tempBalanceCurve.cooldownCurve;
        newBalanceCurve.durationCurve = _tempBalanceCurve.durationCurve;
        newBalanceCurve.intensityCurve = _tempBalanceCurve.intensityCurve;

        string assetPath = $"Assets/BalanceCurves/EnemyData/{_balanceCurveName}.asset";
        AssetDatabase.CreateAsset(newBalanceCurve, assetPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"Balance curve saved at {assetPath}");
    }

    private void Update()
    {
        if (autoRefresh && Time.time - lastRefreshTime > refreshInterval)
        {
            CustomTreeView.GetTrackedEvents();
            treeView.Reload();
            lastRefreshTime = Time.time;
            if (_selectedEnemyEventData != null)
            {
                _selectedEnemyEventData.balanceCurve = _tempBalanceCurve;
                _selectedEnemyEventData.UpdateEventData();
            }
            Repaint();
        }
        else
        {
            lastRefreshTime = Time.time;
        }
    }

    private void RenderTable()
    {
        EditorGUILayout.BeginVertical("CN Box");
        var controlRect = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
        treeView.OnGUI(controlRect);
        EditorGUILayout.EndVertical();
    }

    private void RenderDetailsPanel()
    {
        RenderPhaseManagerData();
    }

    private void RenderPhaseManagerData()
    {
        if (_phaseManager == null) _phaseManager = FindObjectOfType<PhaseManager>();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical();

        EditorGUILayout.LabelField("Current Phase: " + _phaseManager.GetCurrentPhase());
        EditorGUILayout.LabelField("Team One Lives: " + _phaseManager.GetTeamOneLives());
        EditorGUILayout.LabelField("Team Two Lives: " + _phaseManager.GetTeamTwoLives());
        if (GUILayout.Button("Generate Enemy Data")) EnemyCurve();
        if (GUILayout.Button("Generate Phase Data")) PhaseCurve();

        if (_phaseManager != null) DrawGeneration();
        else EditorGUILayout.LabelField("Phase Manager not found in the scene.");

        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical();
        DrawPlaygrounds();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawPhaseManagerData()
    {
        if (_phaseManager == null) _phaseManager = FindObjectOfType<PhaseManager>();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical();

        EditorGUILayout.LabelField("Current Phase: " + _phaseManager.GetCurrentPhase());
        EditorGUILayout.LabelField("Remaining Time: " + _phaseManager.GetRemainingTime().ToString("0.00"));

        if (GUILayout.Button("Generate Enemy Data")) EnemyCurve();
        if (GUILayout.Button("Generate Phase Data")) PhaseCurve();

        if (_phaseManager != null) DrawGeneration();
        else EditorGUILayout.LabelField("Phase Manager not found in the scene.");

        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical();
        DrawPlaygrounds();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void EnemyCurve()
    {
        _selectedEnemyEventData ??= new EnemyEventData();
        _tempBalanceCurve = CreateInstance<BalanceCurve>();
        _tempBalanceCurve.durationCurve = new AnimationCurve();
        _tempBalanceCurve.cooldownCurve = new AnimationCurve();
        _tempBalanceCurve.intensityCurve = new AnimationCurve();

        for (var level = _startLackeyEventLevel; level <= _endLackeyEventLevel; level++)
        {
            var normalizedLevel = (float) (level - _startLackeyEventLevel) /
                                  (_endLackeyEventLevel - _startLackeyEventLevel);
            var durationValue = Mathf.Lerp(_startLackeyDuration, _endLackeyDuration, normalizedLevel);
            var cooldownValue = Mathf.Lerp(_startLackeyCooldown, _endLackeyCooldown, normalizedLevel);
            var intensityValue = Mathf.Lerp(_startLackeyIntensity, _endLackeyIntensity, normalizedLevel);

            _tempBalanceCurve.durationCurve.AddKey(normalizedLevel, durationValue);
            _tempBalanceCurve.cooldownCurve.AddKey(normalizedLevel, cooldownValue);
            _tempBalanceCurve.intensityCurve.AddKey(normalizedLevel, intensityValue);
        }

        _selectedEnemyEventData.balanceCurve = _tempBalanceCurve;
        _selectedEnemyEventData.UpdateEventData();

        Debug.Log("Enemy Event Data simulation complete. Check console for results.");
    }

    private void PhaseCurve()
    {
        if (!_phaseCurve || _selectedPhaseEvent == null)
        {
            _selectedPhaseEvent = new NinjaBossPhase();
            _phaseCurve = CreateInstance<PhaseCurve>();

            _phaseCurve.teamOneLives = new AnimationCurve();
            _phaseCurve.teamTwoLives = new AnimationCurve();

            _selectedPhaseEvent.phaseCurve = _phaseCurve;
        }

        for (var level = _phaseOneMinLives; level <= _phaseTeamOneMaxLives; level++)
        {
            var normalizedLevel =
                Mathf.Clamp01((float) (level - _phaseOneMinLives) / (_phaseTeamOneMaxLives - _phaseOneMinLives));
            var phaseTeamOneMaxLives = Mathf.Lerp(_phaseOneMinLives, _phaseTeamOneMaxLives, normalizedLevel);

            // Clamp the lives to avoid negative values
            phaseTeamOneMaxLives = Mathf.Max(phaseTeamOneMaxLives, 0);

            var key = new Keyframe(normalizedLevel, phaseTeamOneMaxLives);
            key.inTangent = 0;
            key.outTangent = 0;
            _phaseCurve.teamOneLives.AddKey(key);
        }

        for (var level = _phaseTeamTwoMinLives; level <= _phaseTeamTwoMaxLives; level++)
        {
            var normalizedLevel = Mathf.Clamp01((float) (level - _phaseTeamTwoMinLives) /
                                                (_phaseTeamTwoMaxLives - _phaseTeamTwoMinLives));
            var phaseTeamTwoMaxLives = Mathf.Lerp(_phaseTeamTwoMinLives, _phaseTeamTwoMaxLives, normalizedLevel);

            // Clamp the lives to avoid negative values
            phaseTeamTwoMaxLives = Mathf.Max(phaseTeamTwoMaxLives, 0);

            var key = new Keyframe(normalizedLevel, phaseTeamTwoMaxLives);
            key.inTangent = 0;
            key.outTangent = 0;
            _phaseCurve.teamTwoLives.AddKey(key);
        }

        // Smooth the tangents of all keys in the curve
        for (int i = 0; i < _phaseCurve.teamOneLives.length; i++)
        {
            _phaseCurve.teamOneLives.SmoothTangents(i, 0.5f); // Adjust the 0.5f to control smoothness
        }

        for (int i = 0; i < _phaseCurve.teamTwoLives.length; i++)
        {
            _phaseCurve.teamTwoLives.SmoothTangents(i, 0.5f); // Adjust the 0.5f to control smoothness
        }

        _selectedPhaseEvent.phaseCurve = _phaseCurve;
        Debug.Log("Phase Change Data simulation complete. Check console for results.");
    }


    private void DrawGeneration()
    {
        _phasePlayground = EditorGUILayout.Foldout(_phasePlayground, "Generate Phase Data", true);
        if (_phasePlayground) DrawPhaseGeneration();

        _lackeyPlayground = EditorGUILayout.Foldout(_lackeyPlayground, "Generate Enemy Data", true);
        if (_lackeyPlayground) DrawLackeyGeneration();
    }

    private void DrawPhaseGeneration()
    {
        EditorGUI.indentLevel++;

        GUILayout.Space(5);
        EditorGUILayout.LabelField("Event Occurrences", GUIStyles.titleStyle);

        GUILayout.Space(5);
        EditorGUILayout.LabelField("Team One Curves", GUIStyles.titleStyle);

        EditorGUI.indentLevel++;
        _phaseEventLevel = EditorGUILayout.IntSlider("Start Event Level", _phaseEventLevel, 1, 100);
        _phaseEndEventLevel = EditorGUILayout.IntSlider("End Event Level", _phaseEndEventLevel, 1, 100);
        EditorGUI.indentLevel--;

        GUILayout.Space(5);
        EditorGUILayout.LabelField("Team Two Curves", GUIStyles.titleStyle);

        EditorGUI.indentLevel++;
        _phaseOneMinLives = EditorGUILayout.IntSlider("Phase One Start Lives", _phaseOneMinLives, 1, 100);
        _phaseTeamOneMaxLives = EditorGUILayout.IntSlider("Phase One Max Lives", _phaseTeamOneMaxLives, 1, 100);

        _phaseTeamTwoMinLives = EditorGUILayout.IntSlider("Phase Two Start Lives", _phaseTeamTwoMinLives, 1, 100);
        _phaseTeamTwoMaxLives = EditorGUILayout.IntSlider("Phase Two Max Lives", _phaseTeamTwoMaxLives, 1, 100);
        EditorGUI.indentLevel--;

        EditorGUI.indentLevel--;
    }

    private void DrawLackeyGeneration()
    {
        EditorGUI.indentLevel++;

        GUILayout.Space(5);
        EditorGUILayout.LabelField("Event Occurrences", GUIStyles.titleStyle);

        EditorGUI.indentLevel++;
        _startLackeyEventLevel = EditorGUILayout.IntSlider("Start Event Level", _startLackeyEventLevel, 1, 100);
        _startLackeyEventLevel = Math.Min(_startLackeyEventLevel, _endLackeyEventLevel);
        _endLackeyEventLevel = EditorGUILayout.IntSlider("End Event Level", _endLackeyEventLevel, 1, 100);
        _endLackeyEventLevel = Math.Max(_startLackeyEventLevel, _endLackeyEventLevel);
        EditorGUI.indentLevel--;

        GUILayout.Space(5);
        EditorGUILayout.LabelField("Cooldown Curve", GUIStyles.titleStyle);

        EditorGUI.indentLevel++;
        _startLackeyCooldown = EditorGUILayout.Slider("Start Cooldown", _startLackeyCooldown, 0f, 200f);
        _endLackeyCooldown = EditorGUILayout.Slider("End Cooldown", _endLackeyCooldown, 0f, 200f);
        EditorGUI.indentLevel--;

        GUILayout.Space(5);
        EditorGUILayout.LabelField("Duration Curve", GUIStyles.titleStyle);

        EditorGUI.indentLevel++;
        _startLackeyDuration = EditorGUILayout.Slider("Start Duration", _startLackeyDuration, 0f, 100f);
        _endLackeyDuration = EditorGUILayout.Slider("End Duration", _endLackeyDuration, 0f, 100f);
        EditorGUI.indentLevel--;

        GUILayout.Space(5);
        EditorGUILayout.LabelField("Intensity Curve", GUIStyles.titleStyle);

        EditorGUI.indentLevel++;
        _startLackeyIntensity = EditorGUILayout.Slider("Start Intensity", _startLackeyIntensity, 0f, 1f);
        _endLackeyIntensity = EditorGUILayout.Slider("End Intensity", _endLackeyIntensity, 0f, 1f);
        EditorGUI.indentLevel--;

        EditorGUI.indentLevel--;
    }

    private void DrawPlaygrounds()
    {
        _phasePlayground = EditorGUILayout.Foldout(_phasePlayground, "Phase Playground", true);
        if (_phasePlayground) DrawPhasePlayground();

        _lackeyPlayground = EditorGUILayout.Foldout(_lackeyPlayground, "Enemy Playground", true);
        if (_lackeyPlayground) DrawLackeyPlayground();
    }

    private void DrawPhasePlayground()
    {
        if (!_phaseCurve || _selectedPhaseEvent == null || _selectedPhaseEvent.phaseCurve == null ||
            _phaseCurve.teamOneLives == null || _phaseCurve.teamTwoLives == null)
        {
            _selectedPhaseEvent = new NinjaBossPhase();
            _phaseCurve = CreateInstance<PhaseCurve>();

            _phaseCurve.teamOneLives = new AnimationCurve();
            _phaseCurve.teamTwoLives = new AnimationCurve();

            _selectedPhaseEvent.phaseCurve = _phaseCurve;
        }

        _phaseCurve.teamOneLives =
            EditorGUILayout.CurveField("Team One Lives Curve", _selectedPhaseEvent.phaseCurve.teamOneLives);
        _phaseCurve.teamTwoLives =
            EditorGUILayout.CurveField("Team Two Lives Curve", _selectedPhaseEvent.phaseCurve.teamTwoLives);

        EditorGUI.indentLevel++;

        _selectedPhaseEvent.eventLevel =
            EditorGUILayout.IntSlider("Start Event Level", _selectedPhaseEvent.eventLevel, 1, 100);

        var normalizedLevel = Mathf.Clamp01((_selectedPhaseEvent.eventLevel - 1f) / 99f);
        EditorGUILayout.LabelField("Team One Lives Duration: " + (int) _phaseCurve.teamOneLives.Evaluate(normalizedLevel));

        normalizedLevel = Mathf.Clamp01((_selectedPhaseEvent.eventLevel - 1f) / 99f);
        EditorGUILayout.LabelField("Calculated Lives" + (int) _phaseCurve.teamTwoLives.Evaluate(normalizedLevel));

        EditorGUI.indentLevel--;
    }
    private float EvaluateIntCurve(AnimationCurve curve, float normalizedLevel, float start, float end)
    {
        float curveValue = curve.Evaluate(normalizedLevel);
        float interpolatedValue = Mathf.Lerp(start, end, curveValue);
        return Mathf.Round(interpolatedValue);  // Round or ceil/floor depending on needs
    }

    private void DrawLackeyPlayground()
    {
        EditorGUI.indentLevel++;

        if (_selectedEnemyEventData == null || _selectedEnemyEventData.balanceCurve == null)
        {
            EditorGUI.indentLevel--;
            return;
        }

        _selectedEnemyEventData.eventLevel =
            EditorGUILayout.IntSlider("Start Event Level", _selectedEnemyEventData.eventLevel, 1, 100);

        _selectedEnemyEventData.balanceCurve.durationCurve =
            EditorGUILayout.CurveField("Event Duration Curve", _selectedEnemyEventData.balanceCurve.durationCurve);
        _selectedEnemyEventData.balanceCurve.cooldownCurve =
            EditorGUILayout.CurveField("Event Cooldown Curve", _selectedEnemyEventData.balanceCurve.cooldownCurve);
        _selectedEnemyEventData.balanceCurve.intensityCurve =
            EditorGUILayout.CurveField("Event Intensity Curve", _selectedEnemyEventData.balanceCurve.intensityCurve);

        var normalizedLevel = Mathf.Clamp01((_selectedEnemyEventData.eventLevel - 1f) / 99f);
        var duration = _selectedEnemyEventData.balanceCurve.durationCurve.Evaluate(normalizedLevel);
        var cooldown = _selectedEnemyEventData.balanceCurve.cooldownCurve.Evaluate(normalizedLevel);
        var intensity = _selectedEnemyEventData.balanceCurve.intensityCurve.Evaluate(normalizedLevel);
        EditorGUILayout.LabelField("Calculated Event Duration: " + duration.ToString("0.00"));
        EditorGUILayout.LabelField("Calculated Event Cooldown: " + cooldown.ToString("0.00"));
        EditorGUILayout.LabelField("Calculated Event Intensity: " + intensity.ToString("0.00"));

        _selectedEnemyEventData.UpdateEventData();
        EditorGUI.indentLevel--;
    }

    public class TrackedItem
    {
        public string Type { get; }
        public float ElapsedTime { get; }
        public string Status { get; }
        public string Details { get; }
        public object Target { get; } // Store the target object

        public TrackedItem(string type, float elapsedTime, string status, string details, object target)
        {
            Type = type;
            ElapsedTime = elapsedTime;
            Status = status;
            Details = details;
            Target = target;
        }
    }

    static class SplitterGUILayout
    {
        static BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                    BindingFlags.Static;

        static Lazy<Type> splitterStateType = new Lazy<Type>(() =>
        {
            var type = typeof(EditorWindow).Assembly.GetTypes().First(x => x.FullName == "UnityEditor.SplitterState");
            return type;
        });

        static Lazy<ConstructorInfo> splitterStateCtor = new Lazy<ConstructorInfo>(() =>
        {
            var type = splitterStateType.Value;
            return type.GetConstructor(flags, null, new Type[] {typeof(float[]), typeof(int[]), typeof(int[])}, null);
        });

        static Lazy<Type> splitterGUILayoutType = new Lazy<Type>(() =>
        {
            var type = typeof(EditorWindow).Assembly.GetTypes()
                .First(x => x.FullName == "UnityEditor.SplitterGUILayout");
            return type;
        });

        static Lazy<MethodInfo> beginVerticalSplit = new Lazy<MethodInfo>(() =>
        {
            var type = splitterGUILayoutType.Value;
            return type.GetMethod("BeginVerticalSplit", flags, null,
                new Type[] {splitterStateType.Value, typeof(GUILayoutOption[])}, null);
        });

        static Lazy<MethodInfo> endVerticalSplit = new Lazy<MethodInfo>(() =>
        {
            var type = splitterGUILayoutType.Value;
            return type.GetMethod("EndVerticalSplit", flags, null, Type.EmptyTypes, null);
        });

        public static object CreateSplitterState(float[] relativeSizes, int[] minSizes, int[] maxSizes)
        {
            return splitterStateCtor.Value.Invoke(new object[] {relativeSizes, minSizes, maxSizes});
        }

        public static void BeginVerticalSplit(object splitterState, params GUILayoutOption[] options)
        {
            beginVerticalSplit.Value.Invoke(null, new object[] {splitterState, options});
        }

        public static void EndVerticalSplit()
        {
            endVerticalSplit.Value.Invoke(null, Type.EmptyTypes);
        }
    }
}