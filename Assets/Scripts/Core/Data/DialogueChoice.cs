namespace SUNSET16.Core
{
    /// <summary>
    /// A branching choice presented to the player during an Albert dialogue session.
    /// Choices are session-scoped and inert — they do not affect any game state outside
    /// the current dialogue tree.
    /// </summary>
    [System.Serializable]
    public class DialogueChoice
    {
        /// <summary>Text displayed on the choice button.</summary>
        public string choiceText;

        /// <summary>
        /// Index of the DialogueLine to jump to when this choice is selected.
        /// Use -1 to end the dialogue immediately after selection.
        /// </summary>
        public int nextLineIndex = -1;
    }
}
