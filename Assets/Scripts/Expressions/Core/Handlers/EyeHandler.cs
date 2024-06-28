using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class BlendShapeData
{
    public string blendShapeName;
    public int blendShapeIndex;
    public float blendShapeValue;
}

public class EyeHandler : ExpressionHandler
{
    public SkinnedMeshRenderer maleSkinnedMeshRenderer;
    public SkinnedMeshRenderer femaleSkinnedMeshRenderer;
    public bool isMale;
    private SkinnedMeshRenderer _skinnedMeshRenderer;
    private readonly Dictionary<string, BlendShapeData> _blendShapeIndexMap = new();
    private Dictionary<Expressions, EyeData> _expressions = new();

    private Expressions _currentExpression;

    // todo, this broke too
    private void Start()
    {
        InitializeBlendShapes();
    }

    public override void Step(float value)
    {
        if (!_expressions.TryGetValue(currentExpression, out var curExpression) ||
            !_expressions.TryGetValue(nextExpression, out var next))
            return;

        foreach (var blendShapeData in _blendShapeIndexMap)
        {
            var curBlendShape = curExpression.blendShapeValue.FirstOrDefault(x => x.blendShapeName == blendShapeData.Value.blendShapeName);
            var nextBlendShape = next.blendShapeValue.FirstOrDefault(x => x.blendShapeName == blendShapeData.Value.blendShapeName);

            if (curBlendShape != null && nextBlendShape != null)
            {
                var lerpValue = Mathf.Lerp(curBlendShape.blendShapeValue, nextBlendShape.blendShapeValue, value);
                SetBlendShapeValue(blendShapeData.Value.blendShapeName, lerpValue);
            }
        }
    }

    public void InitializeBlendShapes()
    {
        if (_blendShapeIndexMap.Count > 0) return;
        _skinnedMeshRenderer = isMale ? maleSkinnedMeshRenderer : femaleSkinnedMeshRenderer;
        if (_skinnedMeshRenderer != null && _skinnedMeshRenderer.sharedMesh != null)
        {
            var mesh = _skinnedMeshRenderer.sharedMesh;
            var blendShapeCount = mesh.blendShapeCount;

            for (var i = 0; i < blendShapeCount; i++)
            {
                var blendShapeName = mesh.GetBlendShapeName(i);
                _blendShapeIndexMap[blendShapeName] = new BlendShapeData()
                {
                    blendShapeName = blendShapeName,
                    blendShapeIndex = i,
                    blendShapeValue = 0
                };
            }
        }
    }

    public void SetBlendShapeValue(string blendShapeName, float value)
    {
        if (_blendShapeIndexMap.Count == 0) InitializeBlendShapes();
        _skinnedMeshRenderer = isMale ? maleSkinnedMeshRenderer : femaleSkinnedMeshRenderer;

        if (_blendShapeIndexMap.TryGetValue(blendShapeName, out var data))
        {
            _skinnedMeshRenderer.SetBlendShapeWeight(data.blendShapeIndex, value);
            data.blendShapeValue = value;
        }
    }

    public EyeData GetData(Expressions expression)
    {
        if (_blendShapeIndexMap.Count == 0) InitializeBlendShapes();
        var eyeData = ScriptableObject.CreateInstance<EyeData>();
        eyeData.expression = expression;
        eyeData.blendShapeValue = _blendShapeIndexMap.Values.ToList();
        return eyeData;
    }

    public void AddExpression(EyeData expressionEyes)
    {
        if (_blendShapeIndexMap.Count == 0) InitializeBlendShapes();
        if (!_expressions.ContainsKey(expressionEyes.expression))
            _expressions.Add(expressionEyes.expression, expressionEyes);
    }


    public void Initialize(EyeData fromExpressionEyes)
    {
        _expressions[fromExpressionEyes.expression] = fromExpressionEyes;
        foreach (var blendShapeData in fromExpressionEyes.blendShapeValue)
        {
            SetBlendShapeValue(blendShapeData.blendShapeName, blendShapeData.blendShapeValue);
        }
    }

    public void SetNextExpression(EyeData expressionDataEyes)
    {
        nextExpression = expressionDataEyes.expression;
        _expressions[nextExpression] = expressionDataEyes;
    }
}