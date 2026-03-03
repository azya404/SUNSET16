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
    public class ComputerInteractionOld : MonoBehaviour, IInteractable
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
            if (isOverlayActive)
            {
                Debug.LogWarning("[COMPUTER] Computer overlay already active");
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
            if (computerOverlayCanvas != null)
            {
                computerOverlayCanvas.SetActive(true);
                isOverlayActive = true;
                StartConvo();
                Debug.Log("[COMPUTER] Computer overlay shown");
            }
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
        
        public void StartConvo()
        {
            DialogueManager.Instance.OnUIOpen();
            Dialogue dialogue = DialogueManager.Instance.SelectDialogue();
            int id = DialogueManager.Instance.GetCurrentID();
            bool start = DialogueManager.Instance.ChatStarted();
            if (!start)
            {
                DialogueManager.Instance.StartDialogue(id);
            }
        }
    }
}
