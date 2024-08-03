using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class VRUILaserSetup : MonoBehaviour
{
    private Transform anchor;
    [SerializeField] private InputActionAsset actionAsset;
    [SerializeField] private HandSide handSide;
    private Camera laserCamera; // Assign the new camera in the inspector
    private GameObject lastHitObject;
    private InputAction pointerDown;
    private EventSystem eventSystem;
    private LineRenderer _lineRenderer;
    [SerializeField] private GameObject crosshair;

    private Vector3 hitPoint; // To store the hit point for visualization

    private void Awake()
    {
        anchor = transform;
        laserCamera = GetComponent<Camera>();
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.startWidth = 0.02f;
        _lineRenderer.SetPosition(0, anchor.transform.position);
        crosshair.SetActive(false);
        pointerDown = actionAsset.FindAction(
            handSide == HandSide.RIGHT ? "XRI RightHand Interaction/Activate" : "XRI LeftHand Interaction/Activate",
            true);

        var eventSystemObject = new GameObject("EventSystem");
        eventSystem = eventSystemObject.AddComponent<EventSystem>();
    }

    private bool _click;
    private bool _pointerDown;

    private void OnEnable()
    {
        pointerDown.performed += PointerDown;
        pointerDown.canceled += PointerUp;
        pointerDown.Enable();
    }

    private void OnDisable()
    {
        pointerDown.performed -= PointerDown;
        pointerDown.canceled -= PointerUp;
        pointerDown.Disable();
        _click = false;
        _pointerDown = false;
        slider = null;
    }


    // can we make a editor inspector that calls this for testing outside vr headset??
    public void PointerDown(InputAction.CallbackContext context)
    {
        _click = true;
        _pointerDown = true;
    }

    public void PointerUp(InputAction.CallbackContext context)
    {
        _pointerDown = false;
        ExecuteEvents.ExecuteHierarchy(lastHitObject, new PointerEventData(eventSystem), ExecuteEvents.pointerUpHandler);
        eventSystem.SetSelectedGameObject(null);
        slider = null;
    }

    private void Update()
    {
        var raycastResults = new List<RaycastResult>();
        var screenPosition = laserCamera.WorldToScreenPoint(anchor.position + anchor.forward * 15);

        var pointerEventData = new PointerEventData(eventSystem) {position = screenPosition};
        eventSystem.RaycastAll(pointerEventData, raycastResults);
        pointerEventData.pointerPressRaycast = pointerEventData.pointerCurrentRaycast;

        _lineRenderer.SetPosition(0, anchor.transform.position);

        if (raycastResults.Count > 0)
        {
            var hitObject = raycastResults[0].gameObject;
            hitPoint = raycastResults[0].worldPosition;
            _lineRenderer.SetPosition(1, hitPoint);
            crosshair.transform.position = hitPoint;
            crosshair.transform.rotation = Quaternion.LookRotation(hitObject.transform.forward);


            if (!slider && lastHitObject != hitObject)
            {
                if (lastHitObject != null) ExecuteEvents.ExecuteHierarchy(lastHitObject, pointerEventData, ExecuteEvents.pointerExitHandler);

                crosshair.SetActive(true);
                ExecuteEvents.ExecuteHierarchy(hitObject, pointerEventData, ExecuteEvents.pointerEnterHandler);
                lastHitObject = hitObject;
            }
            if (_click)
            {
                ExecuteEvents.ExecuteHierarchy(hitObject, pointerEventData, ExecuteEvents.pointerClickHandler);
                var drag = ExecuteEvents.GetEventHandler<IDragHandler>(hitObject);
                if (drag)
                {
                    slider = drag.GetComponent<Slider>();
                    sliderRectTransform = drag.GetComponent<RectTransform>();
                }
                
                _click = false;
            }

            if (!_pointerDown) return;
            
            ExecuteEvents.ExecuteHierarchy(hitObject, pointerEventData, ExecuteEvents.pointerDownHandler);
            
            if (!slider) return;
                
            RectTransformUtility.ScreenPointToWorldPointInRectangle(sliderRectTransform, screenPosition, laserCamera, out var localPoint);
            Vector2 localPosition = sliderRectTransform.InverseTransformPoint(localPoint);
    
            var normalizedValue = Mathf.InverseLerp(sliderRectTransform.rect.xMin, sliderRectTransform.rect.xMax, localPosition.x);
            var mappedValue = Mathf.Lerp(slider.minValue, slider.maxValue, normalizedValue);
            if (slider.wholeNumbers) mappedValue = Mathf.Round(mappedValue);

            slider.value = mappedValue;
        }
        else
        {
            if (lastHitObject != null)
            {
                ExecuteEvents.ExecuteHierarchy(lastHitObject, pointerEventData, ExecuteEvents.pointerExitHandler);
                lastHitObject = null;
            }

            _lineRenderer.SetPosition(1, anchor.transform.position + anchor.forward * 15);
            crosshair.SetActive(false);
        }
    }

    private Slider slider;
    private RectTransform sliderRectTransform;

   

    private void OnDrawGizmos()
    {
        if (hitPoint == Vector3.zero) return;

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(hitPoint, 0.05f);
    }
}