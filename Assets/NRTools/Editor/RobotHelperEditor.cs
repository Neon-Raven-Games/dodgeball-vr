using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RobotHelper))]
public class RobotHelperEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RobotHelper script = (RobotHelper)target;

        // if (GUILayout.Button("Set Text to Ellipsis")); script.StartTyping("....");
        // if (GUILayout.Button("Set Text to -.-")); script.StartTyping("-.-");
    }
}