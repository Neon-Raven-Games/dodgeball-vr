using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

public class HandPoseEditor : EditorWindow
{
    private GameObject animatorModel;
    private GameObject rightHandRoot;
    private GameObject leftHandRoot;
    private readonly string _handPoseDataPath = "Assets/HandPoses/Data/";
    private readonly string _handPoseAnimationPath = "Assets/HandPoses/Animations/";
    private string _fileName = "NewHandPose";
    
    [MenuItem("Neon Raven/Hand Pose Editor")]
    public static void ShowWindow()
    {
        GetWindow<HandPoseEditor>("Hand Pose Editor");
    }

    private void OnGUI()
    {
        GUILayout.Label("Hand Pose Capture", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox("Add the pose to the left hand and press the button to save both as an asset.", MessageType.Warning);
        animatorModel = (GameObject)EditorGUILayout.ObjectField("Animator Model", animatorModel, typeof(GameObject), true);
        leftHandRoot = (GameObject)EditorGUILayout.ObjectField("Left Hand Root", leftHandRoot, typeof(GameObject), true);
        rightHandRoot = (GameObject)EditorGUILayout.ObjectField("Right Hand Root", rightHandRoot, typeof(GameObject), true);
        _fileName = EditorGUILayout.TextField("File Name", _fileName);
        if (GUILayout.Button("Capture Pose"))
        {
            if (leftHandRoot != null)
            {
                SaveHandPose();
            }
            else
            {
                Debug.LogWarning("Hand root is not assigned.");
            }
        }
        if (GUILayout.Button("Bake Pose to Animation"))
        {
            if (leftHandRoot != null && rightHandRoot != null)
            {
                BakePoseToAnimation();
            }
            else
            {
                Debug.LogWarning("Hand root is not assigned.");
            }
        }
    }

    private void BakePoseToAnimation()
    {
        if (!Directory.Exists(_handPoseAnimationPath)) Directory.CreateDirectory(_handPoseAnimationPath);
        if (!Directory.Exists(_handPoseDataPath))
        {
            Debug.LogWarning("No data was found at path: " + _handPoseDataPath);
            return;
        }
        string[] assetGUIDs = AssetDatabase.FindAssets("t:HandPose", new[] { _handPoseDataPath });
        var assets = assetGUIDs.Select(guid => AssetDatabase.LoadAssetAtPath<HandPose>(AssetDatabase.GUIDToAssetPath(guid))).ToArray();

        if (assets.Length == 0)
        {
            Debug.LogWarning("No HandPose assets found at path: " + _handPoseDataPath);
            return;
        }
        
        foreach (var asset in assets)
        {
            if (asset is HandPose handPose)
            {
                bool isRightHand = asset.name.StartsWith("Right");
                string handPrefix = isRightHand ? "Right" : "Left";
                GameObject handRoot = isRightHand ? rightHandRoot : leftHandRoot;
                if (!Directory.Exists(_handPoseAnimationPath + handPrefix)) Directory.CreateDirectory(_handPoseAnimationPath + handPrefix);
                
                foreach (var transition in assets)
                {
                    if (!(transition is HandPose transitionPose)) continue;
                    if (transition == asset) continue;
                    if (transition.name.StartsWith(handPrefix))
                    {
                        string animationName = $"{asset.name}_to_{transition.name}.anim";
                        string animationPath = _handPoseAnimationPath + handPrefix + "/" + animationName;

                        CreateAnimationClip(animationPath, handRoot, handPose, transitionPose);
                    }
                }
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    
    private void CreateAnimationClip(string animationPath, GameObject handRoot, HandPose fromPose, HandPose toPose)
    {
        var animationClip = new AnimationClip();
        var fingerTransforms = handRoot.GetComponentsInChildren<Transform>().Where(x => x.name.StartsWith("Bind")).ToArray();

        for (int i = 0; i < fingerTransforms.Length; i++)
        {
            Transform transform = fingerTransforms[i];
            string path = AnimationUtility.CalculateTransformPath(transform, handRoot.transform);

            // Create position curves
            AnimationCurve curvePosX = AnimationCurve.Linear(0, fromPose.fingerTransforms[i].localPosition.x, 1, toPose.fingerTransforms[i].localPosition.x);
            AnimationCurve curvePosY = AnimationCurve.Linear(0, fromPose.fingerTransforms[i].localPosition.y, 1, toPose.fingerTransforms[i].localPosition.y);
            AnimationCurve curvePosZ = AnimationCurve.Linear(0, fromPose.fingerTransforms[i].localPosition.z, 1, toPose.fingerTransforms[i].localPosition.z);

            animationClip.SetCurve(path, typeof(Transform), "localPosition.x", curvePosX);
            animationClip.SetCurve(path, typeof(Transform), "localPosition.y", curvePosY);
            animationClip.SetCurve(path, typeof(Transform), "localPosition.z", curvePosZ);

            // Create rotation curves
            AnimationCurve curveRotX = AnimationCurve.Linear(0, fromPose.fingerTransforms[i].localRotation.x, 1, toPose.fingerTransforms[i].localRotation.x);
            AnimationCurve curveRotY = AnimationCurve.Linear(0, fromPose.fingerTransforms[i].localRotation.y, 1, toPose.fingerTransforms[i].localRotation.y);
            AnimationCurve curveRotZ = AnimationCurve.Linear(0, fromPose.fingerTransforms[i].localRotation.z, 1, toPose.fingerTransforms[i].localRotation.z);
            AnimationCurve curveRotW = AnimationCurve.Linear(0, fromPose.fingerTransforms[i].localRotation.w, 1, toPose.fingerTransforms[i].localRotation.w);

            animationClip.SetCurve(path, typeof(Transform), "localRotation.x", curveRotX);
            animationClip.SetCurve(path, typeof(Transform), "localRotation.y", curveRotY);
            animationClip.SetCurve(path, typeof(Transform), "localRotation.z", curveRotZ);
            animationClip.SetCurve(path, typeof(Transform), "localRotation.w", curveRotW);
        }
                        
        AssetDatabase.CreateAsset(animationClip, animationPath);
    }
    
    private void SaveHandPose()
    {
        var path = _handPoseDataPath + "Left" + _fileName + ".asset";
        var rightPath = _handPoseDataPath + "Right" + _fileName + ".asset";
        var leftHandPose = CreateInstance<HandPose>();
        var rightHandPose = CreateInstance<HandPose>();
        
        var leftFingerTransforms = leftHandRoot.GetComponentsInChildren<Transform>().Where(x => x.name.StartsWith("Bind")).ToArray();
        leftHandPose.fingerTransforms = new HandPose.TransformData[leftFingerTransforms.Length];

        for (int i = 0; i < leftFingerTransforms.Length; i++)
        {
            leftHandPose.fingerTransforms[i] = new HandPose.TransformData
            {
                localPosition = leftFingerTransforms[i].localPosition,
                localRotation = leftFingerTransforms[i].localRotation
            };
        }
        var rightFingerTransforms = rightHandRoot.GetComponentsInChildren<Transform>().Where(x => x.name.StartsWith("Bind")).ToArray();
        rightHandPose.fingerTransforms = new HandPose.TransformData[rightFingerTransforms.Length];

        HandPoseUtility.MirrorLeftToRight(leftFingerTransforms, rightFingerTransforms);
        for (int i = 0; i < rightFingerTransforms.Length; i++)
        {
            rightHandPose.fingerTransforms[i] = new HandPose.TransformData
            {
                localPosition = rightFingerTransforms[i].localPosition,
                localRotation = rightFingerTransforms[i].localRotation
            };
        }
        
        if (!Directory.Exists(_handPoseDataPath)) Directory.CreateDirectory(_handPoseDataPath);
        AssetDatabase.CreateAsset(leftHandPose, path);
        AssetDatabase.CreateAsset(rightHandPose, rightPath);
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = leftHandPose;

        Debug.Log("Left hand pose saved to " + path);
        Debug.Log("Right hand pose saved to " + rightPath);
    }
}