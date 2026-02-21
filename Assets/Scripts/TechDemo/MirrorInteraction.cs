/*
the pill choice mirror - most important interactable in the bedroom scene
player walks up, presses E, and gets a UI overlay with Take Pill / Hide Pill buttons

rn in the tech demo this just swaps the music track and doesnt actually
tell PillStateManager anything - so the choice has no real game effect yet
in the real game the buttons need to call PillStateManager.Instance.TakePill()
which would then trigger the whole cascade of visual/audio/difficulty changes

also has its own AudioSource instead of going through AudioManager
which is a bit janky but fine for demo purposes

follows the same overlay pattern as ComputerInteraction:
open -> lock player + disable interaction + show canvas
close -> hide canvas + unlock player + re-enable interaction
*/
using UnityEngine;
using SUNSET16.Core;

namespace SUNSET16.TechDemo
{
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
            //hide it on startup
            if (mirrorOverlayCanvas != null)
            {
                mirrorOverlayCanvas.SetActive(false);
            }

            //grab our InteractionSystem so we can disable it during the overlay
            interactionSystem = GetComponent<InteractionSystem>();
        }

        public void Interact()
        {
            if (isOverlayActive)
            {
                Debug.LogWarning("[MIRROR] Mirror overlay already active");
                return;
            }

            //stop the player from mashing E while overlay is up
            if (interactionSystem != null)
            {
                interactionSystem.SetInteractionEnabled(false);
            }

            //freeze the player in place while theyre looking at the mirror
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.LockMovement(true);
            }

            //show the pill choice UI
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

        //wired to the Take Pill button via OnClick in the Inspector
        public void OnTakePillClicked()
        {
            PlayMusic(onPillMusic);
            CloseOverlay();
        }

        //wired to the Hide Pill button via OnClick
        public void OnHidePillClicked()
        {
            PlayMusic(offPillMusic);
            CloseOverlay();
        }

        //just closes without changing music (the X button basically)
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

            //kill the overlay
            if (mirrorOverlayCanvas != null)
            {
                mirrorOverlayCanvas.SetActive(false);
            }

            //let them walk again
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.LockMovement(false);
            }

            //turn interaction back on
            if (interactionSystem != null)
            {
                interactionSystem.SetInteractionEnabled(true);
            }

            isOverlayActive = false;
            Debug.Log("[MIRROR] Mirror overlay closed");
        }
    }
}
