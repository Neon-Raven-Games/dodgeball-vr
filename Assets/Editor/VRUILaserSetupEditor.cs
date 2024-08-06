using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VRUILaserSetup))]
public class VRUILaserSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        VRUILaserSetup script = (VRUILaserSetup) target;

        if (GUILayout.Button("Simulate Pointer Down"))
        {
            script.OnUITrigger();
        }

        if (GUILayout.Button("Simulate Pointer Up"))
        {
            script.OnUITriggerRelease();
        }
    }
}