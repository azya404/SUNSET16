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
            interactable = GetComponent<IInteractable>();
            if (interactable == null)
            {
                Debug.LogError($"[INTERACTIONSYSTEM] {gameObject.name} requires IInteractable component!");
                enabled = false;
                return;
            }
            triggerCollider = GetComponent<Collider2D>();
            if (!triggerCollider.isTrigger)
            {
                Debug.LogWarning($"[INTERACTIONSYSTEM] {gameObject.name}: Collider2D should be a trigger! Auto-fixing...");
                triggerCollider.isTrigger = true;
            }
            if (promptCanvas != null)
            {
                promptCanvas.SetActive(false);
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

                Interact();
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
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
