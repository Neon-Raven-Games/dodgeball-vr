using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Mono.Cecil;
using Unity.VisualScripting;

public class ScriptReferenceFinder : EditorWindow
{
    private string _directoryPath = "Assets/Scripts"; 
    private readonly List<ScriptReference> _scriptReferences = new();

    [MenuItem("Neon Raven/Script Reference Finder")]
    public static void ShowWindow()
    {
        GetWindow<ScriptReferenceFinder>("Script Reference Finder");
    }

    private void OnGUI()
    {
        GUILayout.Label("Script Reference Finder", EditorStyles.boldLabel);
        _directoryPath = EditorGUILayout.TextField("Directory Path", _directoryPath);

        if (GUILayout.Button("Find References"))
        {
            FindReferences();
        }

        GUILayout.Space(20);

        if (_scriptReferences.Count > 0)
        {
            foreach (var scriptRef in _scriptReferences)
            {
                GUILayout.Label($"Script: {scriptRef.ScriptPath}", EditorStyles.label);
                foreach (var reference in scriptRef.References)
                {
                    GUILayout.Label($"  - {reference}", EditorStyles.label);
                }
                GUILayout.Space(10);
            }
        }
    }

    private void FindReferences()
    {
        _scriptReferences.Clear();
        var scripts = Directory.GetFiles(_directoryPath, "*.cs", SearchOption.AllDirectories);
        foreach (var script in scripts)
        {
            var references = GetScriptReferences(script);
            _scriptReferences.Add(new ScriptReference
            {
                ScriptPath = script,
                References = references
            });
        }
    }

    private List<string> GetScriptReferences(string scriptPath)
    {
        var references = new List<string>();

        string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", scriptPath));
        
        // this does not work
        var assemblyPath = CompileScriptToAssembly(fullPath);

        var externalObjectMap = AssetImporter.GetAtPath(scriptPath).GetExternalObjectMap(); 
        foreach(var obj in externalObjectMap)
        {
            Debug.Log(obj.Key + " : " + obj.Value);
        }
        if (assemblyPath != null)
        {
            var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);
            foreach (var module in assembly.Modules)
            {
                foreach (var reference in module.AssemblyReferences)
                {
                    references.Add(reference.FullName);
                }
            }
        }

        return references;
    }

    private static void BrowseTypes(string fileName)
    {
        ModuleDefinition module = ModuleDefinition.ReadModule(fileName);
        foreach (TypeDefinition type in module.Types)
        {
            if (!type.IsPublic)
                continue;

            Debug.Log(type.FullName);
        }
    }

    private string CompileScriptToAssembly(string scriptPath)
    {
        string assemblyPath = Path.ChangeExtension(scriptPath, ".dll");

        if (File.Exists(assemblyPath))
        {
            return assemblyPath;
        }

        Debug.LogError($"Assembly not found: {assemblyPath}");
        return null;
    }

    private class ScriptReference
    {
        public string ScriptPath { get; set; }
        public List<string> References { get; set; }
    }
}
