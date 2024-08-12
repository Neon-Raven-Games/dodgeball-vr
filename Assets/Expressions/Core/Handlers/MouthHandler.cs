using System.Collections.Generic;
using UnityEngine;

public class MouthHandler : ExpressionHandler
{
    [SerializeField] private SpriteRenderer mouthRenderer;
    private Dictionary<Expressions, List<Sprite>> _mouthSpriteMap = new();
    private int currentFrame;
    private List<Sprite> currentMouthSprites;
    private Dictionary<Expressions, MouthData> _expressions = new();

    public void AddExpression(MouthData mouthData)
    {
        if (!_mouthSpriteMap.ContainsKey(mouthData.expression))
        {
            _mouthSpriteMap[mouthData.expression] = mouthData.mouthSprites;
            currentMouthSprites = mouthData.mouthSprites;
        }
    }

    public override void Step(float value)
    {
        if (value < 1)
        {
            if (value >= currentFrame / (float) currentMouthSprites.Count)
                mouthRenderer.sprite = currentMouthSprites[currentFrame++];
        }
    }

    public MouthData GetData(Expressions expressions)
    {
        var mouth = ScriptableObject.CreateInstance<MouthData>();
        mouth.expression = expressions;
        mouth.mouthSprites = new List<Sprite>();
        return mouth;
    }

    public void SetMouthSprite(Sprite mouthSprite)
    {
        mouthRenderer.sprite = mouthSprite;
    }

    public void Initialize(MouthData fromExpressionMouth)
    {
        _expressions[fromExpressionMouth.expression] = fromExpressionMouth;
        mouthRenderer.sprite = fromExpressionMouth.mouthSprites[^1];
    }

    public void SetNextExpression(MouthData expressionDataMouth)
    {
        nextExpression = expressionDataMouth.expression;
        _expressions[nextExpression] = expressionDataMouth;
        currentMouthSprites = expressionDataMouth.mouthSprites;
        currentFrame = 0;

    }
}