using Hands.SinglePlayer.EnemyAI.Priority;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PriorityHandler))]
public class PriorityHandlerEditor : Editor
{
    private bool targetUtilityFoldout = true;
    private bool dodgeUtilityFoldout = true;
    private bool moveUtilityFoldout = true;
    private bool catchUtilityFoldout = true;
    private bool pickUpUtilityFoldout = true;
    private bool throwUtilityFoldout = true;
    private bool outOfBoundsUtilityFoldout = true;

    public override void OnInspectorGUI()
    {
        PriorityHandler handler = (PriorityHandler)target;

        handler.maxValue = EditorGUILayout.FloatField("Max Value", handler.maxValue);
        handler.recative = EditorGUILayout.Toggle("Reactive", handler.recative);

        if (GUILayout.Button("Balance All Priorities"))
        {
            BalanceAllPriorities(handler);
        }

        DrawUtilityPriorities(ref targetUtilityFoldout, handler.targetUtility, "Target Utility",handler);
        DrawUtilityPriorities(ref dodgeUtilityFoldout, handler.dodgeUtility, "Dodge Utility", handler);
        DrawUtilityPriorities(ref moveUtilityFoldout, handler.moveUtility, "Move Utility", handler);
        DrawUtilityPriorities(ref catchUtilityFoldout, handler.catchUtility, "Catch Utility", handler);
        DrawUtilityPriorities(ref pickUpUtilityFoldout, handler.pickUpUtility, "Pick Up Utility", handler);
        DrawUtilityPriorities(ref throwUtilityFoldout, handler.throwUtility, "Throw Utility", handler);
        DrawUtilityPriorities(ref outOfBoundsUtilityFoldout, handler.outOfBoundsUtility, "Out of Bounds Utility", handler);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawUtilityPriorities(ref bool foldout, PriorityData data, string label, PriorityHandler handler)
    {
        foldout = EditorGUILayout.Foldout(foldout, label, true);
        if (foldout)
        {
            EditorGUI.indentLevel++;

            if (GUILayout.Button("Balance Priorities"))
            {
                data.BalancePriorities();
            }

            data.recative = handler.recative;
            data.maxValue = handler.maxValue;
            foreach (var priority in data.priorities)
            {
                EditorGUILayout.BeginHorizontal();
                priority.priority = (PriorityType)EditorGUILayout.EnumPopup(priority.priority);
                var score = EditorGUILayout.Slider(priority.score, 0, data.maxValue);
                data.BalancePrioritiesAround(priority.priority, score);
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Add New Priority"))
            {
                AddNewPriority(data);
            }
            EditorGUI.indentLevel--;
        }
    }

    private void BalanceAllPriorities(PriorityHandler handler)
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
