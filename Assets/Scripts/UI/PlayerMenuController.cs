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
    [SerializeField] private float menuYRotationOffset = 60f;
    
    private void OnEnable()
    {
        if (!player) player = FindFirstObjectByType<CharacterController>().gameObject;
        transform.rotation = Quaternion.Euler(0, Camera.main.transform.rotation.eulerAngles.y + menuYRotationOffset, 0);
        ShowMenu();
    }

    private void ShowMenu()
    {
        var cameraPosition = Camera.main.transform.position;
        var cameraForward = Camera.main.transform.forward;
        
        var rotationOffset = Quaternion.Euler(0, menuYRotationOffset, 0);
        var rotatedForward = rotationOffset * cameraForward;

        // can we raycast the ground layer, and offset the target by it's height, snapping it to the floor??
        Vector3 targetPosition = cameraPosition + new Vector3(rotatedForward.x, 0, rotatedForward.z).normalized * hologramDistance;
        
        if (Physics.Raycast(targetPosition + Vector3.up * 10, Vector3.down, out var hit, Mathf.Infinity, LayerMask.GetMask("Ground")))
        {
            targetPosition.y = hit.point.y + heightOffset;
        }
        else
        {
            targetPosition.y = player.transform.position.y + heightOffset;
        }
        transform.localScale = Vector3.zero;
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0;

        // will this make the vr headset lag?
        transform.position = targetPosition;
        transform.DOMove(targetPosition, animationDuration).SetEase(Ease.OutBack);
        transform.DOScale(Vector3.one, animationDuration).SetEase(Ease.OutBack);
        canvasGroup.DOFade(1, animationDuration);
    }

    public void HideMenu()
    {
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        transform.DOScale(Vector3.zero, animationDuration).SetEase(Ease.InBack);
        canvasGroup.DOFade(0, animationDuration).OnComplete(() =>
        {
            pedestal.SetActive(false);
            gameObject.SetActive(false);
        });
    }
}