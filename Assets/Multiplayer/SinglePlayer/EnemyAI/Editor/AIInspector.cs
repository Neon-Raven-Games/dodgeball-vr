using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AIInspectorTarget))]
public class AIInspector : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        AIInspectorTarget targetScript = (AIInspectorTarget)target;

        DrawAccessoryControls("Hair", targetScript.hairParent);
        DrawAccessoryControls("Brow", targetScript.browParent);
        DrawAccessoryControls("Eye", targetScript.eyeParent);

        if (GUI.changed)
        {
            EditorUtility.SetDirty(targetScript);
        }
    }

    private void DrawAccessoryControls(string label, Transform parent)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(50));

        if (GUILayout.Button("<", GUILayout.Width(30)))
        {
            ActivatePreviousAccessory(parent);
        }

        if (GUILayout.Button("Rand", GUILayout.Width(60)))
        {
            ActivateRandomAccessory(parent);
        }

        if (GUILayout.Button(">", GUILayout.Width(30)))
        {
            ActivateNextAccessory(parent);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void ActivatePreviousAccessory(Transform parent)
    {
        int activeIndex = GetActiveChildIndex(parent);
        int newIndex = (activeIndex - 1 + parent.childCount) % parent.childCount;
        SetActiveChild(parent, newIndex);
    }

    private void ActivateRandomAccessory(Transform parent)
    {
        int randomIndex = Random.Range(0, parent.childCount);
        SetActiveChild(parent, randomIndex);
    }

    private void ActivateNextAccessory(Transform parent)
    {
        int activeIndex = GetActiveChildIndex(parent);
        int newIndex = (activeIndex + 1) % parent.childCount;
        SetActiveChild(parent, newIndex);
    }

    private int GetActiveChildIndex(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            if (parent.GetChild(i).gameObject.activeSelf)
            {
                return i;
            }
        }
        return -1; // No active child found
    }

    private void SetActiveChild(Transform parent, int index)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            parent.GetChild(i).gameObject.SetActive(i == index);
        }
    }
}
