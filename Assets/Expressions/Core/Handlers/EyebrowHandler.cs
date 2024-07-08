using System;
using System.Collections.Generic;
using UnityEngine;

public class EyebrowHandler : ExpressionHandler
{
    Dictionary<Expressions, EyeBrowData> _expressions = new();
    public GameObject leftEyebrow;
    public GameObject rightEyebrow;

    public void RotateEyebrows(float value)
    {
        leftEyebrow.transform.localRotation = Quaternion.Euler(0, 0, value);
        rightEyebrow.transform.localRotation = Quaternion.Euler(0, 0, -value);
    }

    private float _initialYPosition;
    private float _endRotation;

    public void AddExpression(EyeBrowData data)
    {
        if (!_expressions.ContainsKey(data.expression))
            _expressions.Add(data.expression, data);
    }

    public void Initialize(EyeBrowData data)
    {
        _expressions[data.expression] = data;
        leftEyebrow.transform.localEulerAngles = new Vector3(0, 0, data.browRotation);
        rightEyebrow.transform.localEulerAngles = new Vector3(0, 0, -data.browRotation);

        leftEyebrow.transform.localPosition = new Vector3(leftEyebrow.transform.localPosition.x, data.browHeight,
            leftEyebrow.transform.localPosition.z);
        rightEyebrow.transform.localPosition = new Vector3(rightEyebrow.transform.localPosition.x, data.browHeight,
            rightEyebrow.transform.localPosition.z);
    }

    public void RaiseEyebrows(float value)
    {
        leftEyebrow.transform.localPosition = new Vector3(leftEyebrow.transform.localPosition.x, value,
            leftEyebrow.transform.localPosition.z);
        rightEyebrow.transform.localPosition = new Vector3(rightEyebrow.transform.localPosition.x, value,
            rightEyebrow.transform.localPosition.z);
    }

    public override void Step(float value)
    {
        if (!_expressions.TryGetValue(currentExpression, out var curExpression) || 
            !_expressions.TryGetValue(nextExpression, out var next))
        {
            return;
        }
        

        var height = Mathf.Lerp(curExpression.browHeight, next.browHeight, value);
        RaiseEyebrows(height);
        var rotation = Mathf.Lerp(curExpression.browRotation, next.browRotation, value);
        RotateEyebrows(rotation);
    }

    public EyeBrowData GetData(Expressions expression)
    {
        var eyeBrow = ScriptableObject.CreateInstance<EyeBrowData>();
        eyeBrow.expression = expression;
        eyeBrow.browRotation = leftEyebrow.transform.localEulerAngles.z;
        eyeBrow.browHeight = leftEyebrow.transform.localPosition.y;
        return eyeBrow;
    }

    public void SetNextExpression(EyeBrowData expressionDataEyeBrows)
    {
        _expressions[nextExpression] = expressionDataEyeBrows;
        nextExpression = expressionDataEyeBrows.expression;
    }
}