#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using System.Collections;
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

    public DodgeballAI[] team1Actors;
    public DodgeballAI[] team2Actors;

    [SerializeField] public int dodgeballCount = 4;
    public List<GameObject> dodgeBalls = new();


    public void Initialize()
    {
        foreach (var actor in team1Actors)
        {
            foreach (var ball in dodgeBalls)
            {
                var ai = actor.GetComponent<DodgeballAI>();
                if (ai) ai.DeRegisterBall(ball.GetComponent<DodgeBall>());
            }
        }

        foreach (var actor in team2Actors)
        {
            foreach (var ball in dodgeBalls)
            {
                var ai = actor.GetComponent<DodgeballAI>();
                if (ai) ai.DeRegisterBall(ball.GetComponent<DodgeBall>());
                ball.SetActive(false);
            }
        }

        dodgeBalls.Clear();
        for (var i = 0; i < dodgeballCount; i++)
        {
            var ball = BallPool.SetBall(Vector3.zero);
            dodgeBalls.Add(ball);
        }


    }


    private void OnDrawGizmos()
    {
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
            foreach (var actor in team1Actors)
            {
                if (actor)
                {
                    var str = playerNumber.ToString();
                    var ai = actor.GetComponent<DodgeballAI>();
                    if (ai) str += ": " + ai.currentState;
#if UNITY_EDITOR
                    GUIStyle style = new GUIStyle();
                    style.fontStyle = FontStyle.Bold;
                    style.normal.textColor = team2Color;
                    style.fontSize = 20;
                    var yOffset = actor.transform.position;
                    yOffset.y += 2;
                    Handles.Label(yOffset, str, style);
                    playerNumber++;
#endif
                    Gizmos.DrawWireSphere(actor.transform.position, 0.5f);
                }
            }
        }

        playerNumber = 1;
        if (dodgeBalls != null)
        {
            var color = Color.magenta;
            color.a = 0.4f;
            Gizmos.color = color;
            foreach (var actor in dodgeBalls)
            {
                if (actor)
                {
#if UNITY_EDITOR
                    GUIStyle style = new GUIStyle();
                    style.fontStyle = FontStyle.Bold;
                    style.normal.textColor = Color.white;
                    style.fontSize = 20;
                    var yOffset = actor.transform.position;
                    yOffset.y += 1;
                    Handles.Label(yOffset, playerNumber.ToString(), style);
                    playerNumber++;
#endif
                    Gizmos.DrawWireSphere(actor.transform.position, 0.25f);
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
                if (actor)
                {
                    var str = playerNumber.ToString();
                    var ai = actor.GetComponent<DodgeballAI>();
                    if (ai) str += ": " + ai.currentState;
#if UNITY_EDITOR
                    GUIStyle style = new GUIStyle();
                    style.fontStyle = FontStyle.Bold;
                    style.normal.textColor = team1Color;
                    style.fontSize = 20;
                    var yOffset = actor.transform.position;
                    yOffset.y += 2;
                    Handles.Label(yOffset, str, style);
                    playerNumber++;
#endif
                    Gizmos.DrawWireSphere(actor.transform.position, 0.5f);
                }
            }
        }
    }
}