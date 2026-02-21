/*
quick helper that points the camera at the player and figures out bounds
so it doesnt scroll past the edges of the background

has two modes:
- auto-detect: looks at the background sprite and does math to figure out
  how far the camera can move without showing empty space outside the bg
  theres a safety check in case the bg is smaller than the camera view
- manual: fallback where you just punch in the numbers yourself

the 0.1 second delay on Start is cos PlayerController might not be
ready yet when this script runs (timing stuff)

this is tech demo only - real rooms will handle their own camera bounds
*/
using UnityEngine;
using SUNSET16.Core;

namespace SUNSET16.TechDemo
{
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
            //wait a tick for PlayerController to wake up first
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

            //tell the camera who to follow
            CameraController.Instance.SetTarget(PlayerController.Instance.transform);
            Debug.Log("[CAMERA SETUP] Camera set to follow player");

            //figure out how far the camera can go
            if (autoDetectBounds && backgroundSprite != null)
            {
                CalculateBoundsFromSprite();
            }
            else
            {
                //no sprite to auto-detect from, just use the manual values
                CameraController.Instance.SetBounds(minX, maxX, minY, maxY);
                Debug.Log($"[CAMERA SETUP] Manual bounds set: X({minX}, {maxX}), Y({minY}, {maxY})");
            }
        }

        void CalculateBoundsFromSprite()
        {
            //grab the sprites world-space bounds
            Bounds spriteBounds = backgroundSprite.bounds;

            //work out how much screen space the camera covers in world units
            Camera cam = Camera.main;
            float cameraHeight = cam.orthographicSize * 2f;
            float cameraWidth = cameraHeight * cam.aspect;

            //shrink the bounds inward by half the camera size so we never see past the edges
            float calculatedMinX = spriteBounds.min.x + (cameraWidth / 2f);
            float calculatedMaxX = spriteBounds.max.x - (cameraWidth / 2f);
            float calculatedMinY = spriteBounds.min.y + (cameraHeight / 2f);
            float calculatedMaxY = spriteBounds.max.y - (cameraHeight / 2f);

            //if the bg is tiny and min > max, just center the camera (cant scroll at all)
            if (calculatedMinX > calculatedMaxX) calculatedMinX = calculatedMaxX = (calculatedMinX + calculatedMaxX) / 2f;
            if (calculatedMinY > calculatedMaxY) calculatedMinY = calculatedMaxY = (calculatedMinY + calculatedMaxY) / 2f;

            CameraController.Instance.SetBounds(calculatedMinX, calculatedMaxX, calculatedMinY, calculatedMaxY);
            Debug.Log($"[CAMERA SETUP] Auto-detected bounds from sprite: X({calculatedMinX:F2}, {calculatedMaxX:F2}), Y({calculatedMinY:F2}, {calculatedMaxY:F2})");
        }
    }
}
