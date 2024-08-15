using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class UIState
{
    public bool IsFoldedOut { get; set; }
    public string Path { get; set; }
    public string DisplayName { get; set; }
    public List<UIState> Children { get; set; } = new();
    public float YPosition { get; set; }
    public string PropertyName { get; set; }
}

public static class AnimationPropertyControl
{
    public static void DrawControl(AnimationClip animationClip, Dictionary<string, UIState> foldoutStates,
        string searchText, float scrollViewWidth)
    {
        if (animationClip == null) return;

        foreach (var foldout in foldoutStates)
        {
            // Draw top-level foldouts (e.g., "Hips", "LeftArm")
            foldout.Value.IsFoldedOut = EditorGUILayout.Foldout(foldout.Value.IsFoldedOut, foldout.Value.DisplayName, true);

            if (foldout.Value.IsFoldedOut)
            {
                EditorGUI.indentLevel++;
                foreach (var child in foldout.Value.Children)
                {
                    child.IsFoldedOut = EditorGUILayout.Foldout(child.IsFoldedOut, child.DisplayName, true);
                    if (child.IsFoldedOut)
                    {
                        foreach (var childValue in child.Children)
                        {
                            DrawPropertyContainer(childValue, animationClip, scrollViewWidth);
                        }
                        
                    }
                }
                EditorGUI.indentLevel--;
            }
        }
    }

    private static void DrawPropertyContainer(UIState propertyState, AnimationClip animationClip, float scrollViewWidth)
    {
        if (propertyState.Children.Count > 0)
        {
            // This property has children, so it should be a foldout (e.g., "m_LocalRotation")
            propertyState.IsFoldedOut = EditorGUILayout.Foldout(propertyState.IsFoldedOut, propertyState.DisplayName, true);

            if (propertyState.IsFoldedOut)
            {
                foreach (var childState in propertyState.Children)
                {
                    DrawPropertyContainer(childState, animationClip, scrollViewWidth);
                }
            }
        }
        else
        {
            // If no children, treat this as a regular property
            DrawPropertyField(propertyState, animationClip, scrollViewWidth);
        }
    }

    private static void DrawPropertyField(UIState propertyState, AnimationClip animationClip, float scrollViewWidth)
    {
        var binding = GetBindingForState(animationClip, propertyState);

        if (!binding.HasValue) return;

        var groupStyle = NeonRavenStyles.CompactGroupStyle(scrollViewWidth);
        var iconWidth = 16f;
        var labelWidth = 15f;
        var fieldWidth = scrollViewWidth - iconWidth - labelWidth - 100; 

        var iconStyle = NeonRavenStyles.IconStyle(iconWidth);
        var labelStyle = NeonRavenStyles.CompactLabelStyle(labelWidth);
        var fieldStyle = NeonRavenStyles.CompactFieldStyle(fieldWidth);

        EditorGUILayout.BeginHorizontal(groupStyle);
        var iconContent = GetIcon(binding.Value);

        GUILayout.Label(iconContent, iconStyle, GUILayout.Height(iconStyle.fixedHeight));
        GUILayout.Label(propertyState.DisplayName, labelStyle, GUILayout.MaxWidth(labelWidth));

        float currentTime = 0f; // This should be updated based on your timeline implementation
        float valueAtTime = GetCurveValueAtTime(animationClip, binding.Value, currentTime);
    
        float newValue = EditorGUILayout.FloatField(valueAtTime, fieldStyle, GUILayout.MaxWidth(fieldWidth));
    
        if (Mathf.Abs(newValue - valueAtTime) > Mathf.Epsilon)
        {
            UpdateCurve(animationClip, binding.Value, currentTime, newValue);
        }
        
        // if (Mathf.Abs(newValue - GetCurveValueAtTime(animationClip, binding.Value, currentTime)) > Mathf.Epsilon)
        // {
        //     UpdateCurve(animationClip, binding.Value, currentTime, newValue);
        // }

        EditorGUILayout.EndHorizontal();
    }

    private static float CalculateMaxLabelWidth(IEnumerable<UIState> childStates)
    {
        float maxWidth = 0f;
        foreach (var state in childStates)
        {
            GUIContent content = new GUIContent(state.DisplayName + " ");
            float width = GUI.skin.label.CalcSize(content).x;
            if (width > maxWidth)
            {
                maxWidth = width;
            }
        }

        return maxWidth;
    }

    public static EditorCurveBinding? GetBindingForState(AnimationClip clip, UIState state)
    {
        var bindings = AnimationUtility.GetCurveBindings(clip);

        var matchedBinding = bindings.FirstOrDefault(x =>
        {
            string expectedPath = $"{x.path}/{x.propertyName}";
            string statePath = $"{state.Path}/{state.PropertyName}";
            bool isMatch = string.Equals(expectedPath, statePath, StringComparison.Ordinal);

            if (isMatch)
            {
                string component = state.DisplayName; // This should be x, y, z, w
                isMatch = x.propertyName.EndsWith(component, StringComparison.Ordinal);
            }

            return isMatch;
        });


            return matchedBinding;
    }


    public static void AddPropertyButton(float buttonWidth)
    {
        if (GUILayout.Button("Add Property", NeonRavenStyles.FlexButtonStyle(buttonWidth)))
        {
            Debug.Log("Add Property Button Clicked");
        }
    }

    private static GUIContent GetIcon(EditorCurveBinding binding)
    {
        GUIContent iconContent = EditorGUIUtility.IconContent("DefaultAsset Icon"); // Default icon

        if (binding.type == typeof(Transform))
        {
            iconContent = EditorGUIUtility.IconContent("Transform Icon");
        }
        else if (binding.type == typeof(Material))
        {
            iconContent = EditorGUIUtility.IconContent("Material Icon");
        }
        else if (binding.type == typeof(Renderer))
        {
            iconContent = EditorGUIUtility.IconContent("MeshRenderer Icon");
        }

        return iconContent;
    }

    private static float GetCurveValueAtTime(AnimationClip clip, EditorCurveBinding binding, float time)
    {
        var curve = AnimationUtility.GetEditorCurve(clip, binding);
        if (curve != null)
        {
            return curve.Evaluate(time); // Get the value at the specific time
        }

        return 0f;
    }


    private static void UpdateCurve(AnimationClip clip, EditorCurveBinding binding, float time, float value)
    {
        var curve = AnimationUtility.GetEditorCurve(clip, binding);
        if (curve != null)
        {
            Keyframe key = new Keyframe(time, value);
            int index = curve.AddKey(key);
            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }
    }


    private static GUIStyle CreateDebugStyle(Color color)
    {
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.normal.background = MakeTex(2, 2, color);
        return style;
    }

    private static Texture2D MakeTex(int width, int height, Color col)
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
}