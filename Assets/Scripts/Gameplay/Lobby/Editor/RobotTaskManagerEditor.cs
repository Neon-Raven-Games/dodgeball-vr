using TPUModelerEditor;
using Unity.VisualScripting;

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
            if (taskManager.waypointParent == null)
            {
                Debug.LogError("waypoint parent is not assigned on the robot task manager.");
                return;
            }

            Undo.SetCurrentGroupName("Add Node");

            GameObject newNode = new GameObject("Node " + taskManager.waypointParent.childCount);
            newNode.transform.position = taskManager.transform.position;
            newNode.transform.parent = taskManager.waypointParent;

            Undo.RegisterCreatedObjectUndo(newNode, "Create Node");

            UpdateWayPoints(taskManager);
            EditorUtility.SetDirty(taskManager);
        }

        private void UpdateWayPoints(RobotTaskManager taskManager)
        {
            taskManager.waypoints.Clear();

            foreach (Transform child in taskManager.waypointParent)
            {
                taskManager.waypoints.Add(child);
            }
        }

        private void OnSceneGUI()
        {
            var taskManager = GetTaskManager();
            if (taskManager.waypoints is not { Count: > 1 }) return;
            if (Selection.activeGameObject != taskManager.gameObject) return;

            var currentEvent = Event.current;

            HandleInputEvents(taskManager, currentEvent);
            if (currentEvent.type == EventType.Repaint) DrawScene(taskManager);
        }

        private void HandleInputEvents(RobotTaskManager taskManager, Event currentEvent)
        {
            if (AddingNode(taskManager, currentEvent)) return;

            for (var i = 0; i < taskManager.waypoints.Count; i++)
            {
                if (taskManager.waypoints[i] == null) continue;
                if (DeletingNode(taskManager, i, currentEvent)) break;
            }

            if (DrawTransformHandlesAndCheckChanges(taskManager))
                UpdateWayPoints(taskManager);
        }

        private void DrawScene(RobotTaskManager taskManager)
        {
            DrawNodes(taskManager);
        }

        private static void DrawNodes(RobotTaskManager taskManager)
        {
            for (var i = 0; i < taskManager.waypoints.Count; i++)
            {
                if (taskManager.waypoints[i] == null) continue;

                DrawNodeConnectionLine(i, taskManager);
                DrawSphere(taskManager, i);
                DrawLabel(taskManager, i);
            }
        }

        private bool AddingNode(RobotTaskManager taskManager, Event currentEvent)
        {
            if (currentEvent.type != EventType.MouseDown || currentEvent.button != 0 || !currentEvent.control) return false;

            currentEvent.Use();
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

        private bool DeletingNode(RobotTaskManager taskManager, int i, Event currentEvent)
        {
            if (currentEvent.type != EventType.MouseDown || currentEvent.button != 1) return false;

            var pointPosition = taskManager.waypoints[i].position;
            if (HandleUtility.DistanceToCircle(pointPosition, _HANDLE_SIZE) >= 1f) return false;

            currentEvent.Use();
            if (!DeleteDialogue(i)) return false;

            Undo.DestroyObjectImmediate(taskManager.waypoints[i].gameObject);
            taskManager.waypoints.RemoveAt(i);
            EditorUtility.SetDirty(taskManager);
            return true;
        }

        private static bool DeleteDialogue(int i)
        {
            return EditorUtility.DisplayDialog("Delete Node?", $"Are you sure you want to delete node {i}?", "Yes", "No");
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
            return (RobotTaskManager)target;
        }

        private static void DrawNodeConnectionLine(int i, RobotTaskManager taskManager)
        {
            Handles.color = Color.green;
            if (i < taskManager.waypoints.Count - 1)
            {
                Handles.DrawLine(taskManager.waypoints[i].position, taskManager.waypoints[i + 1].position);
            }
            // Uncomment to draw a line connecting the last node to the first node
            // Handles.DrawLine(taskManager.waypoints[^1].position, taskManager.waypoints[0].position);
        }

        private bool DrawTransformHandlesAndCheckChanges(RobotTaskManager taskManager)
        {
            EditorGUI.BeginChangeCheck();

            for (var i = 0; i < taskManager.waypoints.Count; i++)
            {
                if (taskManager.waypoints[i] == null) continue;

                var oldPoint = taskManager.waypoints[i].position;
                var newPoint = Handles.DoPositionHandle(oldPoint, Quaternion.identity);

                if (EditorGUI.EndChangeCheck())
                {
                    UpdateNodePosition(taskManager, i, newPoint);
                    return true;
                }
            }

            return false;
        }

        private static void UpdateNodePosition(RobotTaskManager taskManager, int i, Vector3 newPoint)
        {
            Undo.RecordObject(taskManager.waypoints[i], "Move Travel Point");
            taskManager.waypoints[i].position = newPoint;
            EditorUtility.SetDirty(taskManager.waypoints[i]);
        }

        private static GUIStyle NumberLabelProperties(Color textColor, int size)
        {
            return new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = textColor },
                fontSize = size,
                alignment = TextAnchor.UpperCenter
            };
        }

        private static void DrawLabel(RobotTaskManager taskManager, int i)
        {
            Vector3 labelPosition = taskManager.waypoints[i].position + Vector3.up * 0.5f;
            Handles.Label(labelPosition, $"{i}", GUIStyles.uvToolbarIconStyle);
        }
    }
}
#endif
