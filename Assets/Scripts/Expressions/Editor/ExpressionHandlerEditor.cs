using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class ExpressionHandlerEditor : EditorWindow
{
    private static readonly Vector2 _defaultWindowSize = new(575, 450);
    private GUIStyle _buttonStyle;
    private EyeBrowData _eyeBrowData;
    private EyeData _eyeData;
    private MouthData _mouthData;
    private Dictionary<Expressions, ExpressionData> _expressionData = new();
    private bool _male;
    private int _mouthIndex;
    private string[] _mouthNames;
    private static FacialFeatureAnimator _faceAnimator;
    private static Vector2 _scrollPositionLeft;
    private static GameObject _faceAnimatorPrefabInstance;
    private string _newExpressionName = "";
    private bool _showCreateNew;
    private bool _showEyebrows;
    private bool _showEyes;
    private bool _showMouth;
    private bool _showPreview;
    private Expressions _currentExpression;
    private static Vector2 _scrollPositionRight;
    private Texture2D _logo;
    private string[] genderOptions = new string[] {"Male", "Female"};
    private int selectedGenderIndex = 0;

    private Color hairColor = Color.black;
    private Color eyeColor = Color.blue;
    private Color skinColor = Color.white;

    private bool interrupt = false;
    private int frame = 0;

    private Expressions fromExpression = Expressions.Angry;
    private Expressions toExpression = Expressions.Sad;

    private Dictionary<MouthSprites, List<Sprite>> _mouthSpriteMap = new();
    private int _mouthFrame;

    [MenuItem("Neon Raven/Expressions Editor")]
    public static void ShowWindow()
    {
        GetWindow<ExpressionHandlerEditor>("Expressions Editor").Init();
    }

    private void OnEnable()
    {
        Init();
    }

    private void OnDisable()
    {
        // SaveExpressions();
    }

    private void Init()
    {
        _logo = Resources.Load<Texture2D>("NR_ExpressionEditorLogo");
        minSize = _defaultWindowSize;
        maxSize = _defaultWindowSize;
        _faceAnimator = FindFirstObjectByType<FacialFeatureAnimator>();
        if (_faceAnimator == null)
        {
            Debug.LogWarning("No FacialFeatureAnimator found on the prefab.");
        }
        else
        {
            _expressionData = PopulateExpressionData();
            _faceAnimator.eyeHandler.InitializeBlendShapes();
            _mouthNames = Enum.GetNames(typeof(MouthSprites));
            if (_faceAnimator.expressions.Count < Enum.GetValues(typeof(Expressions)).Length)
            {
                foreach (Expressions expression in Enum.GetValues(typeof(Expressions)))
                {
                    if (!_expressionData.ContainsKey(expression))
                    {
                        _expressionData.Add(expression, CreateInstance<ExpressionData>());
                        _expressionData[expression].expression = expression;
                        _expressionData[expression].eyes = _faceAnimator.eyeHandler.GetData(expression);
                        _expressionData[expression].eyeBrows = _faceAnimator.eyebrowHandler.GetData(expression);
                        _expressionData[expression].mouth = _faceAnimator.mouthHandler.GetData(expression);
                    }
                }

                SaveExpressions();
            }
            InitializeMouthSpriteMap();
        }
    }

    private void UpdateAvatar()
    {
        _faceAnimator.InitializeExpressions();
        _faceAnimator.StartTransition(_currentExpression);
        _faceAnimator.StepAnimation(1);
    }

    private Dictionary<Expressions, ExpressionData> PopulateExpressionData()
    {
        var expressions = _faceAnimator.expressions;
        if (expressions == null || expressions.Count == 0) return new Dictionary<Expressions, ExpressionData>();
        var expressionData = new Dictionary<Expressions, ExpressionData>();
        foreach (var expression in expressions)
        {
            if (expression)
            {
                PopulateEyeData(expression);
                PopulateBrowData(expression);
                PopulateMouthData(expression);
                expressionData.Add(expression.expression, expression);
            }
        }

        SaveExpressions();
        return expressionData;
    }

    private void PopulateMouthData(ExpressionData expression)
    {
        if (!expression.mouth)
        {
            _expressionData.TryGetValue(expression.expression, out var data);
            if (!data)
            {
                _mouthData = _faceAnimator.mouthHandler.GetData(_currentExpression);
                if (_mouthData.mouthSprites.Count == 0)
                {
                    InitializeMouthSpriteMap();
                    _mouthData.mouthSprites = _mouthSpriteMap[expression.mouth.spriteIndex];
                    expression.mouth = _mouthData;
                    _expressionData[expression.expression] = expression;
                }
            }
            else _mouthData = data.mouth;
        }
    }

    private void PopulateBrowData(ExpressionData expression)
    {
        if (!expression.eyeBrows)
        {
            _expressionData.TryGetValue(_currentExpression, out var data);
            if (!data)
            {
                data = CreateInstance<ExpressionData>();
                data.eyeBrows = _faceAnimator.eyebrowHandler.GetData(_currentExpression);
                _eyeBrowData = data.eyeBrows;
            }
            else _eyeBrowData = data.eyeBrows;
        }
    }

    private void PopulateEyeData(ExpressionData expression)
    {
        if (!expression.eyes)
        {
            _expressionData.TryGetValue(_currentExpression, out var data);
            if (!data)
            {
                data = CreateInstance<ExpressionData>();
                data.eyes = _faceAnimator.eyeHandler.GetData(_currentExpression);
                _eyeData = data.eyes;
            }
            else _eyeData = data.eyes;
        }
    }

    public void OnGUI()
    {
        var originalBackgroundColor = GUI.backgroundColor;
        DrawerHelper.DarkModeBackground(position.width, position.height);
        _buttonStyle ??= DrawerHelper.DarkModeButton();

        GUILayout.BeginHorizontal();

        DrawLeftPanel(originalBackgroundColor);
        DrawRightPanel(originalBackgroundColor);

        GUILayout.EndHorizontal();
    }


    private void DrawRightPanel(Color originalBackgroundColor)
    {
        _scrollPositionRight = GUILayout.BeginScrollView(_scrollPositionRight);
        GUILayout.BeginVertical();
        GUILayout.Space(20);

        _showCreateNew = EditorGUILayout.Foldout(_showCreateNew, "Create New Expression", true);
        if (_showCreateNew) DrawCreateNewExpression(originalBackgroundColor);
        GUILayout.Space(20);

        GUILayout.Label($"Editing {_currentExpression}", EditorStyles.boldLabel);
        GUILayout.Space(5);
        DrawEyebrows();
        GUILayout.Space(10);
        DrawEyes();
        GUILayout.Space(10);

        DrawMouth();
        GUILayout.Space(10);

        GUILayout.Space(20);
        GUILayout.Label("Save Current Expression", EditorStyles.boldLabel);
        GUILayout.Space(5);

        GUI.backgroundColor = new Color(0.35f, 0.85f, 0.35f, 1.0f);

        if (GUILayout.Button("Save Current Expression", _buttonStyle, GUILayout.Height(22)))
        {
            SaveExpressions(false);
        }
        // if (GUILayout.Button("Save All Expressions", _buttonStyle, GUILayout.Height(22)))
        // {
            // SaveExpressions();
        // }

        if (GUILayout.Button("Reload Mouth Sprites", _buttonStyle, GUILayout.Height(22)))
        {
            var mouthSprites = FileUtil.GetScriptParentPath("ExpressionHandlerEditor.cs") + "/MouthSprites/";
            var mouthSpriteStringList = GetMouthSpriteNames(mouthSprites);
            CreateEnumClass(mouthSpriteStringList, "MouthSprites");
            InitializeMouthSpriteMap();
        }

        GUI.backgroundColor = Color.red;
        
        if (GUILayout.Button("Delete Current Expression", _buttonStyle, GUILayout.Height(22)))
        {
            Debug.LogError("LMFAO, you can't delete expressions yet.");
        }
        GUILayout.EndVertical();
        GUILayout.EndScrollView();
        
        GUI.backgroundColor = new Color(0f, 0.55f, 0.8f, 1.0f);
        TransitionPreview();
        GUI.backgroundColor = originalBackgroundColor;
    }

    private bool _transitioning;
    private float _stepTime;
    private bool _frameInterrupted;

    private void Update()
    {
        if (!_transitioning) return;
        _stepTime += Time.deltaTime;

        if (_stepTime < 0) return;

        if (!interrupt) _faceAnimator.StepAnimation(_stepTime);
        else
        {
            var stepDuration = 1f / (_mouthData.mouthSprites.Count - 1);
            if (_stepTime < stepDuration * frame)
            {
                _faceAnimator.StepAnimation(_stepTime);
            }
            else
            {
                _faceAnimator.StartTransition(toExpression);

                if (frame == 1) _stepTime = 0;
                else _stepTime = stepDuration * (frame - 1);

                interrupt = false;
            }
        }

        if (_stepTime < 1f) return;
        _faceAnimator.StepAnimation(1);
        _transitioning = false;
        _stepTime = 0;
    }

    private void TransitionPreview()
    {
        // Avatar Section
        EditorGUILayout.BeginVertical(GUILayout.Width(200));
        EditorGUILayout.LabelField("Avatar", EditorStyles.boldLabel);

        selectedGenderIndex = EditorGUILayout.Popup("Gender", selectedGenderIndex, genderOptions);

        // todo, when blend shapes are fit for the male head uncomment this line
        // _faceAnimator.eyeHandler.isMale = selectedGenderIndex == 0;

        hairColor = EditorGUILayout.ColorField("Hair Color", hairColor);
        eyeColor = EditorGUILayout.ColorField("Eye Color", eyeColor);
        skinColor = EditorGUILayout.ColorField("Skin Color", skinColor);

        GUILayout.Space(20);
        EditorGUILayout.LabelField("Transitions", EditorStyles.boldLabel);

        GUI.backgroundColor = new Color(0.35f, 0.85f, 0.35f, 1.0f);
        interrupt = EditorGUILayout.Toggle("Interrupt", interrupt);
        GUI.backgroundColor = new Color(0f, 0.55f, 0.8f, 1.0f);
        
        frame = EditorGUILayout.IntField("Frame",
            Math.Clamp(frame, 0, _mouthData.mouthSprites.Count > 0 ? _mouthData.mouthSprites.Count - 1 : 1));

        fromExpression = (Expressions) EditorGUILayout.EnumPopup("From", fromExpression);
        toExpression = (Expressions) EditorGUILayout.EnumPopup("To", toExpression);

        EditorGUILayout.LabelField("Play:");
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("<") && !_transitioning)
        {
            (fromExpression, toExpression) = (toExpression, fromExpression);
            SaveTransitionExpressions(fromExpression, toExpression);
            _stepTime = -.5f;
            _faceAnimator.StartTransition(_expressionData[fromExpression], _expressionData[toExpression]);
            _transitioning = true;
        }

        if (GUILayout.Button(">") && !_transitioning)
        {
            SaveTransitionExpressions(fromExpression, toExpression);
            _stepTime = -.5f;
            _faceAnimator.StartTransition(_expressionData[fromExpression], _expressionData[toExpression]);
            _transitioning = true;
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(20);
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace(); // Creates flexible space on the left
        
        GUILayout.Box(_logo, GUILayout.Height(100));
        GUILayout.FlexibleSpace(); // Creates flexible space on the right
        GUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }

    private void SaveTransitionExpressions(Expressions from, Expressions to)
    {
        if (_currentExpression != from && _currentExpression != to) return;
        if (_currentExpression == from || _currentExpression == to) SaveExpressions(false);
    }

    private void DrawMouth()
    {
        if (_faceAnimator == null)
        {
            Debug.LogError("FaceAnimator was found null!");
            return;
        }

        if (_faceAnimator.eyeHandler == null)
        {
            Debug.LogError("EyeHandler was found null!");
            return;
        }

        if (_mouthNames.Length == 0)
        {
            Debug.LogWarning("Found no mouth Sprites to work with.");
        }
        else
        {
            _showMouth = EditorGUILayout.Foldout(_showMouth, "Mouth", true);
            if (_showMouth)
            {
                EditorGUI.indentLevel++;
                _mouthIndex = (int)_mouthData.spriteIndex;
                var selectedMouthSprite = (MouthSprites)Enum.Parse(typeof(MouthSprites), _mouthNames[_mouthIndex]);
                _faceAnimator.mouthHandler.SetMouthSprite(_mouthSpriteMap[selectedMouthSprite][_mouthFrame]);
            
                var index = _mouthIndex;
                _mouthIndex = EditorGUILayout.Popup("Mouth Sprite Sequence", _mouthIndex, _mouthNames);

                if (_mouthIndex != index)
                {
                    _mouthFrame = 0;
                    selectedMouthSprite = (MouthSprites)Enum.Parse(typeof(MouthSprites), _mouthNames[_mouthIndex]);
                    _expressionData[_currentExpression].mouth.mouthSprites = new List<Sprite>(_mouthSpriteMap[selectedMouthSprite]);
                    _mouthData = _expressionData[_currentExpression].mouth;
                    _mouthData.spriteIndex = selectedMouthSprite;
                    SaveExpressions(false);
                }

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Step Frames", GUILayout.Width(80));
                _mouthFrame = (int)EditorGUILayout.Slider(_mouthFrame, 0, _mouthSpriteMap[selectedMouthSprite].Count - 1);
                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
            }
        }
    }

    private void InitializeMouthSpriteMap()
    {
        if (_mouthSpriteMap == null) _mouthSpriteMap = new Dictionary<MouthSprites, List<Sprite>>();

        if (_mouthSpriteMap.Count == 0)
        {
            var mouthSpritePath = FileUtil.GetScriptParentPath("ExpressionHandlerEditor.cs") + "/MouthSprites/";
            var mouthNames = GetMouthSpriteNames(mouthSpritePath);
            foreach (var mouthName in mouthNames)
            {
                string assetPath = mouthSpritePath + mouthName + ".psd";
                var mouthSprites = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().ToList();

                if (mouthSprites.Count > 0)
                {
                        mouthSprites.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
                    try
                    {
                        MouthSprites enumValue = (MouthSprites) Enum.Parse(typeof(MouthSprites), mouthName);
                        _mouthSpriteMap.Add(enumValue, mouthSprites);
                    }
                    catch (ArgumentException)
                    {
                        Debug.LogWarning($"Mouth sprite '{mouthName}' does not match any enum value.");
                    }
                }
                else
                {
                    Debug.LogWarning($"No sprites found for '{mouthName}' in Resources.");
                }
            }
        }
    }

    private List<string> GetMouthSpriteNames(string mouthSpriteDirectory) =>
        Directory.GetFiles(mouthSpriteDirectory, "*.psd")
            .Select(Path.GetFileNameWithoutExtension).ToList();

    private void DrawEyes()
    {
        if (_faceAnimator == null) return;
        if (_faceAnimator.eyeHandler == null) return;

        _showEyes = EditorGUILayout.Foldout(_showEyes, "Eyes", true);
        if (!_transitioning)
        {
            foreach (var blendShape in _eyeData.blendShapeValue)
                _faceAnimator.eyeHandler.SetBlendShapeValue(blendShape.blendShapeName, blendShape.blendShapeValue);
        }

        if (!_showEyes) return;

        EditorGUI.indentLevel++;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.EndHorizontal();

        if (_eyeData == null || _eyeData.blendShapeValue == null)
        {
            Debug.LogWarning("No blend shapes found for the eyes.");
            return;
        }

        foreach (var blendShape in _eyeData.blendShapeValue)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(blendShape.blendShapeName, GUILayout.Width(80));

            blendShape.blendShapeValue = EditorGUILayout.Slider(blendShape.blendShapeValue, 0f, 100f);
            _faceAnimator.eyeHandler.SetBlendShapeValue(blendShape.blendShapeName, blendShape.blendShapeValue);

            EditorGUILayout.EndHorizontal();
        }

        if (_expressionData.ContainsKey(_currentExpression))
        {
            _expressionData[_currentExpression].eyes = _eyeData;
        }
        else
        {
            Debug.LogError("Did not find eyes in the expression data.");
        }
        EditorGUI.indentLevel--;
    }

    private void AddExpressionToFaceAnimator()
    {
        if (_faceAnimator == null)
        {
            Debug.LogError("Face animator was found null!");
            return;
        }

        _faceAnimator.expressions ??= new List<ExpressionData>();
        if (_faceAnimator.expressions.Contains(_expressionData[_currentExpression])) return;
        _faceAnimator.expressions.Add(_expressionData[_currentExpression]);
    }

    private void DrawEyebrows()
    {
        if (_faceAnimator == null) return;
        if (_faceAnimator.eyebrowHandler == null) return;

        _showEyebrows = EditorGUILayout.Foldout(_showEyebrows, "Eyebrows", true);

        if (!_transitioning && _stepTime == 0)
        {
            _faceAnimator.RotateEyebrows(_eyeBrowData.browRotation);
            _faceAnimator.RaiseEyebrows(_eyeBrowData.browHeight);
        }

        if (!_showEyebrows) return;

        EditorGUI.indentLevel++;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Brow Height", GUILayout.Width(80));

        _eyeBrowData.browHeight = EditorGUILayout.Slider(_eyeBrowData.browHeight, 1.35f, 1.41f);
        EditorGUILayout.EndHorizontal();

        // Draw slider for eyebrow rotation
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Brow Rotation", GUILayout.Width(80));
        _eyeBrowData.browRotation = EditorGUILayout.Slider(_eyeBrowData.browRotation, -30f, 70f);
        EditorGUILayout.EndHorizontal();

        EditorGUI.indentLevel--;

        if (_expressionData.ContainsKey(_currentExpression))
        {
            _expressionData[_currentExpression].eyeBrows.browRotation = _eyeBrowData.browRotation;
            _expressionData[_currentExpression].eyeBrows.browHeight = _eyeBrowData.browHeight;
        }
        else
        {
            Debug.LogError("Did not find eyebrows in the expression data.");
        }
    }

    private void DrawCreateNewExpression(Color originalBackgroundColor)
    {
        EditorGUI.indentLevel++;
        GUILayout.BeginHorizontal();
        GUILayout.Space(10);
        GUILayout.Label("Name:", EditorStyles.boldLabel, GUILayout.Width(80));
        _newExpressionName = GUILayout.TextField(_newExpressionName);
        GUILayout.EndHorizontal();

        GUI.backgroundColor = new Color(0.35f, 0.85f, 0.35f, 1.0f);

        if (GUILayout.Button("Create Expression", _buttonStyle, GUILayout.Height(22)))
        {
            // todo validate the name
            CreateNewExpression();
        }

        GUI.backgroundColor = originalBackgroundColor;
        EditorGUI.indentLevel--;
    }

    private void CreateNewExpression()
    {
        var expressionList = Enum.GetValues(typeof(Expressions)).Cast<string>().ToList();
        expressionList.Add(_newExpressionName);
        CreateEnumClass(expressionList, "Expressions");
        AssetDatabase.Refresh();
        _currentExpression = (Expressions) Enum.Parse(typeof(Expressions), _newExpressionName);
        _expressionData.Add(_currentExpression, CreateInstance<ExpressionData>());
        _newExpressionName = "";
        _eyeData = CreateInstance<EyeData>();
        _eyeBrowData = CreateInstance<EyeBrowData>();
        _mouthData = CreateInstance<MouthData>();

        _expressionData[_currentExpression].eyes = _eyeData;
        _expressionData[_currentExpression].eyeBrows = _eyeBrowData;
        _expressionData[_currentExpression].mouth = _mouthData;

        AddExpressionToFaceAnimator();
    }

    private void DrawLeftPanel(Color originalBackgroundColor)
    {
        _scrollPositionLeft =
            GUILayout.BeginScrollView(_scrollPositionLeft, GUILayout.Width(DrawerHelper.CalculateLeftPanelWidth()));
        GUILayout.BeginVertical();

        GUILayout.Space(20);
        GUI.backgroundColor = Color.gray;
        var originalContentColor = GUI.contentColor;
        foreach (Expressions expression in Enum.GetValues(typeof(Expressions)))
        {
            var selected = false;
            if (_currentExpression == expression)
            {
                GUI.contentColor = new Color(0.8f, 0.7f, 0.0f, 1.0f);
                selected = true;
            }

            if (GUILayout.Button(expression.ToString(), _buttonStyle, GUILayout.ExpandWidth(true),
                    GUILayout.Height(24)))
            {
                SaveExpressions(false);
                _currentExpression = expression;
                _mouthData = _expressionData[_currentExpression].mouth;
                _eyeData = _expressionData[_currentExpression].eyes;
                _eyeBrowData = _expressionData[_currentExpression].eyeBrows;
                UpdateAvatar();
            }

            if (selected) GUI.contentColor = originalContentColor;
        }

        if (_expressionData.ContainsKey(_currentExpression))
        {
            _eyeBrowData = _expressionData[_currentExpression].eyeBrows;
            _eyeData = _expressionData[_currentExpression].eyes;
            _mouthData = _expressionData[_currentExpression].mouth;
        }
        else
        {
            _expressionData.Add(_currentExpression, CreateInstance<ExpressionData>());
            _expressionData[_currentExpression].expression = _currentExpression;
        }

        GUI.backgroundColor = originalBackgroundColor;

        GUILayout.EndVertical();
        GUILayout.EndScrollView();
    }

    public void SaveExpressions(bool all = true)
    {
        var savePath = FileUtil.GetScriptParentPath("ExpressionHandlerEditor.cs") + "/Data/";
        if (!Directory.Exists(savePath))
            Directory.CreateDirectory(savePath);

        if (!all)
        {
            var data = _expressionData[_currentExpression];
            SaveOrUpdateAsset(_eyeData, savePath + "Eyes/" + $"{_currentExpression}_Eyes.asset");
            SaveOrUpdateAsset(_mouthData, savePath + "Mouth/" + $"{_currentExpression}_Mouth.asset");
            SaveOrUpdateAsset(_eyeBrowData, savePath + "Eyebrows/" + $"{_currentExpression}_Eyebrows.asset");
            SaveOrUpdateAsset(data, savePath + _currentExpression + ".asset");

            if (_faceAnimator && !_faceAnimator.expressions.Contains(data))
                _faceAnimator.expressions.Add(data);
        }
        else
        {
            foreach (var expression in _expressionData)
            {
                var data = expression.Value;
                if (data == null) continue;

                data.expression = expression.Key;
                SaveOrUpdateAsset(data.eyes, savePath + "Eyes/" + $"{expression.Key}_Eyes.asset");
                SaveOrUpdateAsset(data.mouth, savePath + "Mouth/" + $"{expression.Key}_Mouth.asset");
                SaveOrUpdateAsset(data.eyeBrows, savePath + "EyeBrows/" + $"{expression.Key}_Eyebrows.asset");
                SaveOrUpdateAsset(data, savePath + expression.Key + ".asset");

                if (_faceAnimator && !_faceAnimator.expressions.Contains(data))
                    _faceAnimator.expressions.Add(data);
            }
        }
    }

    private void SaveOrUpdateAsset(Object data, string assetPath)
    {
        var existingAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);

        string fileName = Path.GetFileNameWithoutExtension(assetPath);

        if (existingAsset == null)
        {
            data.name = fileName;
            AssetDatabase.CreateAsset(data, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        else
        {
            EditorUtility.CopySerialized(data, existingAsset);
            EditorUtility.SetDirty(existingAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    private void CreateEnumClass(List<string> expressionNames, string fileName)
    {
        List<string> cleanedExpressionList = new List<string>();
        foreach (string expression in expressionNames)
        {
            var cleaned = expression.Trim().Replace(' ', '_');

            cleanedExpressionList.Add(cleaned);
        }

        WriteEventNamesToFile(cleanedExpressionList, fileName);
    }

    private void WriteEventNamesToFile(List<string> expressionNames, string fileName)
    {
        var savePath = FileUtil.GetScriptParentPath("ExpressionHandlerEditor.cs");
        for (var i = 0; i < expressionNames.Count; i++)
        {
            expressionNames[i] = expressionNames[i].Replace(' ', '_');
            if (i < expressionNames.Count - 1) expressionNames[i] += ",";
        }

        var filePath = Path.Combine(savePath, fileName + ".cs");
        if (File.Exists(filePath)) File.Delete(filePath);

        expressionNames.Insert(0, $"public enum {fileName}\n{{");
        expressionNames.Add("}");

        File.WriteAllLines(filePath, expressionNames);
        AssetDatabase.Refresh();
    }
}