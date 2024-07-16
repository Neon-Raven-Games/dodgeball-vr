using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CustomBuildScript
{
    private const string _BUILD_PATH = @"C:/Users/maros/Desktop/NeonRaven/Dodgeball2.0/";
    private const string _POST_REQUEST_URL = "https://192.168.0.104:5001/api/Server/upload";
    private const string _BUILD_NAME = "DodgeballServer";
    private const string _START_SERVER_URL = "https://192.168.0.104:5001/api/Server/start?serverName=DodgeballServer";
    private const string _STOP_SERVER_URL = "https://192.168.0.104:5001/api/Server/stop?serverName=DodgeballServer";

    [MenuItem("Build/Start Remote Server")]
    public static void StartRemoteServer()
    {
        try
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) =>
                {
                    // Custom logic for certificate validation
                    // For testing purposes, accept all certificates
                    return true;
                }
            };
            using var client = new HttpClient(handler);
            var response = client.GetAsync(_START_SERVER_URL).GetAwaiter().GetResult();
            Debug.Log("Server start response: " + response.StatusCode);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to start server: " + e.Message);
        }
    }
    
    [MenuItem("Build/Stop Remote Server")]
    public static void StopRemoteServer()
    {
        try
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) =>
                {
                    // Custom logic for certificate validation
                    // For testing purposes, accept all certificates
                    return true;
                }
            };
            using var client = new HttpClient(handler);
            var response = client.GetAsync(_STOP_SERVER_URL).GetAwaiter().GetResult();
            Debug.Log("Server stop response: " + response.StatusCode);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to stop server: " + e.Message);
        }
    }
    
    [MenuItem("Build/Build Linux Server")]
    public static void BuildLinuxServer()
    {
        try
        {
            if (Directory.Exists(_BUILD_PATH)) Directory.Delete(_BUILD_PATH, true);
            string[] scenes = GetEnabledScenes();
            if (scenes.Length == 0)
            {
                Debug.LogError("No scenes are enabled in the Build Settings.");
                return;
            }

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] {SceneManager.GetSceneByBuildIndex(0).path},
                locationPathName = Path.Combine(_BUILD_PATH, "Build/LinuxServer", _BUILD_NAME),
                target = BuildTarget.StandaloneLinux64,
                options = BuildOptions.None,
                extraScriptingDefines = new[] {"UNITY_SERVER"}
            };

            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            var summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log("Build succeeded: " + summary.totalSize + " bytes. Zipping folder.");
                var zipPath = Path.Combine("C:/Users/maros/Desktop/NeonRaven", $"{_BUILD_NAME}.x86_64.zip");
                if (File.Exists(zipPath)) File.Delete(zipPath);
                ZipFile.CreateFromDirectory(Path.Combine(_BUILD_PATH, "Build/LinuxServer"), zipPath);
            }
            else if (summary.result == BuildResult.Failed)
            {
                Debug.LogError("Build failed");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Build failed: " + e.Message);
        }
        finally
        {
            var zipPath = Path.Combine("C:/Users/maros/Desktop/NeonRaven", $"{_BUILD_NAME}.x86_64.zip");
            PostBuildToServer(zipPath).GetAwaiter();
        }
    }

    private static async Task PostBuildToServer(string zipPath)
    {
        try
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) =>
                {
                    // Custom logic for certificate validation
                    // For testing purposes, accept all certificates
                    return true;
                }
            };
            using var client = new HttpClient(handler);

            var content = new MultipartFormDataContent();
            var bytes = await File.ReadAllBytesAsync(zipPath);
            var fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            content.Add(fileContent, "file", $"{_BUILD_NAME}.x86_64.zip");

            Debug.Log("Attempting to ship to server...");

            var response = await client.PostAsync(_POST_REQUEST_URL, content);
            Debug.Log("Server response status: " + response.StatusCode);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to post build to server: " + e.Message);
        }
        finally
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        }
    }

    private static string[] GetEnabledScenes()
    {
        var scenes = new string[EditorBuildSettings.scenes.Length];
        for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
        {
            scenes[i] = EditorBuildSettings.scenes[i].path;
        }

        return scenes;
    }
}