using UnityEngine;
using UnityEditor;

public class CharacterGizmo : MonoBehaviour
{
    [SerializeField] private string gizmoText = "Character";
    [SerializeField] private Color gizmoColor = Color.cyan;
    [SerializeField] private Color labelColor = Color.white;
    [SerializeField] private float labelOffset = 0.7f;
    [SerializeField] private float fontSize = 12;
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        DrawCharacterGizmo();
    }

    private void DrawCharacterGizmo()
    {
        // Draw the sphere at the character's position
        Gizmos.color = gizmoColor;
        // Draw the label above the character
        Vector3 labelPosition = transform.position + Vector3.up * labelOffset;
        DrawLabel(labelPosition, gizmoText);
    }

    private void DrawLabel(Vector3 position, string text)
    {
        // Set up the GUI style for the label
        GUIStyle style = new GUIStyle
        {
            normal = { textColor = labelColor },
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12
        };

        // Draw the label using Handles
        Handles.Label(position, text, style);
    }
#endif
}