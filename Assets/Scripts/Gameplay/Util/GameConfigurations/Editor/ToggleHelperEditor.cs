using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ToggleHelper))]
public class ToggleHelperEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ToggleHelper toggleHelper = (ToggleHelper)target;

        if (GUILayout.Button("Upvote"))
        {
            toggleHelper.ShipUpDoot();
        }

        if (GUILayout.Button("Downvote"))
        {
            toggleHelper.ShipDownVote();
        }
    }
}