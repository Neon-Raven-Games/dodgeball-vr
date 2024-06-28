using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FacialFeatureAnimator : MonoBehaviour
{
    public List<ExpressionData> expressions;

    public EyebrowHandler eyebrowHandler;
    public EyeHandler eyeHandler;
    public MouthHandler mouthHandler;
    
    public void InitializeExpressions()
    {
        foreach (var expression in expressions)
        {
            eyebrowHandler.AddExpression(expression.eyeBrows);
            eyeHandler.AddExpression(expression.eyes);
            mouthHandler.AddExpression(expression.mouth);
        }
    }
    
    public void StartTransition(Expressions fromExpression)
    {
        eyebrowHandler.SetExpression(fromExpression);
        // todo, in game we need to initialize for values
        // eyebrowHandler.Initialize();
        eyeHandler.SetExpression(fromExpression);
        mouthHandler.SetExpression(fromExpression);
    }

    public void StepAnimation(float value)
    {
        eyebrowHandler.Step(value);
        eyeHandler.Step(value);
        mouthHandler.Step(value);
    }

    public void RotateEyebrows(float value) => eyebrowHandler.RotateEyebrows(value);
    
    public void RaiseEyebrows(float value) => eyebrowHandler.RaiseEyebrows(value);

    public void StartTransition(ExpressionData fromExpression, ExpressionData expressionData)
    {
        eyebrowHandler.Initialize(fromExpression.eyeBrows);
        eyebrowHandler.SetExpression(fromExpression.expression);
        eyebrowHandler.SetNextExpression(expressionData.eyeBrows);
        
        eyeHandler.Initialize(fromExpression.eyes);
        eyeHandler.SetExpression(fromExpression.expression);
        eyeHandler.SetNextExpression(expressionData.eyes);
        
        mouthHandler.Initialize(fromExpression.mouth);
        mouthHandler.SetExpression(fromExpression.expression);
        mouthHandler.SetNextExpression(expressionData.mouth);
    }
}