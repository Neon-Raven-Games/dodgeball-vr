using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Util.Editor
{
    public class CustomTreeView : TreeView
    {
        public List<TreeViewItem> CurrentBindingItems;

        public CustomTreeView() : base(new TreeViewState(), CreateHeader())
        {
            Reload();
        }

        private static MultiColumnHeader CreateHeader()
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column {headerContent = new GUIContent("Event Type"), width = 150},
                new MultiColumnHeaderState.Column {headerContent = new GUIContent("Elapsed Time"), width = 100},
                new MultiColumnHeaderState.Column {headerContent = new GUIContent("Status"), width = 100},
                new MultiColumnHeaderState.Column {headerContent = new GUIContent("Details"), width = 250}
            };
            return new MultiColumnHeader(new MultiColumnHeaderState(columns));
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem {depth = -1};

            var children = new List<TreeViewItem>();
            var trackedEvents = GetTrackedEvents();
            int id = 0;
            foreach (var evt in trackedEvents)
            {
                var item = new CustomTreeViewItem(id++, 0, evt.TrackedItem.Type,
                    evt.TrackedItem); // Set depth to 0 for root-level items
                children.Add(item);
            }

            CurrentBindingItems = children;
            root.children = CurrentBindingItems;
            return root;
        }

        public static List<CustomTreeViewItem> GetTrackedEvents()
        {
            var trackedItems = new List<CustomTreeViewItem>();
            int id = 1; // Start from 1 since 0 is reserved for root

            foreach (var timerEntry in TimerManager.Timers)
            {
                float remainingTime = timerEntry.Key - Time.time;

                foreach (var action in timerEntry.Value)
                {
                    string methodName = action.Method.Name;
                    string targetName = action.Target != null ? action.Target.ToString() : "null";
                    string status = remainingTime > 0 ? "Active" : "Completed";
                    string details = $"Target: {targetName}";

                    var trackedItem =
                        new TimerManagerEditor.TrackedItem(methodName, remainingTime, status, details, action.Target);
                    var treeViewItem = new CustomTreeViewItem(id++, 0, methodName, trackedItem);

                    trackedItems.Add(treeViewItem);
                }
            }

            return trackedItems;
        }

        public static TimerManagerEditor.TrackedItem SelectedTrackedItem { get; private set; }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = args.item as CustomTreeViewItem;
            
            for (var visibleColumnIndex = 0; visibleColumnIndex < args.GetNumVisibleColumns(); visibleColumnIndex++)
            {
                var rect = args.GetCellRect(visibleColumnIndex);
                var columnIndex = args.GetColumn(visibleColumnIndex);


                switch (columnIndex)
                {
                    case 0:
                        EditorGUI.LabelField(rect, item.TrackedItem.Type);
                        break;
                    case 1:
                        EditorGUI.LabelField(rect, item.TrackedItem.ElapsedTime.ToString("0.00"));
                        break;
                    case 2:
                        EditorGUI.LabelField(rect, item.TrackedItem.Status);
                        break;
                    case 3:
                        EditorGUI.LabelField(rect, item.TrackedItem.Details);
                        break;
                }
            }
        }
    }

    public class CustomTreeViewItem : TreeViewItem
    {
        public TimerManagerEditor.TrackedItem TrackedItem { get; }

        public CustomTreeViewItem(int id, int depth, string displayName, TimerManagerEditor.TrackedItem trackedItem)
            : base(id, depth, displayName)
        {
            this.TrackedItem = trackedItem;
        }
    }
}