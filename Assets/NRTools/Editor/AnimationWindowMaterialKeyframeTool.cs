using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class AnimationWindowMaterialKeyframeTool : EditorWindow
{
    private List<AnimationParticleEffect> _visualEffects = new();

    [MenuItem("Window/Animation/Material Keyframe Tool")]
    public static void ShowWindow()
    {
        GetWindow<AnimationWindowMaterialKeyframeTool>("Material Keyframe Tool");
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Keyframe Selected Object Materials")) AddMaterialKeyFrames();
    }

    private static void AddMaterialKeyFrames()
    {
        var selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("No objects selected");
            return;
        }

        var clip = GetActiveAnimationClip();
        if (clip == null)
        {
            Debug.LogWarning("No animation clip is active.");
            return;
        }

        foreach (var obj in selectedObjects)
        {
            var rend = obj.GetComponent<Renderer>();
            if (rend == null || rend.sharedMaterials.Length == 0)
            {
                Debug.LogWarning("No renderer or materials found on selected object.");
                continue;
            }

            Undo.RecordObject(clip, "Add Material Keyframes");

            foreach (var material in rend.sharedMaterials)
            {
                Debug.Log("Adding keyframes for material: " + material.name);
                Color materialColor = material.GetColor("_AlbedoColor");
                Color rimColor = material.GetColor("_RimColor");
                float smoothness = material.GetFloat("_Smoothness");

                AddKeyFrame(clip, obj, "_Smoothness", smoothness);
                AddKeyFrame(clip, obj, "_AlbedoColor", materialColor);
                AddKeyFrame(clip, obj, "_RimColor", rimColor);
            }
        }

        // todo: update the animator window
        var animationWindow = Resources.FindObjectsOfTypeAll<AnimationWindow>().FirstOrDefault();
        if (animationWindow != null) animationWindow.Repaint();

        // todo: helpful for the particle system preview editor
        EditorApplication.QueuePlayerLoopUpdate();
    }

    private void UpdateParticleSystem()
    {
        if (AnimationMode.InAnimationMode())
        {
            foreach (var particleEffect in _visualEffects)
            {
                // AnimationUtility.GetAnimationEvents()
                if (particleEffect.frameToEnable == /* current frame */0)
                    particleEffect.particleEffect.Play();
            }
        }

        EditorApplication.QueuePlayerLoopUpdate();
    }

    private static Transform GetAnimationRoot(GameObject obj)
    {
        var animator = obj.GetComponentInParent<Animator>();
        if (animator != null)
        {
            Debug.Log($"animator root was {animator.gameObject.name}");
            return animator.transform;
        }

        Debug.LogWarning("No Animator found in parent hierarchy.");
        return null;
    }

    private static AnimationClip GetActiveAnimationClip()
    {
        var animationWindow = Resources.FindObjectsOfTypeAll<AnimationWindow>().FirstOrDefault();
        if (animationWindow != null && animationWindow.animationClip != null) return animationWindow.animationClip;

        Debug.LogWarning("No animation window found.");
        return null;
    }

    private static void AddKeyFrame(AnimationClip clip, GameObject obj, string propertyName, float value)
    {
        var renderer = obj.GetComponent<Renderer>();
        if (renderer == null || renderer.sharedMaterial == null) return;

        var path = AnimationUtility.CalculateTransformPath(obj.transform, GetAnimationRoot(obj));
        clip.SetCurve(path, typeof(Renderer), $"material.{propertyName}", AnimationCurve.Constant(0, 0, value));
    }

    private static void AddKeyFrame(AnimationClip clip, GameObject obj, string propertyName, Color color)
    {
        var renderer = obj.GetComponent<Renderer>();
        if (renderer == null || renderer.sharedMaterial == null) return;

        var path = AnimationUtility.CalculateTransformPath(obj.transform, GetAnimationRoot(obj));
        clip.SetCurve(path, typeof(Renderer), $"material.{propertyName}.r", AnimationCurve.Constant(0, 0, color.r));
        clip.SetCurve(path, typeof(Renderer), $"material.{propertyName}.g", AnimationCurve.Constant(0, 0, color.g));
        clip.SetCurve(path, typeof(Renderer), $"material.{propertyName}.b", AnimationCurve.Constant(0, 0, color.b));
        clip.SetCurve(path, typeof(Renderer), $"material.{propertyName}.a", AnimationCurve.Constant(0, 0, color.a));
    }
}

[Serializable]
public class AnimationParticleEffect
{
    public ParticleSystem particleEffect;
    public int frameToEnable;
    public int frameToDisable;
    internal bool isPlaying;
    public bool looping;
}