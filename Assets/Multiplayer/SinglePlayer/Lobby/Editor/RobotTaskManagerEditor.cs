#if UNITY_EDITOR
namespace Hands.SinglePlayer.Lobby.Editor
{
    using UnityEditor;
    using UnityEngine;
    
    [CustomEditor(typeof(RobotTaskManager))]
    public class RobotTaskManagerEditor : Editor
    {
          private const float _HANDLE_SIZE = 0.5f;

        private void OnEnable()
        {
            var aiController = GetTaskManager();
            UpdateWayPoints(aiController);
            Undo.undoRedoEvent += OnUndoRedo;
        }

        private void OnUndoRedo(in UndoRedoInfo undo)
        {
            var aiController = GetTaskManager();
            UpdateWayPoints(aiController);
            SceneView.RepaintAll();
            if (target) EditorUtility.SetDirty(target);
        }

        private void OnDisable() =>
            Undo.undoRedoEvent -= OnUndoRedo;

        public override void OnInspectorGUI()
        {

            var taskManager = GetTaskManager();
            if (taskManager.waypoints == null)
            {
                EditorGUILayout.HelpBox("Node collection transform is not set.", MessageType.Warning);
            }
            else
            {
                if (GUILayout.Button("Add Node at Character Position"))
                {
                    AddNodeAtCharacterPosition(taskManager);
                }
            }
            
            base.OnInspectorGUI(); // Draw the default inspector
        }
        
        private void AddNodeAtCharacterPosition(RobotTaskManager taskManager)
        {
            // Ensure there's a nodeCollection to parent the new node under
            if (taskManager.waypointParent == null)
            {
                Debug.LogError("waypoint parent is not assigned on the robot task manager.");
                return;
            }

            Undo.SetCurrentGroupName("Add Node");

            // Create a new GameObject as the node
            GameObject newNode = new GameObject("Node " + taskManager.waypointParent.childCount);
            newNode.transform.position = taskManager.transform.position; // Position it at the character's location
            newNode.transform.parent = taskManager.waypointParent; // Set the new node's parent

            // Register the undo operation
            Undo.RegisterCreatedObjectUndo(newNode, "Create Node");

            // Refresh the stalkerTravelPoints list to include all children of nodeCollection
            UpdateWayPoints(taskManager);

            // Mark the AIController as dirty so the editor knows to save the changes
            EditorUtility.SetDirty(taskManager);
        }

        private void UpdateWayPoints(RobotTaskManager taskManager)
        {
            // Clear the existing list
            taskManager.waypoints.Clear();

            // Populate the list with children of nodeCollection
            foreach (Transform child in taskManager.waypointParent)
            {
                taskManager.waypoints.Add(child);
            }
        }

        private void OnSceneGUI()
        {
            var taskManager = GetTaskManager();
            if (taskManager.waypoints is not {Count: > 1}) return;
            
            var currentEvent = SetProperties(out var labelStyle);
            if (AddingNode(taskManager, currentEvent)) return;
            DrawNodes(taskManager, labelStyle, currentEvent);
        }

        private static Event SetProperties(out GUIStyle labelStyle)
        {
            var currentEvent = Event.current;
            labelStyle = NumberLabelProperties(Color.magenta, 20);
            return currentEvent;
        }

        private static void DrawNodes(RobotTaskManager taskManager, GUIStyle labelStyle, Event currentEvent)
        {
            for (var i = 0; i < taskManager.waypoints.Count; i++)
            {
                if (taskManager.waypoints[i] == null)
                    continue;

                // order of importance:draw handle, draw line, draw sphere, draw label
                var newPoint = DrawTransformHandles(taskManager, i);
                if (EditorGUI.EndChangeCheck()) UpdateNodePosition(taskManager, i, newPoint);

                DrawNodeConnectionLine(i, taskManager);
                DrawSphere(taskManager, i);
                DrawLabel(taskManager, i, labelStyle);
                if (DeletingNode(taskManager, i, currentEvent)) break;
            }
        }

        private bool AddingNode(RobotTaskManager taskManager, Event currentEvent)
        {
            // can we register a ctrl + click and create a new node here?
            if (currentEvent.type != EventType.MouseDown || currentEvent.button != 0) return false;
            if (!Event.current.control) return false;
            currentEvent.Use();
            
            // can we create the node at the mouse position?
            var ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
            if (!Physics.Raycast(ray, out var hit)) return false;
            var newNode = new GameObject("Node " + taskManager.waypointParent.childCount);
            newNode.transform.position = hit.point;
            newNode.transform.parent = taskManager.waypointParent;
            Undo.RegisterCreatedObjectUndo(newNode, "Create Node");
            UpdateWayPoints(taskManager);
            EditorUtility.SetDirty(taskManager);
            
            return true;
        }

        private static bool DeletingNode(RobotTaskManager taskManager, int i, Event currentEvent)
        {
            var pointPosition = taskManager.waypoints[i].position;
            
            if (HandleUtility.DistanceToCircle(pointPosition, _HANDLE_SIZE) >= 1f) return false;
            if (currentEvent.type != EventType.MouseDown || currentEvent.button != 1) return false; // Right-click
            
            currentEvent.Use();
            
            var delete = DeleteDialogue(i);
            if (!delete) return false;
            
            Undo.DestroyObjectImmediate(taskManager.waypoints[i].gameObject);
            taskManager.waypoints.RemoveAt(i);
            EditorUtility.SetDirty(taskManager);

            return true;
        }

        private static bool DeleteDialogue(int i)
        {
            return EditorUtility.DisplayDialog("Delete Node?", $"Are you sure you want to delete node {i}?",
                "Yes", "No");
        }

        private static void DrawSphere(RobotTaskManager taskManager, int i)
        {
            Vector3 position = taskManager.waypoints[i].position;
            float handleSize = HandleUtility.GetHandleSize(position) * 0.1f;
            Handles.color = Color.green;
            Handles.SphereHandleCap(0, position, Quaternion.identity, handleSize, EventType.Repaint);
        }

        private RobotTaskManager GetTaskManager()
        {
            RobotTaskManager taskManager = (RobotTaskManager) target;
            return taskManager;
        }

        private static void DrawNodeConnectionLine(int i, RobotTaskManager taskManager)
        {
            Handles.color = Color.green;
            if (i < taskManager.waypoints.Count - 1)
            {
                Handles.DrawLine(taskManager.waypoints[i].position,
                    taskManager.waypoints[i + 1].position);
            }
            // Handles.DrawLine(taskManager.waypoints[^1].position, taskManager.waypoints[0].position);
        }

        private static void UpdateNodePosition(RobotTaskManager taskManager, int i, Vector3 newPoint)
        {
            Undo.RecordObject(taskManager.waypoints[i], "Move Travel Point");
            taskManager.waypoints[i].position = newPoint;
            EditorUtility.SetDirty(taskManager.waypoints[i]);
        }

        private static Vector3 DrawTransformHandles(RobotTaskManager taskManager, int i)
        {
            EditorGUI.BeginChangeCheck();
            var oldPoint = taskManager.waypoints[i].position + Vector3.forward * 0.5f;
            var newPoint = Handles.DoPositionHandle(oldPoint, Quaternion.identity);
            return newPoint;
        }

        private static GUIStyle NumberLabelProperties(Color textColor, int size)
        {
            var labelStyle = new GUIStyle
            {
                normal = {textColor = textColor},
                fontSize = size,
                alignment = TextAnchor.UpperCenter
            };
            return labelStyle;
        }

        private static void DrawLabel(RobotTaskManager taskManager, int i, GUIStyle labelStyle)
        {
            Vector3 labelPosition = taskManager.waypoints[i].position + Vector3.up * 0.5f;
            Handles.Label(labelPosition, $"{i}", labelStyle);
        }
    }
}
#endif