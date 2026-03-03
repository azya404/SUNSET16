using UnityEngine;

namespace SUNSET16.Core
{
    /// <summary>
    /// A complete dialogue tree for Albert (the computer terminal).
    /// Which sequence is active is determined externally (by day, pill state, task completion).
    /// Assign via the Unity asset menu: SUNSET16 > Dialogue Sequence.
    /// </summary>
    [CreateAssetMenu(fileName = "NewDialogueSequence", menuName = "SUNSET16/Dialogue Sequence")]
    public class DialogueSequence : ScriptableObject
    {
        [Tooltip("Unique ID used to identify and select this sequence (e.g. 'albert_day1_pill').")]
        public string sequenceId;

        [Tooltip("All dialogue lines in this tree, played in order unless a choice branches the index.")]
        public DialogueLine[] lines;
    }
}
