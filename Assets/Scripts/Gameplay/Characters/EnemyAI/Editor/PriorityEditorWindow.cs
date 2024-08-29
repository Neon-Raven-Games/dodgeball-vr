using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Gameplay.Characters.EnemyAI.Utilities.UtilityRefactor;
using Gameplay.Characters.EnemyAI.Utilities.UtilityRefactor.UtilityCalculators;
using UnityEditor;
using UnityEngine;
using Hands.SinglePlayer.EnemyAI.Priority;
using Unity.VisualScripting;

public class PriorityEditorWindow : EditorWindow
{
    private List<PriorityData> priorityDataList = new List<PriorityData>();
    private string searchQuery = "";
    private Vector2 scrollPosition;
    private bool showSimulationOptions = false;
    private Actor actor;
    private AISimulator simulator;
    private int simulationSteps = 100;
    private HashSet<PriorityType> usedPrioritiesInCode;

    [MenuItem("Window/AI Priority Editor")]
    public static void ShowWindow()
    {
        GetWindow<PriorityEditorWindow>("AI Priority Editor");
    }

    private List<PriorityType> definedPriorityTypes;
    private Dictionary<PriorityData, HashSet<PriorityType>> usedPriorities;

    private void OnEnable()
    {
        LoadAllPriorityData();
        LoadDefinedPriorityTypes();
        usedPriorities = PriorityUsageChecker.CheckPriorityDataIntegrity();
    }


    private void LoadDefinedPriorityTypes()
    {
        definedPriorityTypes = Enum.GetValues(typeof(PriorityType)).Cast<PriorityType>().ToList();
    }

    private void LoadAllPriorityData()
    {
        priorityDataList.Clear();
        string[] guids = AssetDatabase.FindAssets("t:PriorityData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            PriorityData priorityData = AssetDatabase.LoadAssetAtPath<PriorityData>(path);
            priorityDataList.Add(priorityData);
        }
    }

    private void OnGUI()
    {
        // Search bar and Load button
        EditorGUILayout.BeginHorizontal();
        searchQuery = EditorGUILayout.TextField("Search", searchQuery);
        if (GUILayout.Button("Load", GUILayout.Width(60)))
        {
            LoadAllPriorityData();
        }

        EditorGUILayout.EndHorizontal();

        // Scroll view for grid layout
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

// Calculate the width of each column based on the available window width
        float windowWidth = position.width - 40; // Adjusting for padding/margins
        int columns = 3; // Number of columns
        float columnWidth = windowWidth / columns;

        int rows = Mathf.CeilToInt(priorityDataList.Count / (float) columns);

        for (int row = 0; row < rows; row++)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Width(windowWidth));

            for (int col = 0; col < columns; col++)
            {
                int index = row * columns + col;
                if (index < priorityDataList.Count)
                {
                    EditorGUILayout.BeginVertical(GUILayout.Width(columnWidth));
                    DrawPriorityDataEditor(priorityDataList[index]);
                    EditorGUILayout.EndVertical();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        // Add new PriorityData button
        if (GUILayout.Button("Add New Priority Data"))
        {
            AddNewPriorityData();
        }

        // Simulation options
        EditorGUILayout.Space();
        showSimulationOptions = EditorGUILayout.Foldout(showSimulationOptions, "Simulation Options");
        if (showSimulationOptions)
        {
            DrawSimulationOptions();
        }
    }

    private void DrawPriorityDataEditor(PriorityData priorityData)
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.LabelField(priorityData.name);
        priorityData.utilityType =
            (UtilityType) EditorGUILayout.EnumFlagsField("Utility Type", priorityData.utilityType);
        priorityData.maxValue = EditorGUILayout.FloatField("Max Value", priorityData.maxValue);
        priorityData.recative = EditorGUILayout.Toggle("Reactive", priorityData.recative);

        if (GUILayout.Button("Balance All Priorities"))
        {
            priorityData.BalancePriorities();
        }

        // Fetch used priorities for this particular PriorityData
        HashSet<PriorityType> prioritiesUsedInCode = null;
        usedPriorities.TryGetValue(priorityData, out prioritiesUsedInCode);

        foreach (PriorityType priorityType in Enum.GetValues(typeof(PriorityType)))
        {
            bool isDefinedInData = priorityData.ContainsPriority(priorityType);
            bool isUsedInCode = prioritiesUsedInCode != null && prioritiesUsedInCode.Contains(priorityType);

            var priority = priorityData.priorities.FirstOrDefault(p => p.priority == priorityType);

            EditorGUILayout.BeginHorizontal();

            if (isUsedInCode && !isDefinedInData)
            {
                GUI.color = Color.yellow;
                GUILayout.Label("!", GUILayout.Width(20));
                GUI.color = Color.white;
                GUILayout.Label(priorityType.ToString());
                GUILayout.Label("Found In Code");
                if (GUILayout.Button("Add"))
                    priorityData.priorities.Add(new Priority {priority = priorityType, score = 0});
            }

            // why are these backwards? \ (•◡•) /
            if (!isUsedInCode && isDefinedInData)
            {
                GUI.color = Color.red;
                GUILayout.Label("X");
                GUI.color = Color.white;
                GUILayout.Label(priorityType.ToString());
                GUILayout.Label("Not In Code");
                if (GUILayout.Button("Remove"))
                    priorityData.priorities.Remove(priorityData.priorities.First(p => p.priority == priorityType));
            }

            // Draw the priority slider and EnumPopup if it's defined in PriorityData
            if (isUsedInCode && isDefinedInData)
            {
                priority.priority = (PriorityType) EditorGUILayout.EnumPopup(priorityType);
                priority.score = EditorGUILayout.Slider(priority.score, 0, priorityData.maxValue);
            }


            EditorGUILayout.EndHorizontal();
        }

        // Add New Priority Button
        if (GUILayout.Button("Add New Priority"))
        {
            AddNewPriority(priorityData);
        }

        EditorUtility.SetDirty(priorityData);
        EditorGUILayout.EndVertical();
    }

    private void AddNewPriorityData()
    {
        PriorityData newPriorityData = CreateInstance<PriorityData>();
        string path = EditorUtility.SaveFilePanelInProject("Save Priority Data", "New Priority Data", "asset",
            "Save new Priority Data");
        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.CreateAsset(newPriorityData, path);
            AssetDatabase.SaveAssets();
            priorityDataList.Add(newPriorityData);
        }
    }

    private void AddNewPriority(PriorityData priorityData)
    {
        priorityData.priorities.Add(new Priority {priority = PriorityType.DistanceToBall, score = 0});
    }

    private void DrawSimulationOptions()
    {
        GUILayout.Label("AI Simulator", EditorStyles.boldLabel);

        actor = (Actor) EditorGUILayout.ObjectField("Actor", actor, typeof(Actor), true);
        simulationSteps = EditorGUILayout.IntSlider("Simulation Steps", simulationSteps, 10, 1000);

        if (GUILayout.Button("Run Simulation"))
        {
            RunSimulation();
        }

        if (simulator != null && simulator.actor == actor)
        {
            GUILayout.Label("Simulation Results", EditorStyles.boldLabel);
            Rect graphRect = GUILayoutUtility.GetRect(200, 100);
            DrawUtilityGraph(graphRect, simulator.utilityValues);
        }
    }

    private void RunSimulation()
    {
        if (simulator == null)
        {
            simulator = FindObjectOfType<AISimulator>();
            if (simulator == null) simulator = actor.AddComponent<AISimulator>();
        }

        simulator.actor = actor;
        simulator.simulationSteps = simulationSteps;
        if (priorityDataList != null)
        {
            foreach (var data in priorityDataList)
            {
                if (data.utilityType != UtilityType.None)
                {
                    if ((data.utilityType & UtilityType.Ball) != 0)
                    {
                        var calc = new PickUpUtilityCalculator();
                        calc.PriorityData = data;
                        calc.PriorityData.Initialize();
                        simulator.AddUtilityCalculator(calc);
                    }
                    else if ((data.utilityType & UtilityType.Actor) != 0)
                    {
                        var calc = new ThrowUtilityCalculator();
                        calc.PriorityData = data;
                        calc.PriorityData.Initialize();
                        simulator.AddUtilityCalculator(calc);
                    }
                    else if ((data.utilityType & UtilityType.Trajectory) != 0)
                    {
                        var dodge = new DodgeUtilityCalculator();
                        var ctch = new CatchUtilityCalculator();
                        dodge.PriorityData = data;
                        dodge.PriorityData.Initialize();
                        
                        ctch.PriorityData = data;
                        ctch.PriorityData.Initialize();
                        
                        simulator.AddUtilityCalculator(dodge);
                        simulator.AddUtilityCalculator(ctch);
                    }
                    // simulator.CreateAndAddUtilityCalculator(data.utilityType, data);
                }
            }
        }

        simulator.SimulateAI();
    }

    private void DrawUtilityGraph(Rect rect, List<float> utilityValues)
    {
        if (utilityValues == null || utilityValues.Count == 0) return;

        Handles.BeginGUI();
        Handles.color = Color.green;

        float maxUtility = utilityValues.Max();
        float width = rect.width;
        float height = rect.height;

        for (int i = 0; i < utilityValues.Count - 1; i++)
        {
            float x1 = rect.x + (i / (float) utilityValues.Count) * width;
            float x2 = rect.x + ((i + 1) / (float) utilityValues.Count) * width;
            float y1 = rect.y + (1 - utilityValues[i] / maxUtility) * height;
            float y2 = rect.y + (1 - utilityValues[i + 1] / maxUtility) * height;

            Handles.DrawLine(new Vector3(x1, y1), new Vector3(x2, y2));
        }

        Handles.EndGUI();
    }
}