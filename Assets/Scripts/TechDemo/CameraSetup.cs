using UnityEngine;
using SUNSET16.Core;

namespace SUNSET16.TechDemo
{
    /// <summary>
    /// Auto-setup camera to follow player in tech demo scene.
    /// Calculates bounds based on background sprite or manual settings.
    /// </summary>
    public class CameraSetup : MonoBehaviour
    {
        [Header("Bounds Settings")]
        [SerializeField] private bool autoDetectBounds = true;
        [SerializeField] private SpriteRenderer backgroundSprite; // Assign bedroom background

        [Header("Manual Bounds (if not auto-detecting)")]
        [SerializeField] private float minX = -5f;
        [SerializeField] private float maxX = 5f;
        [SerializeField] private float minY = -3f;
        [SerializeField] private float maxY = 3f;

        void Start()
        {
            // Wait a frame for PlayerController to initialize
            Invoke(nameof(SetupCamera), 0.1f);
        }

        void SetupCamera()
        {
            if (CameraController.Instance == null)
            {
                Debug.LogWarning("[CAMERA SETUP] Could not find CameraController");
                return;
            }

            if (PlayerController.Instance == null)
            {
                Debug.LogWarning("[CAMERA SETUP] Could not find PlayerController");
                return;
            }

            // Set camera target
            CameraController.Instance.SetTarget(PlayerController.Instance.transform);
            Debug.Log("[CAMERA SETUP] Camera set to follow player");

            // Calculate and set bounds
            if (autoDetectBounds && backgroundSprite != null)
            {
                CalculateBoundsFromSprite();
            }
            else
            {
                // Use manual bounds
                CameraController.Instance.SetBounds(minX, maxX, minY, maxY);
                Debug.Log($"[CAMERA SETUP] Manual bounds set: X({minX}, {maxX}), Y({minY}, {maxY})");
            }
        }

        void CalculateBoundsFromSprite()
        {
            // Get sprite bounds
            Bounds spriteBounds = backgroundSprite.bounds;

            // Get camera height/width in world units
            Camera cam = Camera.main;
            float cameraHeight = cam.orthographicSize * 2f;
            float cameraWidth = cameraHeight * cam.aspect;

            // Calculate bounds (sprite edges minus half camera size)
            float calculatedMinX = spriteBounds.min.x + (cameraWidth / 2f);
            float calculatedMaxX = spriteBounds.max.x - (cameraWidth / 2f);
            float calculatedMinY = spriteBounds.min.y + (cameraHeight / 2f);
            float calculatedMaxY = spriteBounds.max.y - (cameraHeight / 2f);

            // Ensure min is actually less than max
            if (calculatedMinX > calculatedMaxX) calculatedMinX = calculatedMaxX = (calculatedMinX + calculatedMaxX) / 2f;
            if (calculatedMinY > calculatedMaxY) calculatedMinY = calculatedMaxY = (calculatedMinY + calculatedMaxY) / 2f;

            CameraController.Instance.SetBounds(calculatedMinX, calculatedMaxX, calculatedMinY, calculatedMaxY);
            Debug.Log($"[CAMERA SETUP] Auto-detected bounds from sprite: X({calculatedMinX:F2}, {calculatedMaxX:F2}), Y({calculatedMinY:F2}, {calculatedMaxY:F2})");
        }
    }
}
