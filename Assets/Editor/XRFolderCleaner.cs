using UnityEditor;
using UnityEngine;
using System.IO;

// [InitializeOnLoad]
public class XRFolderCleaner
{
    static XRFolderCleaner()
    {
        // EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            string xrFolderPath = Path.Combine(Application.dataPath, "XR");
            if (Directory.Exists(xrFolderPath))
            {
                try
                {
                    Directory.Delete(xrFolderPath, true);
                    File.Delete(xrFolderPath + ".meta"); // Also delete the meta file
                    AssetDatabase.Refresh();
                    Debug.Log("Deleted unnecessary XR folder. Thanks Unity.");
                }
                catch (IOException e)
                {
                    Debug.LogError("Error deleting XR folder: " + e.Message);
                }
            }
        }
    }
}