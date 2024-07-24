using System.Collections;
using System.Collections.Generic;
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
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
