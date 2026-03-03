/*
the computer terminal in Andy's room - press E to start Albert's dialogue
assign one DialogueSequence per day in the Inspector (index 0 = day 1, index 1 = day 2 etc)

guards against opening if another dialogue is already showing or DOLOS is running
(InteractionSystem also blocks DOLOS upstream but we double-check here just in case)

DialogueUIManager handles all the movement locking and Escape key stuff once
the dialogue is open - this script just picks the right sequence and hands it over

replaces TechDemo/ComputerInteraction.cs which just logged to console and did nothing

TODO: different sequences based on pill state too (not just day)?
TODO: computer screen glow effect when player is in range
*/
using UnityEngine;
using SUNSET16.Core;
using SUNSET16.UI;

namespace SUNSET16.Interaction
{
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
            //dialogue already open, dont stack another one on top
            if (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueActive)
            {
                Debug.LogWarning("[COMPUTER] Dialogue already active — ignoring interaction");
                return;
            }

            //DOLOS is talking, not a great time to open Albert's terminal
            /*if (DOLOSManager.Instance != null && DOLOSManager.Instance.IsAnnouncementActive)
            {
                Debug.LogWarning("[COMPUTER] DOLOS active — ignoring interaction");
                return;
            }*/

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

            //fallback to first entry if DayManager isnt up yet
            if (DayManager.Instance == null) return daySequences[0];

            int index = DayManager.Instance.CurrentDay - 1; //day 1 → index 0
            if (index < 0 || index >= daySequences.Length)  return null;

            return daySequences[index];
        }
    }
}
