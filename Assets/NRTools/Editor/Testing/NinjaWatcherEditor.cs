using Hands.SinglePlayer.EnemyAI.Watchers;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace CloudFine.ThrowLab.Testing
{
    [CustomEditor(typeof(NinjaWatcher))]
    public class NinjaWatcherEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var watcher = (NinjaWatcher) target;
            if (GUILayout.Button("Start Smoke Bomb"))
            {
                // watcher.SmokeBomb();
            }
        }
    }
}