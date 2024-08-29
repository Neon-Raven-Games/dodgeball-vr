using Hands.SinglePlayer.EnemyAI.Priority;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PriorityHandler))]
public class PriorityHandlerEditor : Editor
{
    private SerializedProperty maxValueProp;
    private SerializedProperty reactiveProp;
    private PriorityHandler handler;

    private void OnEnable()
    {
        handler = (PriorityHandler)target;
        maxValueProp = serializedObject.FindProperty("maxValue");
        reactiveProp = serializedObject.FindProperty("reactive");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("AI Priority Settings", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(maxValueProp, new GUIContent("Max Value"));
        EditorGUILayout.PropertyField(reactiveProp, new GUIContent("Reactive"));

        if (GUILayout.Button("Balance All Priorities"))
        {
            BalanceAllPriorities();
        }

        DrawUtilityFoldout("Target Utility", handler.targetUtility, ref handler.targetUtilityFoldout);
        DrawUtilityFoldout("Dodge Utility", handler.dodgeUtility, ref handler.dodgeUtilityFoldout);
        DrawUtilityFoldout("Move Utility", handler.moveUtility, ref handler.moveUtilityFoldout);
        DrawUtilityFoldout("Catch Utility", handler.catchUtility, ref handler.catchUtilityFoldout);
        DrawUtilityFoldout("Pick Up Utility", handler.pickUpUtility, ref handler.pickUpUtilityFoldout);
        DrawUtilityFoldout("Throw Utility", handler.throwUtility, ref handler.throwUtilityFoldout);
        DrawUtilityFoldout("Out of Bounds Utility", handler.outOfBoundsUtility, ref handler.outOfBoundsUtilityFoldout);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawUtilityFoldout(string label, PriorityData data, ref bool foldout)
    {
        foldout = EditorGUILayout.Foldout(foldout, label, true);
        if (foldout)
        {
            EditorGUI.indentLevel++;

            if (GUILayout.Button($"Balance {label} Priorities"))
            {
                data.BalancePriorities();
            }

            data.recative = EditorGUILayout.Toggle("Reactive", data.recative);
            data.maxValue = EditorGUILayout.FloatField("Max Value", data.maxValue);

            for (int i = 0; i < data.priorities.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                data.priorities[i].priority = (PriorityType)EditorGUILayout.EnumPopup(data.priorities[i].priority);
                data.priorities[i].score = EditorGUILayout.Slider(data.priorities[i].score, 0, data.maxValue);

                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    data.priorities.RemoveAt(i);
                }

                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Add New Priority"))
            {
                AddNewPriority(data);
            }

            EditorGUI.indentLevel--;
        }
    }

    private void BalanceAllPriorities()
    {
        handler.targetUtility.BalancePriorities();
        handler.dodgeUtility.BalancePriorities();
        handler.moveUtility.BalancePriorities();
        handler.catchUtility.BalancePriorities();
        handler.pickUpUtility.BalancePriorities();
        handler.throwUtility.BalancePriorities();
        handler.outOfBoundsUtility.BalancePriorities();
    }

    private void AddNewPriority(PriorityData data)
    {
        data.priorities.Add(new Priority { priority = PriorityType.DistanceToBall, score = 0 });
    }
}
