/*
drop this on any scene's root setup object to point the camera at the player
and set the room clamp bounds

either auto-detects bounds from the background sprite (recommended) or falls
back to the manual min/max values if autoDetectBounds is off or no sprite assigned

fires 0.1s after Start so PlayerController has time to finish its own Awake/Start
before we try to call SetTarget on it

replaces TechDemo/CameraSetup.cs - same logic, proper namespace

TODO: smooth camera transition when entering a new room would be nice
*/
using UnityEngine;
using SUNSET16.Core;

namespace SUNSET16.Interaction
{
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
            //small delay so PlayerController finishes its own Awake/Start first
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

            //if camera is wider/taller than the background, just centre it
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
