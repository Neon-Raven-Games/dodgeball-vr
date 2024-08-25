using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum BattlePhase
{
    Lackey,
    Boss
}
public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    
    public static int teamOneScore;
    public static int teamTwoScore;
    public BattlePhase battlePhase;

    private static Text _teamOneScoreText;
    private static Text _teamTwoScoreText;
    
    public Text teamOneScoreTextInstance;
    public Text teamTwoScoreTextInstance;
    public DodgeballPlayArea dodgeballPlayArea;
    
    public static Action<BattlePhase> onPhaseChange;
    private void Start()
    {
        if (!_instance)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        if (!dodgeballPlayArea)
            dodgeballPlayArea = FindObjectOfType<DodgeballPlayArea>();
        
        _teamOneScoreText = teamOneScoreTextInstance;
        _teamTwoScoreText = teamTwoScoreTextInstance;
        ResetScores();
    }

    public static void ChangePhase(BattlePhase phase)
    {
        _instance.battlePhase = phase;
        onPhaseChange?.Invoke(phase);
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
