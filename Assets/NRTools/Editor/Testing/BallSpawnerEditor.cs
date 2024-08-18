using UnityEditor;
using UnityEngine;

namespace CloudFine.ThrowLab.Testing
{
    [CustomEditor(typeof(BallSpawner))]
    public class BallSpawnerEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            BallSpawner ballSpawner = (BallSpawner) target;
            if (GUILayout.Button("Spawn Balls"))
            {
                ballSpawner.SpawnBalls();
            }
        }

    }
}