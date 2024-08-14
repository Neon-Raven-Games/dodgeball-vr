using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HandCannon))]
public class CannonEditor : Editor
{
    
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        HandCannon cannon = (HandCannon) target;
        if (GUILayout.Button("Grip Action"))
            cannon.GripPerformedAction(default);
        if (GUILayout.Button("Grip Release Action"))
            cannon.GripReleasedAction(default);
        if (GUILayout.Button("Fire Action"))
            cannon.TriggerPerformedAction(default);
        if (GUILayout.Button("Fire Release Action"))
            cannon.TriggerPerformedAction(default);
        
    }
}