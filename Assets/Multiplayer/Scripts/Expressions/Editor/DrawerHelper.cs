using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class DrawerHelper
{
    private const float _SCROLLBAR_WIDTH = 20f;
    
    public static Dictionary<Expressions, Action<Expressions>> CreateExpressionMap(Action<Expressions> expressionCallBack)
    {
        var expressionSelection = new Dictionary<Expressions, Action<Expressions>>();
        foreach (Expressions expression in Enum.GetValues(typeof(Expressions)))
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(expression.ToString());
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            expressionSelection.Add(expression, expressionCallBack);
        }

        return expressionSelection;
    }
    
    public static float CalculateLeftPanelWidth()
    {
        float maxWidth = 0f;

        foreach (Expressions expression in Enum.GetValues(typeof(Expressions)))
        {
            GUIContent content = new GUIContent(expression.ToString());
            float width = GUI.skin.button.CalcSize(content).x;
            if (width > maxWidth)
            {
                maxWidth = width;
            }
        }

        maxWidth = Math.Max(80, maxWidth);
        return maxWidth + _SCROLLBAR_WIDTH;
    }

    public static Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];

        for (int i = 0; i < pix.Length; i++)
        {
            pix[i] = col;
        }

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();

        return result;
    }

    static DrawerHelper()
    {
        InitTextures();
    }

    private static void InitTextures()
    {
        _backTexture =MakeTex(1, 1, Color.black);
        _activeTexture = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.1f, 1.0f));
        _normalTexture = MakeTex(2, 2, new Color(0.25f, 0.25f, 0.25f, 1.0f));
        
    }
    private static Texture2D _backTexture;
    private static Texture2D _activeTexture;
    private static Texture2D _normalTexture;

    public static GUIStyle DarkModeButton()
    {
        var buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.normal.textColor = Color.white; // Set the text color
        buttonStyle.fontSize = 14; // Set the text size
        buttonStyle.alignment = TextAnchor.MiddleCenter; // Center the text

        if (!_backTexture || !_activeTexture || !_normalTexture) InitTextures();
        // Set the background color
        buttonStyle.hover.textColor = Color.gray;
        buttonStyle.normal.background = _normalTexture;
        // buttonStyle.hover.background = _hoverTexture;
        buttonStyle.active.background = _activeTexture;
        buttonStyle.margin = new RectOffset(2, 0, 4, 0);
        return buttonStyle;
    }

    public static void DarkModeBackground(float width, float height)
    {
        GUI.DrawTexture(new Rect(0, 0, width, height), _backTexture);

    }
}