using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using System.Reflection;

namespace Assets.DuckType.Jiggle
{
    [CustomEditor(typeof(Jiggle))]
    [CanEditMultipleObjects]
    class JiggleEditor : Editor
    {
        private SerializedProperty UseCenterOfMass_SP = null;
        SerializedProperty CenterOfMass_SP = null;
        SerializedProperty CenterOfMassInertia_SP = null;
        SerializedProperty AddWind_SP = null;
        SerializedProperty WindDirection_SP = null;
        SerializedProperty WindStrength_SP = null;
        SerializedProperty AddNoise_SP = null;
        SerializedProperty NoiseStrength_SP = null;
        SerializedProperty NoiseScale_SP = null;
        SerializedProperty NoiseSpeed_SP = null;

        SerializedProperty RotationInertia_SP = null;
        SerializedProperty Gravity_SP = null;
        SerializedProperty SpringStrength_SP = null;

        SerializedProperty Dampening_SP = null;

        SerializedProperty BlendToOriginalRotation_SP = null;

        SerializedProperty Hinge_SP = null;
        SerializedProperty HingeAngle_SP = null;

        SerializedProperty UseAngleLimit_SP = null;
        SerializedProperty AngleLimit_SP = null;
        SerializedProperty UseSoftLimit_SP = null;
        SerializedProperty SoftLimitInfluence_SP = null;
        SerializedProperty SoftLimitStrength_SP = null;

        SerializedProperty UpdateWithPhysics_SP = null;

        SerializedProperty ShowViewportGizmos_SP = null;
        SerializedProperty GizmoScale_SP = null;

        void OnEnable()
        {
            // set all SerializedProperties using reflection. easier than doing them one-by-one
            var properties = this.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var p in properties.Where(x => x.FieldType == typeof(SerializedProperty) && x.Name.EndsWith("_SP")))
                p.SetValue(this, serializedObject.FindProperty(p.Name.Replace("_SP", "")));
        }

        public override void OnInspectorGUI()
        {
            // Update the serializedProperty - always do this in the beginning of OnInspectorGUI.
            serializedObject.Update();

            //var t = target as Jiggle;
            
            EditorGUILayout.PropertyField(UseCenterOfMass_SP, new GUIContent("Use Center Of Mass", "Center of mass lets objects \"trail\" based on their own weight. It is highly recommended to use this"));
            if (UseCenterOfMass_SP.boolValue != false)
            {
                EditorGUILayout.PropertyField(CenterOfMass_SP, new GUIContent("Center Of Mass", "The point around which the object naturally wants to rotate"));
                if (CenterOfMass_SP.vector3Value.magnitude == 0)
                    CenterOfMass_SP.vector3Value = Vector3.right * 0.001f;
                
                EditorGUILayout.Slider(CenterOfMassInertia_SP, 0, 1, new GUIContent("Mass Inertia", "How much does the object trail after its parent based on its center of mass"));
                EditorGUILayout.Slider(Gravity_SP, 0, 1, new GUIContent("Gravity", "How much does gravity affect this object"));

                EditorGUILayout.PropertyField(AddWind_SP, new GUIContent("Add Wind", "Adds a costant force to the center of mass to simulate constant wind"));
                if (AddWind_SP.boolValue != false)
                {
                    EditorGUILayout.PropertyField(WindDirection_SP, new GUIContent("Direction", "The direction the wind is blowing in"));
                    if (WindDirection_SP.vector3Value.magnitude == 0)
                        WindDirection_SP.vector3Value = Vector3.right * 0.001f;
                    EditorGUILayout.Slider(WindStrength_SP, 0, 1, new GUIContent("Strength", "How strongly the wind affects the center of mass"));
                }
                EditorGUILayout.PropertyField(AddNoise_SP, new GUIContent("Add Noise", "Adds a noise force to the center of mass to simulate constant turbulence"));
                if (AddNoise_SP.boolValue != false)
                {
                    EditorGUILayout.Slider(NoiseScale_SP, 0, 100, new GUIContent("Scale", "Larger values create more even noise, smaller values create more irregular noise"));
                    EditorGUILayout.Slider(NoiseStrength_SP, 0, 100, new GUIContent("Strength", "How strongly the noise affects the center of mass"));
                    EditorGUILayout.Slider(NoiseSpeed_SP, 0, 100, new GUIContent("Speed", "How quickly the noise changes over time"));
                }

            }
            EditorGUILayout.Separator();

            EditorGUILayout.Slider(RotationInertia_SP, 0, 1, new GUIContent("Rotation Inertia", "How much does the object's rotation lag behind"));
            EditorGUILayout.Separator();

            EditorGUILayout.Slider(SpringStrength_SP, 0, 1, "Spring Strength");
            EditorGUILayout.Separator();

            EditorGUILayout.Slider(Dampening_SP, 0, 1, "Dampening");
            EditorGUILayout.Separator();

            EditorGUILayout.PropertyField(BlendToOriginalRotation_SP, new GUIContent("Blend with original rotation", "Blends Jiggle with the object's original rotation, for example existing keyframe animation"));
            EditorGUILayout.Separator();

            EditorGUILayout.PropertyField(Hinge_SP, new GUIContent("Hinge", "Limit the object's rotation to a single axis"));
            if (Hinge_SP.boolValue != false)
                EditorGUILayout.Slider(HingeAngle_SP, -90, 90, new GUIContent("Hinge Orientation", "The direction of the hinge rotation"));

            EditorGUILayout.PropertyField(UseAngleLimit_SP, new GUIContent("Limit Angle", "Restrict how far the object can rotate away from its rest orientation"));
            if (UseAngleLimit_SP.boolValue != false)
            {
                EditorGUILayout.Slider(AngleLimit_SP, 0, 180, new GUIContent("Limit", "How far can the object rotation from its rest rotation"));
                EditorGUILayout.PropertyField(UseSoftLimit_SP, new GUIContent("Soft Limit", "Brake the object's rotation before it hits the limit"));
                if (UseSoftLimit_SP.boolValue != false)
                {
                    EditorGUILayout.Slider(SoftLimitInfluence_SP, 0, 1, new GUIContent("Influence", "How far from the limit does the soft limit begin"));
                    EditorGUILayout.Slider(SoftLimitStrength_SP, 0, 1, new GUIContent("Strength", "How strongly does the soft limit push back"));
                }
            }
            EditorGUILayout.Separator();

            EditorGUILayout.PropertyField(UpdateWithPhysics_SP, new GUIContent("Update During Physics Step (Experimental)", "EXPERIMENTAL:\nThis makes Jiggle update during the physics step. Use this setting if you are getting jittery results when Jiggle is attached to physics objects such as rigidbodies or character controllers.\n\nBe careful about mixing physics with non-physics Jiggle scripts."));

            EditorGUILayout.Separator();

            EditorGUILayout.PropertyField(ShowViewportGizmos_SP, new GUIContent("Show Viewport Gizmos", ""));
            if (ShowViewportGizmos_SP.boolValue != false)
                EditorGUILayout.Slider(GizmoScale_SP, 0, 1, new GUIContent("Scale", "How large are the script's Gizmos in the viewport"));

            // Apply changes to the serializedProperty - always do this in the end of OnInspectorGUI.
            serializedObject.ApplyModifiedProperties();
            
            /*if (GUI.changed)
                EditorUtility.SetDirty(t);*/
        }
    }
}