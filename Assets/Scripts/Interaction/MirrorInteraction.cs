/*
the bathroom mirror - press E to decide whether to take the pill or not
shows an overlay with Take Pill / Hide Pill buttons

calling PillStateManager.TakePill() is the IMPORTANT bit here - the old TechDemo
version only played music and never actually recorded the choice which was a bug

AudioManager listens to OnPillTaken and crossfades the music automatically
so we dont need to touch it here, the event chain handles it

DOLOS and dialogue blocking is handled upstream by InteractionSystem so we
dont need to guard against those, just the overlay-already-open and
already-chose-today cases

replaces TechDemo/MirrorInteraction.cs
*/
using UnityEngine;
using SUNSET16.Core;

namespace SUNSET16.Interaction
{
    public class MirrorInteraction : MonoBehaviour, IInteractable
    {
        [Header("UI References")]
        [Tooltip("Canvas or panel containing the Take Pill / Hide Pill buttons.")]
        [SerializeField] private GameObject mirrorOverlayCanvas;

        [Header("Settings")]
        [SerializeField] private string interactionPrompt = "Press E to look in mirror";

        private bool _isOverlayActive = false;

        // ─── Lifecycle ────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (mirrorOverlayCanvas != null)
                mirrorOverlayCanvas.SetActive(false);
        }

        // ─── IInteractable ────────────────────────────────────────────────────────

        public void Interact()
        {
            if (_isOverlayActive)
            {
                Debug.LogWarning("[MIRROR] Overlay already active — ignoring");
                return;
            }

            //pill already decided for today, nothing to show
            if (PillStateManager.Instance != null && PillStateManager.Instance.HasTakenPillToday())
            {
                Debug.Log("[MIRROR] Pill choice already made today — interaction skipped");
                return;
            }

            ShowOverlay();
        }

        public string GetInteractionPrompt() => interactionPrompt;

        // ─── Button Callbacks (wired via Inspector OnClick) ───────────────────────

        public void OnTakePillClicked()
        {
            //TakePill fires OnPillTaken → AudioManager crossfades to onPillMusic
            PillStateManager.Instance?.TakePill(PillChoice.Taken);
            CloseOverlay();
        }

        public void OnHidePillClicked()
        {
            //TakePill fires OnPillTaken → AudioManager crossfades to offPillMusic
            PillStateManager.Instance?.TakePill(PillChoice.NotTaken);
            CloseOverlay();
        }

        // ─── Internal ─────────────────────────────────────────────────────────────

        private void ShowOverlay()
        {
            _isOverlayActive = true;

            if (mirrorOverlayCanvas != null)
                mirrorOverlayCanvas.SetActive(true);

            if (PlayerController.Instance != null)
                PlayerController.Instance.LockMovement(true);

            Debug.Log("[MIRROR] Pill-choice overlay shown");
        }

        private void CloseOverlay()
        {
            if (!_isOverlayActive) return;

            _isOverlayActive = false;

            if (mirrorOverlayCanvas != null)
                mirrorOverlayCanvas.SetActive(false);

            if (PlayerController.Instance != null)
                PlayerController.Instance.LockMovement(false);

            Debug.Log("[MIRROR] Pill-choice overlay closed");
        }
    }
}
