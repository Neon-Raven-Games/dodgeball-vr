using System.IO;
using UnityEngine;

public class FileUtil : MonoBehaviour
{
    public static string GetScriptParentPath(string script)
    {
        var directoryPath =
            Directory.GetFiles(Application.dataPath, script, SearchOption.AllDirectories);
        if (directoryPath.Length == 0) Debug.LogError($"Could not find {script} in project.");

        // yucky
        var scriptPath = directoryPath[0].Replace(script, "").Replace("\\", "/");
        scriptPath = scriptPath.Substring(scriptPath.LastIndexOf("/Assets"),
            scriptPath.Length - scriptPath.LastIndexOf("/Assets"));
        scriptPath = scriptPath.Substring(1, scriptPath.Length - 2);
        scriptPath = scriptPath.Substring(0, scriptPath.LastIndexOf("/"));
        return scriptPath;
    }

}
