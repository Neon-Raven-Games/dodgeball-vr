using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LogBuffer))]
public class LogBufferEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        LogBuffer logBuffer = (LogBuffer)target;

        if (GUILayout.Button("Export Logs to Console"))
        {
            string logsJson = logBuffer.ExportLogs();
            Debug.Log(logsJson);
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Capture AI State"))
        {
            var aiBase = target.GetComponent<NinjaAgent>();
            if (aiBase != null)
            {
                string stateJson = logBuffer.CaptureState(aiBase);
                Debug.Log(stateJson);
            }
        }
        
        if (GUILayout.Button("Export AI State and Logs"))
        {
            var aiBase = target.GetComponent<NinjaAgent>();
            if (aiBase != null)
            {
               // aiBase.Logs();
            }
        }

        GUILayout.Space(10);

        if (logBuffer != null && logBuffer.GetLogs() != null)
        {
            GUILayout.Label("Log Buffer:");
            foreach (var log in logBuffer.GetLogs())
            {
                GUILayout.Label(log);
            }
        }
    }
    /*
     logs, state, and call stack for ai
    public void Logs()
    {
        string stateJson = "Call Stack:\n";
        stateJson += logBuffer.CaptureState(this);
        string logsJson = logBuffer.ExportLogs();
        stateJson += "\n\n" + "Logs\n" + logsJson;
        Debug.Log(stateJson);
    }
    */
}