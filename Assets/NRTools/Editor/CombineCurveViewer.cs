using System;
using UnityEditor;
using UnityEngine;

namespace CloudFine.ThrowLab
{
    public class CombineCurveViewer : EditorWindow
    {
        public string jointName = "Hips";
        public CombinedCurve combinedCurve;
        public AnimationClip clip;
        public string propertyPath = "m_LocalPosition";
        private Vector2 scrollPos;

        [MenuItem("Neon Raven/Combined Curve Viewer")]
        public static void ShowWindow()
        {
            GetWindow<CombineCurveViewer>("Combined Curve Viewer");
        }

        private void ExtractCurvesFromClip()
        {
            combinedCurve = ScriptableObject.CreateInstance<CombinedCurve>();

            // Use the jointName in the binding path
            AnimationCurve curveX = AnimationUtility.GetEditorCurve(clip,
                EditorCurveBinding.FloatCurve(jointName, typeof(Transform), $"{propertyPath}.x"));
            AnimationCurve curveY = AnimationUtility.GetEditorCurve(clip,
                EditorCurveBinding.FloatCurve(jointName, typeof(Transform), $"{propertyPath}.y"));
            AnimationCurve curveZ = AnimationUtility.GetEditorCurve(clip,
                EditorCurveBinding.FloatCurve(jointName, typeof(Transform), $"{propertyPath}.z"));

            if (curveX == null && curveY == null && curveZ == null)
            {
                Debug.LogError($"No curve found for {jointName}.{propertyPath}");
                return;
            }

            int keyframeCount = Mathf.Max(curveX?.length ?? 0, curveY?.length ?? 0, curveZ?.length ?? 0);
            combinedCurve.keyframes = new Vector3Keyframe[keyframeCount];

            for (int i = 0; i < keyframeCount; i++)
            {
                float time = i < curveX.length ? curveX.keys[i].time :
                    i < curveY.length ? curveY.keys[i].time : curveZ.keys[i].time;

                Vector3 value = new Vector3(
                    curveX?.Evaluate(time) ?? 0f,
                    curveY?.Evaluate(time) ?? 0f,
                    curveZ?.Evaluate(time) ?? 0f
                );

                combinedCurve.keyframes[i] = new Vector3Keyframe {time = time, value = value};
            }

            EditorUtility.SetDirty(combinedCurve);
        }

        private void SaveCurve()
        {
            var path = FileUtil.GetScriptParentPath("CombineCurveViewer.cs") + $"/CombinedCurves/{clip.name}.asset";
            AssetDatabase.CreateAsset(combinedCurve, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void OnGUI()
        {
            clip = (AnimationClip) EditorGUILayout.ObjectField("Animation Clip", clip, typeof(AnimationClip), false);
            propertyPath = EditorGUILayout.TextField("Property Path", propertyPath);
            jointName = EditorGUILayout.TextField("Joint Name", jointName);
            if (GUILayout.Button("Extract Curve") && clip != null)
            {
                ExtractCurvesFromClip();
            }

            combinedCurve =
                (CombinedCurve) EditorGUILayout.ObjectField("Combined Curve", combinedCurve, typeof(CombinedCurve),
                    false);



            if (combinedCurve != null)
            {
                if (GUILayout.Button("Save Curve") && clip != null)
                {
                    SaveCurve();
                }

                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Width(position.width),
                    GUILayout.Height(position.height - 40));

                Rect rect = GUILayoutUtility.GetRect(10, 2000, 100, 500);

                DrawCurves(rect);

                EditorGUILayout.EndScrollView();
            }

            // can we many an animation clip and extract the curve from a specific property?
        }

        private void DrawCurves(Rect rect)
        {
            if (combinedCurve.keyframes == null || combinedCurve.keyframes.Length == 0)
                return;

            Handles.color = Color.gray;
            Handles.DrawSolidRectangleWithOutline(rect, new Color(0.1f, 0.1f, 0.1f), Color.white);

            float minTime = combinedCurve.keyframes[0].time;
            float maxTime = combinedCurve.keyframes[combinedCurve.keyframes.Length - 1].time;

            // Determine the min and max values across all curves to normalize them within the rect
            float minValue = Mathf.Min(GetMinValue(Vector3Component.X), GetMinValue(Vector3Component.Y),
                GetMinValue(Vector3Component.Z));
            float maxValue = Mathf.Max(GetMaxValue(Vector3Component.X), GetMaxValue(Vector3Component.Y),
                GetMaxValue(Vector3Component.Z));

            for (int i = 0; i < combinedCurve.keyframes.Length - 1; i++)
            {
                Vector3 current = combinedCurve.keyframes[i].value;
                Vector3 next = combinedCurve.keyframes[i + 1].value;

                float currentT = Mathf.InverseLerp(minTime, maxTime, combinedCurve.keyframes[i].time);
                float nextT = Mathf.InverseLerp(minTime, maxTime, combinedCurve.keyframes[i + 1].time);

                // Draw X Curve
                DrawCurveSegment(rect, currentT, nextT, current.x, next.x, minValue, maxValue, Color.red);

                // Draw Y Curve
                DrawCurveSegment(rect, currentT, nextT, current.y, next.y, minValue, maxValue, Color.green);

                // Draw Z Curve
                DrawCurveSegment(rect, currentT, nextT, current.z, next.z, minValue, maxValue, Color.blue);
            }
        }

        private void DrawCurveSegment(Rect rect, float startT, float endT, float startValue, float endValue,
            float minValue, float maxValue, Color color)
        {
            Vector3 startPoint = new Vector3(
                Mathf.Lerp(rect.xMin, rect.xMax, startT),
                Mathf.Lerp(rect.yMax, rect.yMin, Mathf.InverseLerp(minValue, maxValue, startValue))
            );

            Vector3 endPoint = new Vector3(
                Mathf.Lerp(rect.xMin, rect.xMax, endT),
                Mathf.Lerp(rect.yMax, rect.yMin, Mathf.InverseLerp(minValue, maxValue, endValue))
            );

            Handles.color = color;
            Handles.DrawLine(startPoint, endPoint);
        }

        private float GetMinValue(Vector3Component component)
        {
            float minValue = float.MaxValue;
            foreach (var keyframe in combinedCurve.keyframes)
            {
                float value = GetComponentValue(keyframe.value, component);
                if (value < minValue) minValue = value;
            }

            return minValue;
        }

        private float GetMaxValue(Vector3Component component)
        {
            float maxValue = float.MinValue;
            foreach (var keyframe in combinedCurve.keyframes)
            {
                float value = GetComponentValue(keyframe.value, component);
                if (value > maxValue) maxValue = value;
            }

            return maxValue;
        }

        private float GetComponentValue(Vector3 vector, Vector3Component component)
        {
            switch (component)
            {
                case Vector3Component.X: return vector.x;
                case Vector3Component.Y: return vector.y;
                case Vector3Component.Z: return vector.z;
                default: return 0;
            }
        }

        private enum Vector3Component
        {
            X,
            Y,
            Z
        }
    }
}