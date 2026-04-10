/*
this is what makes objects in the world interactable
you slap this component on a door, computer, mirror, whatever
and it handles the "walk near -> see prompt -> press E -> thing happens" flow

the cool part is it doesnt know or care WHAT its interacting with
it just looks for anything that implements IInteractable on the same GameObject
and calls Interact() on it - polymorphism does the rest
so adding a new type of interactable object = zero changes to this script

needs a Collider2D set to isTrigger on the object (RequireComponent handles that)
when the player walks into the trigger it shows the prompt, walks out it hides
also wont let you interact if PlayerController has movement locked
cos you shouldnt be opening doors mid-puzzle lol

TODO: interaction cooldown so you cant spam E
TODO: glowing outline on objects when youre in range
TODO: multiple interaction types per object (like E to open, F to examine)
*/
using UnityEngine;
using SUNSET16.UI;

namespace SUNSET16.Core
{
    [RequireComponent(typeof(Collider2D))]
    public class InteractionSystem : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [SerializeField] private KeyCode interactionKey = KeyCode.E;
        [SerializeField] private string defaultPrompt = "Press E to interact";

        [Header("Detection")]
        [SerializeField] private LayerMask playerLayer;

        private IInteractable interactable;
        private IProximityResponder proximityResponder;
        private bool playerInRange = false;
        private Collider2D triggerCollider;

        void Awake()
        {
            interactable = GetComponent<IInteractable>();
            if (interactable == null)
            {
                Debug.LogError($"[INTERACTIONSYSTEM] {gameObject.name} requires IInteractable component!");
                enabled = false;
                return;
            }
            proximityResponder = GetComponent<IProximityResponder>();
            triggerCollider = GetComponent<Collider2D>();
            if (!triggerCollider.isTrigger)
            {
                Debug.LogWarning($"[INTERACTIONSYSTEM] {gameObject.name}: Collider2D should be a trigger! Auto-fixing...");
                triggerCollider.isTrigger = true;
            }
        }

        void Update()
        {
            if (playerInRange && Input.GetKeyDown(interactionKey))
            {
                if (PlayerController.Instance != null && PlayerController.Instance.IsMovementLocked())
                {
                    Debug.Log($"[INTERACTIONSYSTEM] Input locked - cannot interact with {gameObject.name}");
                    return;
                }

                if (DOLOSManager.Instance != null && DOLOSManager.Instance.IsAnnouncementActive)
                {
                    Debug.Log($"[INTERACTIONSYSTEM] DOLOS active — interaction with {gameObject.name} blocked");
                    return;
                }

                Interact();
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!enabled) return;

            if (other.CompareTag("Player"))
            {
                playerInRange = true;
                ShowPrompt();
                proximityResponder?.OnPlayerEnterZone();
                Debug.Log($"[INTERACTIONSYSTEM] Player entered range of {gameObject.name}");
            }
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (!enabled) return;

            if (other.CompareTag("Player"))
            {
                playerInRange = false;
                HidePrompt();
                proximityResponder?.OnPlayerExitZone();
                Debug.Log($"[INTERACTIONSYSTEM] Player exited range of {gameObject.name}");
            }
        }

        void Interact()
        {
            
            if (interactable != null)
            {
                if (!interactable.GetLocked())
                    InteractionHotbarController.Instance?.ForceHide();
                interactable.Interact();
                Debug.Log($"[INTERACTIONSYSTEM] Interacted with {gameObject.name}");
            }
        }

        void ShowPrompt()
        {
            string text = interactable.GetInteractionPrompt();
            if (string.IsNullOrEmpty(text)) text = defaultPrompt;
            InteractionHotbarController.Instance?.RegisterPrompt(this, text);
        }

        void HidePrompt()
        {
            InteractionHotbarController.Instance?.UnregisterPrompt(this);
        }

        public void SetInteractionEnabled(bool enabled)
        {
            this.enabled = enabled;

            if (!enabled)
            {
                InteractionHotbarController.Instance?.UnregisterPrompt(this);
                playerInRange = false;
            }
        }

        public void UpdatePrompt(string newPrompt)
        {
            if (playerInRange)
                InteractionHotbarController.Instance?.RegisterPrompt(this, newPrompt);
        }

        public void RefreshPrompt()
        {
            if (playerInRange)
                ShowPrompt();
        }
    }
}
