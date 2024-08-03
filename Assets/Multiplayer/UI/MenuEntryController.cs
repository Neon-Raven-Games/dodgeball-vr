using CloudFine.ThrowLab;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MenuEntryController : MonoBehaviour
{
    [SerializeField] private GameObject menu;
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private LabManager labManager;
    private InputAction _menuAction;
    private VRUILaserSetup _laserSetup;

    private void Awake()
    {
        _menuAction = inputActions.FindAction("XRI RightHand/Menu");
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += SceneUnloaded;
    }

    private void OnEnable()
    {
        _menuAction.performed += MenuAction;
        _menuAction.Enable();
    }

    private void OnDisable()
    {
        _menuAction.performed -= MenuAction;
        _menuAction.Disable();
    }

    private void OnDestroy()
    {
        SceneManager.sceneUnloaded -= SceneUnloaded;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindLaserSetup();
    }

    private void SceneUnloaded(Scene scene)
    {
        _laserSetup = null;
        menu.SetActive(false);
    }

    private void FindLaserSetup()
    {
        _laserSetup = FindFirstObjectByType<VRUILaserSetup>();
        if (_laserSetup == null) return;
        
        menu.GetComponent<Canvas>().worldCamera = _laserSetup.GetComponent<Camera>();
        _laserSetup.gameObject.SetActive(false);
    }

    private void MenuAction(InputAction.CallbackContext obj)
    {
        if (menu.activeInHierarchy)
        {
            menu.GetComponent<PlayerMenuController>().HideMenu();
            if (_laserSetup != null) _laserSetup.gameObject.SetActive(false);
            if (labManager) labManager.RemoveBall();
        }
        else
        {
            menu.SetActive(true);
            if (_laserSetup != null) _laserSetup.gameObject.SetActive(true);
        }
    }
}