using UnityEditor;
using UnityEngine;

namespace CloudFine.ThrowLab.Testing
{
    [CustomEditor(typeof(ShadowCourt))]
    public class ShadowCourtEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button("Shadow Court Move")) ShadowCourt.SmokeScreen();
        }
    }
}