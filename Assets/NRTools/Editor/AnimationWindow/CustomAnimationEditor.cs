using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class CustomAnimationEditor : EditorWindow
{
    private AnimationClip animationClip;
    private Vector2 scrollPosition;
    private SerializedObject serializedObject;
    private SerializedProperty keyframes;
    private Animator selectedAnimator;
    private int selectedClipIndex = 0;
    private string propertySearchFeild = "";

    private Dictionary<string, UIState> foldoutStates = new();
    private AnimationClip[] animationClips;

    // For transform hierarchy
    private Transform[] transforms;

    [MenuItem("Neon Raven/Custom Animation Editor")]
    public static void ShowWindow()
    {
        GetWindow<CustomAnimationEditor>("Custom Animation Editor");
    }

    private void OnEnable()
    {
        serializedObject = new SerializedObject(this);
    }

    private float leftViewWidth = 350f;
    private float rightViewWidth;

    private void OnGUI()
    {
        GUILayout.Label("Select Animator", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        selectedAnimator = (Animator) EditorGUILayout.ObjectField(selectedAnimator, typeof(Animator), true);

        if (selectedAnimator != null)
        {
            // Retrieve all animation clips from the selected animator
            animationClips = selectedAnimator.runtimeAnimatorController.animationClips;

            // Dropdown to select an Animation Clip
            GUILayout.Label("Select Animation Clip", EditorStyles.boldLabel);
            string[] clipNames = animationClips.Select(clip => clip.name).ToArray();
            selectedClipIndex = EditorGUILayout.Popup(selectedClipIndex, clipNames);

            // Assign the selected clip to animationClip
            animationClip = animationClips[selectedClipIndex];
        }
        else
        {
            EditorGUILayout.EndHorizontal();
            return;
        }

        GUILayout.Label("Animation Clip", EditorStyles.boldLabel);
        var anim = animationClip;
        animationClip = (AnimationClip) EditorGUILayout.ObjectField(animationClip, typeof(AnimationClip), false);
        if (anim != animationClip || foldoutStates.Count == 0)
        {
            foldoutStates.Clear();
            PopulateUIStateDictionary(animationClip);
        }

        EditorGUILayout.EndHorizontal();
        propertySearchFeild = EditorGUILayout.TextField("Search Properties", propertySearchFeild);
        EditorGUILayout.Space();
        rightViewWidth = leftViewWidth * 3;
        scrollPosition =
            EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Width(leftViewWidth + rightViewWidth),
                GUILayout.Height(300));

        if (animationClip != null)
        {
            EditorGUILayout.BeginHorizontal();
            DisplayTransformHierarchy();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            DrawTimeline(animationClip, _currentTime, rightViewWidth, 300);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();

            AnimationPropertyControl.AddPropertyButton(leftViewWidth);

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }
    }

    private float _currentTime;

private void DrawTimeline(AnimationClip clip, float currentTime, float width, float height)
{
    var yOffset = 26f;
    // Collect displayed properties based on the foldout states
    List<UIState> displayedProperties = new List<UIState>();
    CollectDisplayedProperties(foldoutStates, displayedProperties);

    float rowHeight = 16f; // Adjust this value according to your property row height
    float pixelsPerSecond = width / clip.length;

    // Background for the timeline
    EditorGUI.DrawRect(new Rect(leftViewWidth, yOffset, width, Math.Max(300, rowHeight * displayedProperties.Count)), Color.grey);

    // Draw time markers
    float timeStep = 0.1f; // Example: each marker represents 0.1 seconds
    for (float t = 0; t < clip.length; t += timeStep)
    {
        float xPos = t * pixelsPerSecond + leftViewWidth;
        EditorGUI.DrawRect(new Rect(xPos, yOffset, 1, Math.Max(300, rowHeight * displayedProperties.Count)), Color.white);
    }

    // Draw keyframes
    foreach (var display in displayedProperties)
    {
        if (string.IsNullOrEmpty(display.PropertyName)) continue;
        var binding = AnimationPropertyControl.GetBindingForState(animationClip, display);
        if (!binding.HasValue) continue;
        
        int propertyIndex = displayedProperties.IndexOf(display);
        if (propertyIndex == -1) continue; // Skip if property is not in the displayed list

        var curve = AnimationUtility.GetEditorCurve(clip, binding.Value);
        foreach (var key in curve.keys)
        {
            float xPos = key.time * pixelsPerSecond + leftViewWidth;
            float yPos = propertyIndex * rowHeight + yOffset; // Calculate YPosition based on property index
            EditorGUI.DrawRect(new Rect(xPos, yPos, 3, rowHeight - 2), Color.green);
        } 
    }

    // Draw the current time cursor
    float cursorXPos = currentTime * pixelsPerSecond + leftViewWidth;
    EditorGUI.DrawRect(new Rect(cursorXPos, yOffset, 2, rowHeight * displayedProperties.Count), Color.red);

    // Display the current time as text
    EditorGUI.LabelField(new Rect(cursorXPos + 5, height - 20, 100, 20), $"{currentTime:F2}s");
}

private void CollectDisplayedProperties(Dictionary<string, UIState> foldoutStates, List<UIState> displayedProperties)
{
    foreach (var foldout in foldoutStates)
    {
        if (foldout.Value.IsFoldedOut)
        {
            // Add the foldout itself if needed
            displayedProperties.Add(foldout.Value);

            // Recursively add children
            AddChildrenToDisplayedProperties(foldout.Value, displayedProperties);
        }
    }
}

private void AddChildrenToDisplayedProperties(UIState parentState, List<UIState> displayedProperties)
{
    foreach (var child in parentState.Children)
    {
        // Only add and process children that are folded out
        if (parentState.IsFoldedOut)
        {
            displayedProperties.Add(child);

            // Recursively add grandchildren, etc.
            AddChildrenToDisplayedProperties(child, displayedProperties);
        }
    }
}


    
    private void CollectChildProperties(List<UIState> children, List<UIState> displayedProperties)
    {
        foreach (var child in children)
        {
            displayedProperties.Add(child);

            // If the child is folded out, add its children
            if (child.IsFoldedOut)
            {
                CollectChildProperties(child.Children, displayedProperties);
            }
        }
    }


// Call this method where you are handling user input to update `currentTime`
    void HandleTimelineInput(float pixelsPerSecond, float width)
    {
        if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag)
        {
            Vector2 mousePosition = Event.current.mousePosition;
            if (mousePosition.y >= 0 &&
                mousePosition.y <= 300) // Ensure the click is within the timeline's vertical bounds
            {
                _currentTime = mousePosition.x / pixelsPerSecond;
                _currentTime = Mathf.Clamp(_currentTime, 0, animationClip.length);
                Event.current.Use();
            }
        }
    }

    private void DisplayTransformHierarchy()
    {
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField("Animation Properties", EditorStyles.boldLabel);

        if (animationClip != null)
        {
            AnimationPropertyControl.DrawControl(animationClip, foldoutStates, propertySearchFeild, leftViewWidth - 13);
        }
        else
        {
            EditorGUILayout.LabelField("No animation clip selected.");
        }

        EditorGUILayout.EndVertical();
    }

private void PopulateUIStateDictionary(AnimationClip animationClip)
{
    var bindings = AnimationUtility.GetCurveBindings(animationClip);

    var groupedBindings = bindings.GroupBy(binding => binding.path)
        .Select(group =>
        {
            var nameStr = group.Key.Split('/').Last();
            if (nameStr.StartsWith("Bind_")) nameStr = nameStr[5..];
            return new
            {
                Name = nameStr,
                Path = group.Key,
                Group = group.GroupBy(binding => binding.propertyName.Split('.').First())
            };
        })
        .Where(item =>
            string.IsNullOrEmpty(propertySearchFeild) ||
            item.Name.Contains(propertySearchFeild, StringComparison.OrdinalIgnoreCase))
        .OrderBy(item => item.Name)
        .ToList();

    foldoutStates.Clear();

    foreach (var group in groupedBindings)
    {
        string topLevelKey = group.Path;

        foldoutStates[topLevelKey] = new UIState
        {
            IsFoldedOut = false,
            DisplayName = group.Name,
        };

        foreach (var propertyGroup in group.Group)
        {
            var state = new UIState
            {
                IsFoldedOut = false,
                DisplayName = propertyGroup.Key,
                Path = group.Path
            };
            foldoutStates[topLevelKey].Children.Add(state);
            
            foreach (var binding in propertyGroup)
            {
                state.Children.Add(new UIState
                {
                    IsFoldedOut = false,
                    DisplayName = binding.propertyName[(binding.propertyName.LastIndexOf('.') + 1)..],
                    Path = group.Path,
                    PropertyName = binding.propertyName
                });
            }
        }
    }
}


    private void DisplayKeyframes()
    {
        // for (int i = 0; i < keyframes.arraySize; i++)
        // {
        //     SerializedProperty keyframe = keyframes.GetArrayElementAtIndex(i);
        //     EditorGUILayout.PropertyField(keyframe);
        // }
        //
        // // Option to add new properties (keyframes)
        // if (GUILayout.Button("Add Keyframe"))
        // {
        //     keyframes.InsertArrayElementAtIndex(keyframes.arraySize);
        // }
    }

    private void PlayParticleSystems()
    {
        // Logic to play particle systems at keyframe positions
    }
}