using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static int teamOneScore;
    public static int teamTwoScore;

    private static Text _teamOneScoreText;
    private static Text _teamTwoScoreText;

    public Text teamOneScoreTextInstance;
    public Text teamTwoScoreTextInstance;
    private void Start()
    {
        _teamOneScoreText = teamOneScoreTextInstance;
        _teamTwoScoreText = teamTwoScoreTextInstance;
        ResetScores();
    }

    public static void AddScore(Team team)
    {
        if (team == Team.TeamOne) teamOneScore++;
        else teamTwoScore++;
        UpdateScore();
    }

    private static List<GameObject> _matchBalls = new();
    private static List<Vector3> _matchBallsInitialPos = new();
    public static void InitBallForGame(GameObject ball)
    {
        _matchBalls.Add(ball);
        _matchBallsInitialPos.Add(ball.transform.position);
    }
    
    public static void ResetMatchBalls()
    {
        for (var i = 0; i < _matchBalls.Count; i++)
        {
            _matchBalls[i].transform.position = _matchBallsInitialPos[i];
            _matchBalls[i].GetComponent<Rigidbody>().velocity = Vector3.zero;
            _matchBalls[i].GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        }
    }
    
    public static void UpdateScore()
    {
        if (!_teamOneScoreText || !_teamTwoScoreText) return;
        _teamOneScoreText.text = "Player:   " + teamOneScore;
        _teamTwoScoreText.text = "Enemy:   " + teamTwoScore;
    }
    
    public void ResetScores()
    {
        teamOneScore = 0;
        teamTwoScore = 0;
        UpdateScore();
    }

    public static void RemoveBallForGame(GameObject ball)
    {
        var index = _matchBalls.IndexOf(ball);
        if (index == -1) return;
        _matchBalls.RemoveAt(index);
        _matchBallsInitialPos.RemoveAt(index);
    }
}
