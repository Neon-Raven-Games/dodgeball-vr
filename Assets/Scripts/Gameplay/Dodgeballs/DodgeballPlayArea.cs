#if UNITY_EDITOR
using UnityEditor;
#endif

using System.Collections.Generic;
using UnityEngine;

public class DodgeballPlayArea : MonoBehaviour
{
    public Transform team1PlayArea;
    public Transform team2PlayArea;
    public Transform team1OutOfBounds;
    public Transform team2OutOfBounds;

    public Color team1Color = Color.blue;
    public Color team2Color = Color.red;

    public Actor[] team1Actors;
    public Actor[] team2Actors;

    public readonly Dictionary<DodgeBall, int> dodgeBalls = new();
    
    [SerializeField] public int dodgeballCount = 4;
    
    private static DodgeballPlayArea _instance;
    public SceneActors aiSceneActors;
    public Actor boss;
    private void Awake()
    {
        if (_instance != null) Destroy(gameObject);
        _instance = this;

        aiSceneActors = new SceneActors();
        aiSceneActors.lackeys = new List<Actor>(team2Actors);
        aiSceneActors.boss = boss;
    }

    public static void RemoveDodgeball(DodgeBall ball)
    {
        if (_instance.dodgeBalls.ContainsKey(ball)) _instance.dodgeBalls.Remove(ball);
    }

    public static void AddDodgeBallToGame(DodgeBall ball)
    {
        if (!_instance) return;
        _instance.dodgeBalls.Add(ball, 0);
    }

#if UNITY_EDITOR
    private GUIStyle _team1Style;
    private void OnDrawGizmos()
    {
        _team1Style ??= new GUIStyle()
        {
            fontStyle = FontStyle.Bold,
            normal = {textColor = Color.white},
            fontSize = 20
        };

        // Draw play area for team 1
        if (team1PlayArea)
        {
            Gizmos.color = new Color(team1Color.r, team1Color.g, team1Color.b, 0.5f);
            Gizmos.DrawCube(team1PlayArea.position, team1PlayArea.localScale);
        }

        // Draw out-of-bounds area for team 1
        if (team1OutOfBounds)
        {
            var r = Mathf.Clamp01(team1Color.r - 0.2f);
            var g = Mathf.Clamp01(team1Color.g - 0.2f);
            var b = Mathf.Clamp01(team1Color.b - 0.2f);
            Gizmos.color = new Color(r, g, b, 0.2f);
            Gizmos.DrawCube(team1OutOfBounds.position, team1OutOfBounds.localScale);
        }

        // Draw play area for team 2
        if (team2PlayArea)
        {
            Gizmos.color = new Color(team2Color.r, team2Color.g, team2Color.b, 0.5f);
            Gizmos.DrawCube(team2PlayArea.position, team2PlayArea.localScale);
        }

        // Draw out-of-bounds area for team 2
        if (team2OutOfBounds)
        {
            var r = Mathf.Clamp01(team2Color.r - 0.2f);
            var g = Mathf.Clamp01(team2Color.g - 0.2f);
            var b = Mathf.Clamp01(team2Color.b - 0.2f);
            Gizmos.color = new Color(r, g, b, 0.2f);
            Gizmos.DrawCube(team2OutOfBounds.position, team2OutOfBounds.localScale);
        }

        // Draw team 1 actors
        var playerNumber = 1;
        if (team1Actors != null)
        {
            Gizmos.color = team1Color;
            _team1Style.normal.textColor = team2Color;

            foreach (var actor in team1Actors)
            {
                var str = playerNumber.ToString();
                if (actor is DodgeballAI ai) str += ": " + ai.currentState;

                var yOffset = actor.transform.position;
                yOffset.y += 2;
                Handles.Label(yOffset, str, _team1Style);
                playerNumber++;
                Gizmos.DrawWireSphere(actor.transform.position, 0.5f);
            }
        }

        playerNumber = 1;
        if (dodgeBalls != null)
        {
            var color = Color.magenta;
            color.a = 0.4f;
            Gizmos.color = color;
            _team1Style.normal.textColor = Color.white;
            foreach (var ball in dodgeBalls.Keys)
            {
                if (ball)
                {
                    var yOffset = ball.transform.position;
                    yOffset.y += 1;
                    Handles.Label(yOffset, playerNumber.ToString(), _team1Style);
                    playerNumber++;
                    Gizmos.DrawWireSphere(ball.transform.position, 0.25f);
                }
            }
        }

        playerNumber = 1;
        // Draw team 2 actors
        if (team2Actors != null)
        {
            Gizmos.color = team2Color;
            foreach (var actor in team2Actors)
            {
                var str = playerNumber.ToString();
                if (actor is DodgeballAI ai) str += ": " + ai.currentState;
                _team1Style.normal.textColor = team1Color;

                var yOffset = actor.transform.position;
                yOffset.y += 2;
                Handles.Label(yOffset, str, _team1Style);
                playerNumber++;
                Gizmos.DrawWireSphere(actor.transform.position, 0.5f);
            }
        }
    }
#endif
}