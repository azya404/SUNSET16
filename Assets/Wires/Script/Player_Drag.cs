using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;

public class Player_Drag : MonoBehaviour
{
    private bool dragging;

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
        dragging = true;
    }

    private void Update()
    {
        if (!dragging) return;

        // move the drag icon
        rect.anchoredPosition = MouseToAnchoredPos();

        // ALWAYS update which slot is under the cursor (no missed enters)
        currentHoverSlot = RaycastSlotUnderMouse();
    }

    public void end()
    {
        dragging = false;
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




// {
//     private bool dragging;

//     private RectTransform rect;
//     private Canvas rootCanvas;
//     private Vector2 startAnchoredPos;

//     private void Awake()
//     {
//         rect = GetComponent<RectTransform>();
//         rootCanvas = GetComponentInParent<Canvas>();

//         if (rect == null)
//             Debug.LogError("Player_Drag: No RectTransform found (this must be a UI object).");

//         if (rootCanvas == null)
//             Debug.LogError("Player_Drag: No Canvas parent found.");
//     }

//     private void Start()
//     {
//         // Don't disable the whole GameObject if you want the script to exist;
//         // but it's okay if OTHER scripts call begin() to enable it.
//         gameObject.SetActive(false);
//     }

//     public void begin()
//     {
//         gameObject.SetActive(true);

//         startAnchoredPos = rect.anchoredPosition;

//         // snap to cursor immediately
//         rect.anchoredPosition = MouseToAnchoredPos();
//         dragging = true;
//     }

//     private void Update()
//     {
//         if (!dragging) return;
//         rect.anchoredPosition = MouseToAnchoredPos();
//     }

//     public void end()
//     {
//         dragging = false;
//         rect.anchoredPosition = startAnchoredPos;
//         gameObject.SetActive(false);
//     }

//     private Vector2 MouseToAnchoredPos()
//     {
//         // Works for Screen Space - Overlay and Screen Space - Camera canvases
//         RectTransform canvasRect = rootCanvas.transform as RectTransform;

//         Vector2 localPoint;
//         Camera eventCam = (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : rootCanvas.worldCamera;

//         RectTransformUtility.ScreenPointToLocalPointInRectangle(
//             canvasRect,
//             Input.mousePosition,
//             eventCam,
//             out localPoint
//         );

//         return localPoint;
//     }

//     private void OnDisable()
//     {
//         dragging = false;
//     }
// }




// {
//     private bool dragging;
//     private Vector3 startPos;
//     private Camera cam;

//     private void Awake()
//     {
//         cam = Camera.main;
//         if (cam == null)
//             Debug.LogError("Player_Drag: No MainCamera found. Tag your camera as MainCamera.");
//     }

//     private void Start()
//     {
//         gameObject.SetActive(false);
//     }

//     // Convert mouse to world point ON the plane where this object lives (same Z)
//     private Vector3 MouseWorldOnObjectPlane()
//     {
//         if (cam == null) return transform.position;

//         Ray ray = cam.ScreenPointToRay(Input.mousePosition);

//         // Plane parallel to the camera view, at the object's Z
//         Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, startPos.z));

//         if (plane.Raycast(ray, out float enter))
//         {
//             Vector3 hit = ray.GetPoint(enter);
//             hit.z = startPos.z; // lock Z exactly
//             return hit;
//         }

//         return transform.position;
//     }

//     public void begin()
//     {
//         if (cam == null) return;

//         gameObject.SetActive(true);
//         startPos = transform.position;

//         // snap immediately
//         transform.position = MouseWorldOnObjectPlane();
//         dragging = true;
//     }

//     private void Update()
//     {
//         if (!dragging) return;
//         transform.position = MouseWorldOnObjectPlane();
//     }

//     public void end()
//     {
//         dragging = false;
//         transform.position = startPos;
//         gameObject.SetActive(false);
//     }

//     private void OnDisable()
//     {
//         dragging = false; // safety if object gets disabled mid-drag
//     }
// }
// {
//     private bool dragging;
//     private Vector3 startPos;

//     private Camera cam;

//     private void Awake()
//     {
//         cam = Camera.main;
//         if (cam == null)
//         {
//             Debug.LogError("Player_Drag: No MainCamera found. Tag your camera as MainCamera.");
//         }
//     }

//     private void Start()
//     {
//         gameObject.SetActive(false);
//     }

//     // 2D-friendly mouse world position: use camera ScreenToWorldPoint and force Z
//     private Vector3 MouseWorld2D()
//     {
//         if (cam == null) return transform.position;

//         Vector3 mouse = cam.ScreenToWorldPoint(Input.mousePosition);

//         // Keep the object on the same Z it started at (usually 0 in 2D)
//         mouse.z = startPos.z;

//         return mouse;
//     }

//     // Call this from your other script when right click starts (or whatever)
//     public void begin()
//     {
//         Debug.Log("BEGIN called on: " + name);

//         if (cam == null)
//         {
//             Debug.LogError("Player_Drag.begin(): cam is null (MainCamera not found).");
//             return;
//         }

//         gameObject.SetActive(true);

//         startPos = transform.position;

//         // Snap EXACTLY to cursor tip immediately
//         transform.position = MouseWorld2D();

//         dragging = true;
//     }

//     private void Update()
//     {
//         if (!dragging) return;

//         // Follow EXACTLY on cursor tip
//         transform.position = MouseWorld2D();
//     }

//     // Call this from your other script when right click ends (or whatever)
//     public void end()
//     {
//         Debug.Log("END called on: " + name);

//         dragging = false;

//         // Reset back to where it started
//         transform.position = startPos;

//         gameObject.SetActive(false);
//     }
//}


// {
//     private bool dragging = false;
//     private Vector3 start_position;
//     private Vector3 offset;
    
//     // Start is called before the first frame update

//     // Update is called once per frame
//     void Update()
//     {
//         if (dragging)
//         {
//             transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
//         }
    
//     }
//     public void begin()
//     {
//         gameObject.SetActive(true);
//         start_position = transform.position;
//         offset =  transform.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);
//         dragging = true;
//     }

//     public void end()
//     {
//         dragging = false;
//         transform.position = start_position;
//         gameObject.SetActive(false);
//     }

//     private void Start() {
//         gameObject.SetActive(false);
//     }

    
// }
