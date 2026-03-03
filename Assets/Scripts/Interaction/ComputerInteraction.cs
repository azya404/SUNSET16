using UnityEngine;
using SUNSET16.Core;
using SUNSET16.UI;

namespace SUNSET16.Interaction
{
    /// <summary>
    /// Computer terminal world-space interaction object (Albert's computer).
    /// Press E to open a branching dialogue via DialogueUIManager.
    ///
    /// Sequences are assigned per day in the Inspector:
    ///   daySequences[0] = Day 1, daySequences[1] = Day 2, etc.
    ///
    /// DialogueUIManager owns movement locking and Escape handling for the
    /// duration of the conversation — ComputerInteraction does nothing beyond
    /// selecting and starting the sequence.
    ///
    /// Guards (all silently drop the interaction):
    ///   • Dialogue already active   (prevents double-open)
    ///   • DOLOS announcement active (also blocked upstream by InteractionSystem)
    /// </summary>
    public class ComputerInteraction : MonoBehaviour, IInteractable
    {
        [Header("Dialogue Sequences")]
        [Tooltip("One DialogueSequence ScriptableObject per game day (index 0 = Day 1, index 1 = Day 2, …).")]
        [SerializeField] private DialogueSequence[] daySequences;

        [Header("Settings")]
        [SerializeField] private string interactionPrompt = "Press E to use computer";

        // ─── IInteractable ────────────────────────────────────────────────────────

        public void Interact()
        {
            // Guard: another dialogue is already open
            if (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueActive)
            {
                Debug.LogWarning("[COMPUTER] Dialogue already active — ignoring interaction");
                return;
            }

            // Guard: DOLOS announcement running (defensive; also blocked by InteractionSystem)
            if (DOLOSManager.Instance != null && DOLOSManager.Instance.IsAnnouncementActive)
            {
                Debug.LogWarning("[COMPUTER] DOLOS active — ignoring interaction");
                return;
            }

            DialogueSequence sequence = GetSequenceForToday();
            if (sequence == null)
            {
                Debug.LogWarning("[COMPUTER] No dialogue sequence assigned for the current day");
                return;
            }

            if (DialogueUIManager.Instance != null)
                DialogueUIManager.Instance.ShowDialogue(sequence);
            else
                Debug.LogWarning("[COMPUTER] DialogueUIManager not found in scene");
        }

        public string GetInteractionPrompt() => interactionPrompt;

        // ─── Internal ─────────────────────────────────────────────────────────────

        private DialogueSequence GetSequenceForToday()
        {
            if (daySequences == null || daySequences.Length == 0) return null;

            // Fallback to first entry if DayManager is not yet available
            if (DayManager.Instance == null) return daySequences[0];

            int index = DayManager.Instance.CurrentDay - 1; // Day 1 → index 0
            if (index < 0 || index >= daySequences.Length)  return null;

            return daySequences[index];
        }
    }
}
