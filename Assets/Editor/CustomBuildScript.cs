using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class CustomBuildScript
{
     private static string manifestPath = "Packages/manifest.json";
    private static string[] xrPackages = {
        "\"com.unity.xr.interaction.toolkit\": \"2.5.4\",",
        "\"com.unity.xr.management\": \"4.3.3\",",
        "\"com.unity.xr.openxr\": \"1.10.0\",",
        "\"com.unity.modules.xr\": \"1.0.0\",",
        "\"com.unity.modules.vr\": \"1.0.0\","
    };

    [MenuItem("Build/Build Linux Server")]
    public static void BuildLinuxServer()
    {
        RemoveXRPackages();
        try
        {
            // Ensure XR settings are disabled
            PlayerSettings.virtualRealitySupported = false;
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(BuildTarget.StandaloneLinux64);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, "UNITY_SERVER");

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/MainScene.unity" },
                locationPathName = "Build/LinuxServer/YourServerExecutable",
                target = BuildTarget.StandaloneLinux64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log("Build succeeded: " + summary.totalSize + " bytes");
            }
            else if (summary.result == BuildResult.Failed)
            {
                Debug.LogError("Build failed");
            }
        }
        finally
        {
            ReaddXRPackages();
        }
    }

    private static void RemoveXRPackages()
    {
        if (File.Exists(manifestPath))
        {
            var lines = File.ReadAllLines(manifestPath);
            using (var writer = new StreamWriter(manifestPath))
            {
                foreach (var line in lines)
                {
                    bool shouldWriteLine = true;
                    foreach (var xrPackage in xrPackages)
                    {
                        if (line.Contains(xrPackage))
                        {
                            shouldWriteLine = false;
                            break;
                        }
                    }
                    if (shouldWriteLine)
                    {
                        writer.WriteLine(line);
                    }
                }
            }
            AssetDatabase.Refresh();
        }
        else
        {
            Debug.LogError("manifest.json not found!");
        }
    }
    private static void ReaddXRPackages()
    {
        if (File.Exists(manifestPath))
        {
            var lines = File.ReadAllLines(manifestPath);
            using (var writer = new StreamWriter(manifestPath))
            {
                bool added = false;
                foreach (var line in lines)
                {
                    writer.WriteLine(line);
                    if (line.Contains("\"dependencies\": {") && !added)
                    {
                        foreach (var xrPackage in xrPackages)
                        {
                            writer.WriteLine(xrPackage);
                        }
                        added = true;
                    }
                }
                if (!added)
                {
                    foreach (var xrPackage in xrPackages)
                    {
                        writer.WriteLine(xrPackage);
                    }
                }
            }
            AssetDatabase.Refresh();
        }
        else
        {
            Debug.LogError("manifest.json not found!");
        }
    }
}