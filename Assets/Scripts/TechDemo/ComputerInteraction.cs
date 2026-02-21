using UnityEngine;
using SUNSET16.Core;

namespace SUNSET16.TechDemo
{
    /// <summary>
    /// Handles computer interaction for tech demo.
    /// Press E to interact, shows computer overlay.
    /// </summary>
    public class ComputerInteraction : MonoBehaviour, IInteractable
    {
        [Header("UI References")]
        [SerializeField] private GameObject computerOverlayCanvas;

        [Header("Settings")]
        [SerializeField] private string interactionPrompt = "Press E to use computer";

        private bool isOverlayActive = false;
        private InteractionSystem interactionSystem;

        void Awake()
        {
            // Ensure overlay is hidden at start
            if (computerOverlayCanvas != null)
            {
                computerOverlayCanvas.SetActive(false);
            }

            // Get reference to InteractionSystem
            interactionSystem = GetComponent<InteractionSystem>();
        }

        public void Interact()
        {
            // For tech demo: just log interaction, no overlay needed
            Debug.Log("[COMPUTER] Computer interacted - skipping overlay for tech demo");

            // Interaction happens instantly, no locking needed
            // Player can immediately walk away
        }

        public string GetInteractionPrompt()
        {
            return interactionPrompt;
        }

        // Called by close button OnClick event
        public void OnCloseButtonClicked()
        {
            if (!isOverlayActive) return;

            // Hide overlay
            if (computerOverlayCanvas != null)
            {
                computerOverlayCanvas.SetActive(false);
            }

            // Unlock player movement
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.LockMovement(false);
            }

            // Re-enable InteractionSystem
            if (interactionSystem != null)
            {
                interactionSystem.SetInteractionEnabled(true);
            }

            isOverlayActive = false;
            Debug.Log("[COMPUTER] Computer overlay closed");
        }
    }
}
