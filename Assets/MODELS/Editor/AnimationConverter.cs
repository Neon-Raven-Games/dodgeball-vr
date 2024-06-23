using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Formats.Fbx.Exporter;
using UnityEngine;

namespace MODELS.Editor
{
    public class AnimationConverter : MonoBehaviour
    {
        [MenuItem("Neon Raven/Convert FBX Generics to Humanoid Clips")]
        public static void ConvertFBXAnimationsToHumanoid()
        {
            var scriptPath = GetEditorScriptPath();

            var fbxFolderPath = scriptPath + "/RawFBX";
            var convertedAnimationsPath = scriptPath + "/ConvertedAnimations";
            var animatorControllerPath = scriptPath + "/ConvertedAnimations/AnimatorController.controller";

            Directory.CreateDirectory(convertedAnimationsPath);

            var fbxFiles = AssetDatabase.FindAssets("t:Model", new[] {fbxFolderPath});

            if (fbxFiles.Length == 0)
            {
                Debug.LogError("No FBX files found at path: " + fbxFolderPath);
                return;
            }

            var animationClips = new List<AnimationClip>();
            Avatar avatar = null;

            foreach (var guid in fbxFiles)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var modelImporter = AssetImporter.GetAtPath(path) as ModelImporter;

                if (modelImporter == null) continue;

                modelImporter.animationType = ModelImporterAnimationType.Human;
                modelImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                AssetDatabase.ImportAsset(path);
                var fbxFileName = Path.GetFileNameWithoutExtension(path);

                if (avatar == null)
                {
                    var fbxModel = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    avatar = Instantiate(fbxModel.GetComponent<Animator>().avatar);
                    var avatarPath = convertedAnimationsPath + "/Avatar.asset";
                    AssetDatabase.CreateAsset(avatar, avatarPath);
                }

                var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                var oldAnimations = new List<AnimationClip>();
                foreach (var asset in assets)
                {
                    if (asset is not AnimationClip clip || clip.name.Contains("__preview__")) continue;
                    oldAnimations.Add(clip);
                    
                    SetAnimationClipLoop(clip, true);
                   
                    var newPath = convertedAnimationsPath + "\\" + fbxFileName + ".anim";
                    Debug.Log("Writing Animation Clip to: " + newPath);
                    AssetDatabase.CreateAsset(Instantiate(clip), newPath);
                    animationClips.Add(AssetDatabase.LoadAssetAtPath<AnimationClip>(newPath));
                }

                if (oldAnimations.Count <= 0) continue;
                
                // does not work
                var newFbxPath = Path.Combine(fbxFolderPath, fbxFileName + ".fbx");
                CreateEmptyFbxWithAnimations(newFbxPath, oldAnimations);
                oldAnimations.Clear();
                
                // Delete the original FBX file if needed
                // AssetDatabase.DeleteAsset(path);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            CreateAnimatorController(animationClips, animatorControllerPath);

            AssetDatabase.Refresh();
        }

        // if you can get the animations to export in the gui, I can call it in code
        private static void CreateEmptyFbxWithAnimations(string path, List<AnimationClip> clips)
        {
            var tempGO = new GameObject("TempFBXCreator");
            var anim = tempGO.AddComponent<Animation>();
            foreach (var clip in clips)
            {
                anim.AddClip(clip, clip.name);
            }

            var options = new ExportModelOptions
            {
                AnimationSource = tempGO.transform,
                KeepInstances = true,
            };

            // this should export the animations to a new fbx file
            ModelExporter.ExportObject(path, tempGO, options);
            DestroyImmediate(tempGO);
        }

        private static string GetEditorScriptPath()
        {
            var directoryPath =
                Directory.GetFiles(Application.dataPath, "AnimationConverter.cs", SearchOption.AllDirectories);
            if (directoryPath.Length == 0) Debug.LogError("Could not find AnimationConverter.cs in project.");

            // yucky
            var scriptPath = directoryPath[0].Replace("AnimationConverter.cs", "").Replace("\\", "/");
            scriptPath = scriptPath.Substring(scriptPath.LastIndexOf("/Assets"),
                scriptPath.Length - scriptPath.LastIndexOf("/Assets"));
            scriptPath = scriptPath.Substring(1, scriptPath.Length - 2);
            scriptPath = scriptPath.Substring(0, scriptPath.LastIndexOf("/"));
            return scriptPath;
        }

        // todo, get with dan for naming conventions to set loop
        private static void SetAnimationClipLoop(AnimationClip clip, bool loop)
        {
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }

        private static void CreateAnimatorController(List<AnimationClip> clips, string animatorControllerPath)
        {
            var animatorController = AnimatorController.CreateAnimatorControllerAtPath(animatorControllerPath);

            foreach (var clip in clips)
            {
                var stateName = clip.name;
                var state = animatorController.AddMotion(clip);
                state.name = stateName;
            }

            AssetDatabase.SaveAssets();
        }
    }
}