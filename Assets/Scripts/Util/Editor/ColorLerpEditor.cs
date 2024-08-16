using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ColorLerp))]
public class ColorLerpEditor : Editor
{
    // private SerializedProperty lerpValue;

    private void OnEnable()
    {
        // lerpValue = serializedObject.FindProperty("lerpValue");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        return;
        serializedObject.Update();

        // EditorGUILayout.Slider(lerpValue, 0f, 1f, new GUIContent("Lerp Value"));

        // serializedObject.ApplyModifiedProperties();
    }
}