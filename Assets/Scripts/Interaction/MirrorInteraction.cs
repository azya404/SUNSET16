using UnityEngine;
using SUNSET16.Core;

namespace SUNSET16.Interaction
{
    /// <summary>
    /// Bathroom mirror world-space interaction object.
    /// Press E to open the pill-choice overlay (Take / Hide pill buttons).
    ///
    /// Pill recording:
    ///   OnTakePillClicked  → PillStateManager.TakePill(PillChoice.Taken)
    ///   OnHidePillClicked  → PillStateManager.TakePill(PillChoice.NotTaken)
    ///
    ///   PillStateManager fires OnPillTaken, which AudioManager subscribes to and uses
    ///   to cross-fade to the correct music track.  MirrorInteraction does NOT call
    ///   AudioManager directly — the event chain handles it.
    ///
    /// Guards (all silently drop the interaction):
    ///   • Pill already chosen today  → log and return
    ///   • Overlay already showing    → return
    ///   (DOLOS/dialogue are blocked upstream by InteractionSystem)
    ///
    /// Replaces TechDemo/MirrorInteraction.cs — critical fix: actually records
    /// the pill choice via PillStateManager instead of only playing music.
    /// </summary>
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

            // Guard: pill already chosen today — nothing left to decide
            if (PillStateManager.Instance != null && PillStateManager.Instance.HasTakenPillToday())
            {
                Debug.Log("[MIRROR] Pill choice already made today — interaction skipped");
                return;
            }

            ShowOverlay();
        }

        public string GetInteractionPrompt() => interactionPrompt;

        // ─── Button Callbacks (wired via Inspector OnClick) ───────────────────────

        /// <summary>Player chose to take the pill.</summary>
        public void OnTakePillClicked()
        {
            // TakePill validates forced days (1-2), fires OnPillTaken →
            // AudioManager cross-fades to onPillMusic automatically.
            PillStateManager.Instance?.TakePill(PillChoice.Taken);
            CloseOverlay();
        }

        /// <summary>Player chose to hide the pill.</summary>
        public void OnHidePillClicked()
        {
            // TakePill validates forced days (1-2), fires OnPillTaken →
            // AudioManager cross-fades to offPillMusic automatically.
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
