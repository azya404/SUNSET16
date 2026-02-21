using UnityEngine;
using SUNSET16.Core;

namespace SUNSET16.TechDemo
{
    /// <summary>
    /// Handles mirror interaction for tech demo.
    /// Press E to interact, shows overlay with choices.
    /// </summary>
    public class MirrorInteraction : MonoBehaviour, IInteractable
    {
        [Header("UI References")]
        [SerializeField] private GameObject mirrorOverlayCanvas;

        [Header("Music Settings")]
        [SerializeField] private AudioClip onPillMusic;  // Take Pill music
        [SerializeField] private AudioClip offPillMusic; // Hide Pill music
        [SerializeField] private AudioSource audioSource;

        [Header("Settings")]
        [SerializeField] private string interactionPrompt = "Press E to look in mirror";

        private bool isOverlayActive = false;
        private InteractionSystem interactionSystem;

        void Awake()
        {
            // Ensure overlay is hidden at start
            if (mirrorOverlayCanvas != null)
            {
                mirrorOverlayCanvas.SetActive(false);
            }

            // Get reference to InteractionSystem
            interactionSystem = GetComponent<InteractionSystem>();
        }

        public void Interact()
        {
            if (isOverlayActive)
            {
                Debug.LogWarning("[MIRROR] Mirror overlay already active");
                return;
            }

            // Disable InteractionSystem to prevent repeated interactions
            if (interactionSystem != null)
            {
                interactionSystem.SetInteractionEnabled(false);
            }

            // Lock player movement
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.LockMovement(true);
            }

            // Show mirror overlay
            if (mirrorOverlayCanvas != null)
            {
                mirrorOverlayCanvas.SetActive(true);
                isOverlayActive = true;
                Debug.Log("[MIRROR] Mirror overlay shown");
            }
        }

        public string GetInteractionPrompt()
        {
            return interactionPrompt;
        }

        // Called by Take Pill button
        public void OnTakePillClicked()
        {
            PlayMusic(onPillMusic);
            CloseOverlay();
        }

        // Called by Hide Pill button
        public void OnHidePillClicked()
        {
            PlayMusic(offPillMusic);
            CloseOverlay();
        }

        // Called by Close Mirror button (no music)
        public void OnButtonClicked()
        {
            CloseOverlay();
        }

        private void PlayMusic(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.clip = clip;
                audioSource.Play();
                Debug.Log($"[MIRROR] Playing music: {clip.name}");
            }
            else
            {
                Debug.LogWarning("[MIRROR] AudioSource or AudioClip is missing!");
            }
        }

        private void CloseOverlay()
        {
            if (!isOverlayActive) return;

            // Hide overlay
            if (mirrorOverlayCanvas != null)
            {
                mirrorOverlayCanvas.SetActive(false);
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
            Debug.Log("[MIRROR] Mirror overlay closed");
        }
    }
}
