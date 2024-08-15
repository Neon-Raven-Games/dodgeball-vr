using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class NeonRavenStyles
{

    public static GUIStyle GetNeonRavenStyle()
    {
        GUIStyle neonRavenStyle = new GUIStyle();
        neonRavenStyle.normal.textColor = Color.white;
        neonRavenStyle.fontSize = 20;
        neonRavenStyle.fontStyle = FontStyle.Bold;
        neonRavenStyle.alignment = TextAnchor.MiddleCenter;
        return neonRavenStyle;
    }

    public static GUIStyle FlexButtonStyle(float width)
    {
        GUIStyle style = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12,
            margin = new RectOffset(5, 4, 0, 0),
            padding = new RectOffset(12, 10, 5, 5),
            fixedWidth = width - width * 0.05f
        };
        return style;
    }

    public static GUIStyle IconStyle(float width)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            margin = new RectOffset(2, 0, 0, 0),
            padding = new RectOffset(0, 0, 0, 0),
            fixedWidth = width,
            fixedHeight = 16f
        };
        return style;
    }
    public static GUIStyle CompactLabelStyle(float width)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            margin = new RectOffset(1, 0, 0, 0),
            padding = new RectOffset(0, 0, 0, 0),
            fontSize = 10,
            fixedWidth = width,
            alignment = TextAnchor.MiddleLeft
        };
        return style;
    }
    
    public static GUIStyle CompactFieldStyle(float width)
    {
        GUIStyle style = new GUIStyle(GUI.skin.textField)
        {
            margin = new RectOffset(0, 10, 0, 0),
            padding = new RectOffset(2, 0, 0, 0),
            fontSize = 10,
            fixedHeight = 16f,
            fixedWidth = width,
            alignment = TextAnchor.MiddleLeft
        };
        return style;
    }
    
    public static GUIStyle CompactGroupStyle(float width)
    {
        GUIStyle style = new GUIStyle(GUI.skin.box)
        {
            margin = new RectOffset(35, 0, 0, 0),
            padding = new RectOffset(0, 0, 0, 0),
            alignment = TextAnchor.MiddleLeft,
            fixedWidth = width
        };
        return style;
    }
}
