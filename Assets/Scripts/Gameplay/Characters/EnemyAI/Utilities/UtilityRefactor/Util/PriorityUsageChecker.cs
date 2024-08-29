#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Gameplay.Characters.EnemyAI.Utilities.UtilityRefactor.Util;
using Hands.SinglePlayer.EnemyAI.Priority;
using UnityEditor;
using UnityEngine;

public class PriorityUsageChecker
{
    public static Dictionary<PriorityData, HashSet<PriorityType>> CheckPriorityDataIntegrity()
    {
        var result = new Dictionary<PriorityData, HashSet<PriorityType>>();
        var methods = GetMethodsWithUtilityLink();

        foreach (var method in methods)
        {
            var attribute = (UtilityLinkAttribute) method.GetCustomAttribute(typeof(UtilityLinkAttribute), false);
            if (attribute == null)
            {
                Debug.LogWarning($"Method {method.Name} does not have a UtilityLinkAttribute.");
                continue;
            }

            // Load the PriorityData asset associated with this method
            var priorityData = AssetDatabase.LoadAssetAtPath<PriorityData>(attribute.PriorityDataPath);
            if (priorityData == null)
            {
                Debug.LogWarning($"PriorityData not found at path: {attribute.PriorityDataPath}");
                continue;
            }

            Debug.Log($"Found PriorityData: {priorityData.name} for method {method.Name}");

            // Get the source code of the method
            var sourceCode = LoadMethodSourceCode(method);
            if (sourceCode == null)
            {
                Debug.LogWarning($"Could not load source code for method: {method.Name}");
                continue;
            }

            // Extract PriorityTypes from the method's source code
            var usedPriorityTypes = ExtractPriorityTypesFromCode(sourceCode);
            if (usedPriorityTypes.Count > 0)
            {
                Debug.Log(
                    $"Found {usedPriorityTypes.Count} PriorityTypes in method {method.Name}: {string.Join(", ", usedPriorityTypes)}");
            }

            // Add the data to the result dictionary
            if (!result.ContainsKey(priorityData))
            {
                result[priorityData] = new HashSet<PriorityType>();
            }

            foreach (var priorityType in usedPriorityTypes)
            {
                result[priorityData].Add(priorityType);
            }
        }

        Debug.Log($"Returning result with {result.Count} PriorityData entries.");
        return result;
    }

    private static MethodInfo[] GetMethodsWithUtilityLink()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .SelectMany(type =>
                type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                BindingFlags.Static))
            .Where(method => method.GetCustomAttributes(typeof(UtilityLinkAttribute), false).Length > 0)
            .ToArray();
    }

    private static string LoadMethodSourceCode(MethodInfo method)
    {
        var attribute = method.GetCustomAttribute<UtilityLinkAttribute>();
        if (attribute == null || string.IsNullOrEmpty(attribute.ScriptFileName))
        {
            Debug.LogWarning($"Method {method.Name} does not have a valid UtilityLinkAttribute with a file name.");
            return null;
        }

        // Search for the file in all directories
        var files = Directory.GetFiles(Application.dataPath, attribute.ScriptFileName, SearchOption.AllDirectories);
        if (files.Length == 0)
        {
            Debug.LogWarning($"Script file not found: {attribute.ScriptFileName}");
            return null;
        }

        if (files.Length > 1)
        {
            Debug.LogWarning(
                $"Multiple files found with the name: {attribute.ScriptFileName}. Using the first one found.");
        }

        var scriptPath = files[0];
        var content = File.ReadAllText(scriptPath);

        if (!content.Contains(method.Name))
        {
            Debug.LogWarning($"Method {method.Name} not found in file: {scriptPath}");
            return null;
        }

        // Find the method's starting point
        int methodIndex = content.IndexOf(method.Name);
        if (methodIndex == -1) return null;

        // Now let's extract the method body
        int startIndex = content.LastIndexOf("public", methodIndex); // or "private", "protected", etc.
        if (startIndex == -1) startIndex = content.LastIndexOf("float", methodIndex); // or "void", "int", etc.

        int braceOpenIndex = content.IndexOf("{", methodIndex);
        int braceCloseIndex = FindMatchingClosingBrace(content, braceOpenIndex);

        if (braceOpenIndex != -1 && braceCloseIndex != -1)
        {
            string methodBody = content.Substring(startIndex, braceCloseIndex - startIndex + 1);
            Debug.Log("Extracted method body:");
            Debug.Log(methodBody);
            return methodBody;
        }

        return null;
    }

    private static int FindMatchingClosingBrace(string content, int openIndex)
    {
        int braceLevel = 1;
        for (int i = openIndex + 1; i < content.Length; i++)
        {
            if (content[i] == '{') braceLevel++;
            if (content[i] == '}') braceLevel--;
            if (braceLevel == 0) return i;
        }

        return -1; // if no matching brace found
    }


    private static HashSet<PriorityType> ExtractPriorityTypesFromCode(string sourceCode)
    {
        var priorityTypes = new HashSet<PriorityType>();
        // Improved Regex to capture all PriorityType instances
        Regex regex = new Regex(@"\bPriorityType\.(\w+)\b");

        Debug.Log("Parsing source code:");
        Debug.Log(sourceCode);

        MatchCollection matches = regex.Matches(sourceCode);
        foreach (Match match in matches)
        {
            Debug.Log($"Match found: {match.Value}");
            if (Enum.TryParse(match.Groups[1].Value, out PriorityType priorityType))
            {
                priorityTypes.Add(priorityType);
                Debug.Log($"Parsed PriorityType: {priorityType}");
            }
            else
            {
                Debug.LogWarning($"Failed to parse PriorityType from match: {match.Value}");
            }
        }

        return priorityTypes;
    }
}

#endif