using UnityEngine;

namespace SUNSET16.Core
{
    /// <summary>
    /// A single line of dialogue spoken by one character.
    /// Part of a DialogueSequence ScriptableObject.
    /// </summary>
    [System.Serializable]
    public class DialogueLine
    {
        [Tooltip("Name displayed above the dialogue text (e.g. 'Albert', 'DOLOS-XIII').")]
        public string speakerName;

        [Tooltip("Optional speaker portrait sprite. Leave null to hide portrait slot.")]
        public Sprite portrait;

        [Tooltip("The dialogue text. Rendered with typewriter effect.")]
        [TextArea(3, 6)]
        public string text;

        [Tooltip("0 = wait for player to press Continue. >0 = auto-advance after this many seconds.")]
        public float autoAdvanceDelay = 0f;

        [Tooltip("Leave empty for linear flow. Populate for branching choice buttons (up to 3).")]
        public DialogueChoice[] choices;

        /// <summary>True if this line presents player choices rather than auto/manual advance.</summary>
        public bool HasChoices => choices != null && choices.Length > 0;
    }
}
