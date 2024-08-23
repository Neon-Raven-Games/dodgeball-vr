using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneSelectionHelper : MonoBehaviour
{
    [SerializeField] private Button lobbyButton;
    [SerializeField] private Button gymButton;
    [SerializeField] private Button dojoButton;
    [SerializeField] private Slider dodgeballCountSlider;
    [SerializeField] private TextMeshProUGUI dodgeballCountText;
    private BallRespawnManager _ballRespawnManager;
    
    private void OnEnable()
    {
        var sceneIndex = SceneManager.GetActiveScene().buildIndex;

        lobbyButton.interactable = sceneIndex != 0;
        if (sceneIndex != 0)
        {
            _ballRespawnManager = FindFirstObjectByType<BallRespawnManager>();
            if (_ballRespawnManager)
            {
                dodgeballCountSlider.gameObject.SetActive(true);
                dodgeballCountText.gameObject.SetActive(true);
            }
        }
        else
        {
            dodgeballCountSlider.gameObject.SetActive(false);
            dodgeballCountText.gameObject.SetActive(false);
        }
        
        gymButton.interactable = sceneIndex != 1;
        dojoButton.interactable = sceneIndex != 2;
    }

    public void SetBalls(float value)
    {
        if (!_ballRespawnManager) return;
        _ballRespawnManager.SetNewNumberBalls((int) value);
        dodgeballCountText.text = value.ToString();
    }
}
