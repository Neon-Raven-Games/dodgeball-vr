using TMPro;
using UnityEngine;

public class BodyPage : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI headerTitle;
    [SerializeField] private string title;
    [SerializeField] private GameObject[] pageSpecificObjects;

    private void OnEnable()
    {
        headerTitle.text = title;
        foreach (var obj in pageSpecificObjects) obj.SetActive(true);
    }
    
    private void OnDisable()
    {
        foreach (var obj in pageSpecificObjects) obj.SetActive(false);
    }
}
