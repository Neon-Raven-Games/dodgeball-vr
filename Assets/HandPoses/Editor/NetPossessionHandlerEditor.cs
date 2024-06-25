using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(NetBallPossessionHandler))]
public class NetBallPossessionHandlerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        NetBallPossessionHandler handler = (NetBallPossessionHandler)target;

        if (GUILayout.Button("Set None"))
        {
            handler.Start();
            const BallType BALL_TYPE = BallType.None;
            handler.Editor_SetBallType(BALL_TYPE);
        }
        if (GUILayout.Button("Set Dodgeball"))
        {
            handler.Start();
            const BallType BALL_TYPE = BallType.Dodgeball;
            handler.Editor_SetBallType(BALL_TYPE);
        }
        if (GUILayout.Button("Set SpeedBall"))
        {
            handler.Start();
            const BallType BALL_TYPE = BallType.SpeedBall;
            handler.Editor_SetBallType(BALL_TYPE);
        }
    }
}