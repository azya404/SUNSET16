/*
placeholder computer - rn it just logs a message when you press E on it lol
the overlay canvas and close button logic are already wired up tho
so when we actually need to show something on the computer screen
(lore entries, station logs, whatever) we just uncomment the overlay
activation in Interact() and itll work

the close button already handles unlocking the player and re-enabling
interaction so thats taken care of at least
*/
using UnityEngine;
using SUNSET16.Core;

namespace SUNSET16.TechDemo
{
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
            //hide the overlay on startup so its not just sitting there
            if (computerOverlayCanvas != null)
            {
                computerOverlayCanvas.SetActive(false);
            }

            //grab the InteractionSystem on this same GameObject
            interactionSystem = GetComponent<InteractionSystem>();
        }

        public void Interact()
        {
            //for now just log it - no overlay needed in the tech demo
            Debug.Log("[COMPUTER] Computer interacted - skipping overlay for tech demo");

            //player can just walk away, no locking needed rn
        }

        public string GetInteractionPrompt()
        {
            return interactionPrompt;
        }

        //the close button in the overlay calls this via OnClick event
        public void OnCloseButtonClicked()
        {
            if (!isOverlayActive) return;

            //kill the overlay
            if (computerOverlayCanvas != null)
            {
                computerOverlayCanvas.SetActive(false);
            }

            //let the player move again
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.LockMovement(false);
            }

            //turn interaction back on so they can press E on stuff again
            if (interactionSystem != null)
            {
                interactionSystem.SetInteractionEnabled(true);
            }

            isOverlayActive = false;
            Debug.Log("[COMPUTER] Computer overlay closed");
        }
    }
}
