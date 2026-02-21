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
using TMPro;

namespace SUNSET16.Core
{
    [RequireComponent(typeof(Collider2D))]
    public class InteractionSystem : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [SerializeField] private KeyCode interactionKey = KeyCode.E;

        [Header("Prompt UI")]
        [SerializeField] private GameObject promptCanvas;
        [SerializeField] private TMP_Text promptText;
        [SerializeField] private string defaultPrompt = "Press E to interact";

        [Header("Detection")]
        [SerializeField] private LayerMask playerLayer;

        private IInteractable interactable;
        private bool playerInRange = false;
        private Collider2D triggerCollider;

        void Awake()
        {
            //grab whatever IInteractable is on this same object (door, computer, etc)
            interactable = GetComponent<IInteractable>();
            if (interactable == null)
            {
                Debug.LogError($"[INTERACTIONSYSTEM] {gameObject.name} requires IInteractable component!");
                enabled = false; //no point running if theres nothing to interact with
                return;
            }
            //auto-fix if someone forgot to set the collider as a trigger
            triggerCollider = GetComponent<Collider2D>();
            if (!triggerCollider.isTrigger)
            {
                Debug.LogWarning($"[INTERACTIONSYSTEM] {gameObject.name}: Collider2D should be a trigger! Auto-fixing...");
                triggerCollider.isTrigger = true;
            }
            if (promptCanvas != null)
            {
                promptCanvas.SetActive(false); //hide prompt until player walks up
            }
        }

        void Update()
        {
            if (playerInRange && Input.GetKeyDown(interactionKey))
            {
                //dont let the player interact if theyre locked (mid-puzzle, mid-task, etc)
                if (PlayerController.Instance != null && PlayerController.Instance.IsMovementLocked())
                {
                    Debug.Log($"[INTERACTIONSYSTEM] Input locked - cannot interact with {gameObject.name}");
                    return;
                }

                Interact();
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player")) //only react to the player, not random colliders
            {
                playerInRange = true;
                ShowPrompt();
                Debug.Log($"[INTERACTIONSYSTEM] Player entered range of {gameObject.name}");
            }
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                playerInRange = false;
                HidePrompt();
                Debug.Log($"[INTERACTIONSYSTEM] Player exited range of {gameObject.name}");
            }
        }

        void Interact()
        {
            if (interactable != null)
            {
                interactable.Interact();
                Debug.Log($"[INTERACTIONSYSTEM] Interacted with {gameObject.name}");
            }
        }

        void ShowPrompt()
        {
            if (promptCanvas != null)
            {
                promptCanvas.SetActive(true);
                if (promptText != null)
                {
                    //ask the interactable what its prompt should say (each one can be different)
                    string customPrompt = interactable.GetInteractionPrompt();
                    promptText.text = string.IsNullOrEmpty(customPrompt) ? defaultPrompt : customPrompt;
                }
            }
        }

        void HidePrompt()
        {
            if (promptCanvas != null)
            {
                promptCanvas.SetActive(false);
            }
        }

        //other scripts (like MirrorInteraction) call this to turn interaction on/off
        //disabling also hides the prompt and resets playerInRange so it doesnt get stuck
        public void SetInteractionEnabled(bool enabled)
        {
            this.enabled = enabled;

            if (!enabled)
            {
                HidePrompt();
                playerInRange = false;
            }
        }

        public void UpdatePrompt(string newPrompt)
        {
            if (promptText != null)
            {
                promptText.text = newPrompt;
            }
        }

        public void RefreshPrompt()
        {
            if (playerInRange)
            {
                ShowPrompt();
            }
        }
    }
}
