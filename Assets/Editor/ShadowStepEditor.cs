using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ShadowStep))]
public class ShadowStepEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ShadowStep shadowStep = (ShadowStep)target;
        if (GUILayout.Button("ShadowStep Move"))
        {
            shadowStep.ShadowStepMove();
        }
    }
}