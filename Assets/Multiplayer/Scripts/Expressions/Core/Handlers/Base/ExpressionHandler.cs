using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExpressionHandler : MonoBehaviour
{
    
    protected bool interrupt;
    protected Expressions currentExpression;
    protected Expressions nextExpression;

    /// <summary>
    /// Set the expression for the handler.
    /// </summary>
    /// <param name="expression">Enum value for the expression to transition to.</param>
    public virtual void SetExpression(Expressions expression)
    {
        currentExpression = expression;
    }

    private float _transitionTime;

    /// <summary>
    /// The step function for the expression handler for animations.
    /// </summary>
    /// <param name="value">Normalized float value of our current position in the animation</param>
    public virtual void Step(float value)
    {
        
    }
}
