using UnityEngine;
using DG.Tweening;
using Unity.XR.CoreUtils; // Ensure you have DoTween namespace included

public class PlayerMenuController : MonoBehaviour
{
    private GameObject player;
    [SerializeField] private float hologramDistance = 2f; // Distance in front of the player
    [SerializeField] private float animationDuration = 1f; // Duration of the animation
    [SerializeField] private GameObject pedestal;
    [SerializeField] private float heightOffset = 1.5f;

    private void OnEnable()
    {
        if (!player) player = FindFirstObjectByType<CharacterController>().gameObject;
        transform.rotation = player.transform.rotation;
        ShowMenu();
    }

    private void ShowMenu()
    {
        // Calculate the target position in front of the player
        Vector3 targetPosition = player.transform.position + player.transform.forward * hologramDistance;
        targetPosition.y = player.transform.position.y + heightOffset; // Align with player's height if necessary

        // Set the menu's initial state (scaled down and possibly transparent)
        transform.localScale = Vector3.zero;
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0;

        // Move, scale up, and fade in the menu
        transform.position = targetPosition;
        transform.DOMove(targetPosition, animationDuration).SetEase(Ease.OutBack);
        transform.DOScale(Vector3.one, animationDuration).SetEase(Ease.OutBack);
        canvasGroup.DOFade(1, animationDuration).onComplete += () =>
        {
            var pedestalPos = pedestal.transform.position;
            pedestalPos.y = player.transform.position.y;
            pedestal.transform.position = pedestalPos;
        };
    }

    public void HideMenu()
    {
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        transform.DOScale(Vector3.zero, animationDuration).SetEase(Ease.InBack);
        canvasGroup.DOFade(0, animationDuration).OnComplete(() => { gameObject.SetActive(false); });
    }
}