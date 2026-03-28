using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;

public class Player_Drag : MonoBehaviour
{
    public bool is_dragging;

    private RectTransform rect;
    private Canvas rootCanvas;

    public Slot currentHoverSlot { get; private set; } // <- IMPORTANT

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>();
    }

    private void Start()
    {
        gameObject.SetActive(false);
    }

    public void begin()
    {
        gameObject.SetActive(true);
        is_dragging = true;
    }

    private void Update()
    {
        if (!is_dragging) return;

        // move the drag icon
        rect.anchoredPosition = MouseToAnchoredPos();

        // ALWAYS update which slot is under the cursor (no missed enters)
        currentHoverSlot = RaycastSlotUnderMouse();
    }

    public void end()
    {
        is_dragging = false;
        currentHoverSlot = null;
        gameObject.SetActive(false);
    }

    private Vector2 MouseToAnchoredPos()
    {
        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        Camera eventCam = (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : rootCanvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, Input.mousePosition, eventCam, out Vector2 localPoint);

        return localPoint;
    }

    private Slot RaycastSlotUnderMouse()
    {
        if (EventSystem.current == null) return null;

        PointerEventData ped = new PointerEventData(EventSystem.current);
        ped.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(ped, results);

        for (int i = 0; i < results.Count; i++)
        {
            Slot s = results[i].gameObject.GetComponentInParent<Slot>();
            if (s != null) return s;
        }

        return null;
    }
}
