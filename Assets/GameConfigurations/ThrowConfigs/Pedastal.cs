using UnityEngine;
using UnityEngine.UI;

public class Pedastal : MonoBehaviour
{
    [SerializeField] private Button spawnButton;
    private void OnEnable()
    {
        spawnButton.interactable = false;
    }

    private void OnDisable()
    {
        spawnButton.interactable = true;
        gameObject.SetActive(false);
    }
}
