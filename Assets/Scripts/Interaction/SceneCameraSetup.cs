using UnityEngine;
using SUNSET16.Core;

namespace SUNSET16.Interaction
{
    /// <summary>
    /// Per-scene camera configuration helper.
    /// Assigns the camera target (player) and calculates clamp bounds from the room's
    /// background sprite or from manually-entered values.
    ///
    /// Drop this component on any scene's root Setup object.
    /// After a 0.1 s delay the CameraController is pointed at the PlayerController
    /// and the room bounds are applied.
    ///
    /// Replaces TechDemo/CameraSetup.cs — identical logic, proper namespace.
    /// </summary>
    public class SceneCameraSetup : MonoBehaviour
    {
        [Header("Bounds Mode")]
        [SerializeField] private bool autoDetectBounds = true;
        [Tooltip("Assign the room's background SpriteRenderer for automatic bounds.")]
        [SerializeField] private SpriteRenderer backgroundSprite;

        [Header("Manual Bounds (used when Auto Detect is off)")]
        [SerializeField] private float minX = -5f;
        [SerializeField] private float maxX =  5f;
        [SerializeField] private float minY = -3f;
        [SerializeField] private float maxY =  3f;

        private void Start()
        {
            // Small delay — lets PlayerController finish its own Awake/Start initialisation
            Invoke(nameof(ApplyCameraSetup), 0.1f);
        }

        // ─── Setup ────────────────────────────────────────────────────────────────

        private void ApplyCameraSetup()
        {
            if (CameraController.Instance == null)
            {
                Debug.LogWarning("[SCENECAMERASETUP] CameraController not found");
                return;
            }

            if (PlayerController.Instance == null)
            {
                Debug.LogWarning("[SCENECAMERASETUP] PlayerController not found");
                return;
            }

            CameraController.Instance.SetTarget(PlayerController.Instance.transform);
            Debug.Log("[SCENECAMERASETUP] Camera target set to player");

            if (autoDetectBounds && backgroundSprite != null)
                ApplyBoundsFromSprite();
            else
                ApplyManualBounds();
        }

        private void ApplyBoundsFromSprite()
        {
            Bounds b    = backgroundSprite.bounds;
            Camera cam  = Camera.main;
            float  camH = cam.orthographicSize * 2f;
            float  camW = camH * cam.aspect;

            float x0 = b.min.x + camW / 2f;
            float x1 = b.max.x - camW / 2f;
            float y0 = b.min.y + camH / 2f;
            float y1 = b.max.y - camH / 2f;

            // If the camera is wider/taller than the background, centre it
            if (x0 > x1) x0 = x1 = (x0 + x1) / 2f;
            if (y0 > y1) y0 = y1 = (y0 + y1) / 2f;

            CameraController.Instance.SetBounds(x0, x1, y0, y1);
            Debug.Log($"[SCENECAMERASETUP] Auto bounds: X({x0:F2}, {x1:F2})  Y({y0:F2}, {y1:F2})");
        }

        private void ApplyManualBounds()
        {
            CameraController.Instance.SetBounds(minX, maxX, minY, maxY);
            Debug.Log($"[SCENECAMERASETUP] Manual bounds: X({minX}, {maxX})  Y({minY}, {maxY})");
        }
    }
}
