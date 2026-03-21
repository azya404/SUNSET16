using UnityEngine;

/// <summary>
/// Assigns Camera.main to this Canvas at runtime.
/// Required when the Canvas is Screen Space - Camera mode and the
/// main camera lives in a different scene (CoreScene).
/// Attach to ComputerCanvas GO.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class CanvasCameraAssigner : MonoBehaviour
{
    [SerializeField] private float planeDistance = 1f;

    private void Start()
    {
        Canvas canvas = GetComponent<Canvas>();
        canvas.worldCamera = Camera.main;
        canvas.planeDistance = planeDistance;
    }
}
