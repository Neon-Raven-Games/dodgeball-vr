using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    public void LoadSinglePlayerMatch()
    {
        SceneManager.LoadScene(sceneBuildIndex: 1);
    }
    
    public void LoadMultiplayerMatch()
    {
        SceneManager.LoadScene(sceneBuildIndex: 2);
    }
}
