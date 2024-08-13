using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    public void LoadScene(int sceneIndex)
    {
        SceneManager.LoadScene(sceneBuildIndex: sceneIndex);
    }
    
    public void LoadSinglePlayerMatch()
    {
        SceneManager.LoadScene(sceneBuildIndex: 1);
    }

    public void LoadMultiplayerMatch()
    {
        SceneManager.LoadScene(sceneBuildIndex: 2);
    }

    public void GoBackToLobby()
    {
        SceneManager.LoadScene(sceneBuildIndex: 0);
    }
}