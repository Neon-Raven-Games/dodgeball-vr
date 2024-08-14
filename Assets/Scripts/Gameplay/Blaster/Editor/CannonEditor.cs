using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Cannon))]
public class CannonEditor : Editor
{
    
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        Cannon cannon = (Cannon) target;
        if (GUILayout.Button("Launch Dodgeball"))
        {
            cannon.LaunchDodgeball();
        }
    }
}