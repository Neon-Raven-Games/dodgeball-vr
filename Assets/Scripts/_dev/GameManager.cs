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
    }
    public static void UpdateScore()
    {
        _teamOneScoreText.text = "Targets Hit: " + teamOneScore;
        _teamTwoScoreText.text = "Targets Hit: " + teamTwoScore;
    }
    
    public void ResetScores()
    {
        teamOneScore = 0;
        teamTwoScore = 0;
        UpdateScore();
    }
}
