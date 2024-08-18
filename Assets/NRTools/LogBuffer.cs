using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class LogBuffer : MonoBehaviour
{
    private Queue<string> logBuffer;
    [SerializeField] private int bufferSize = 30; // Adjust the buffer size as needed

    private void Awake()
    {
        logBuffer = new Queue<string>(bufferSize);
    }

    public void AppendLog(string logEntry)
    {
        if (logBuffer.Count >= bufferSize)
        {
            logBuffer.Dequeue(); // Remove oldest log
        }
        logBuffer.Enqueue(logEntry); // Add new log
    }

    public string ExportLogs()
    {
        return JsonConvert.SerializeObject(logBuffer, Formatting.Indented);
    }

    public string CaptureState(object targetObject)
    {
        var str = JsonUtility.ToJson(targetObject, true);
        if (targetObject is NinjaAgent agent)
        {
            str += "\n\n";
            str += "Target Utility: " + JsonUtility.ToJson(agent.targetUtility, true);
            str += "\n";
            str += "Pickup Utility: " + JsonUtility.ToJson(agent._pickUpUtility, true);
            str += "\n";
            str += "Right Ball Index: " + JsonUtility.ToJson(agent.rightBallIndex, true);
            str += "\n";
            str += "Left Ball Index: " + JsonUtility.ToJson(agent.leftBallIndex, true);
            str += "\n";
        }

        return str;
        return JsonConvert.SerializeObject(targetObject, Formatting.Indented,
             new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore, // Include null values to avoid null exceptions
                Formatting = Formatting.Indented
            });
    }

    public Queue<string> GetLogs()
    {
        return logBuffer;
    }
}