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
    private BallBoundsHelper _ballBoundsHelper;
    private void OnEnable()
    {
        var sceneIndex = SceneManager.GetActiveScene().buildIndex;

        lobbyButton.interactable = sceneIndex != 0;
        if (sceneIndex != 0)
        {
            _ballBoundsHelper = FindFirstObjectByType<BallBoundsHelper>();
            if (_ballBoundsHelper)
            {
                dodgeballCountSlider.gameObject.SetActive(true);
                dodgeballCountText.gameObject.SetActive(true);
                dodgeballCountSlider.value = _ballBoundsHelper.ballCount;
                dodgeballCountText.text = _ballBoundsHelper.ballCount.ToString();
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
        if (!_ballBoundsHelper) return;
        _ballBoundsHelper.SetNewNumberBalls((int) value);
        dodgeballCountText.text = value.ToString();
    }
}
