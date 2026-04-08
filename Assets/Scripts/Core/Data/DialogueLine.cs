/*
single line of dialogue for Albert (or whoever is speaking)
holds the speaker name, optional portrait, the actual text, and an optional
list of player choices for branching

HasChoices tells DialogueUIManager whether to show choice buttons or just
a continue prompt. used by DialogueSequence which is the ScriptableObject
that groups these into a full conversation
*/
using System.Collections.Generic;
using UnityEngine;

namespace SUNSET16.Core
{
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

        public bool sendDelay = true;
        
        public int delayRepeats;

        [Tooltip("0 = wait for player to press Continue. >0 = auto-advance after this many seconds.")]
        public float autoAdvanceDelay = 0f;

        [Tooltip("Leave empty for linear flow. Populate for branching choice buttons (up to 3).")]
        public List<DialogueChoice> choices;

        //true if this line has choices, false if its just a continue prompt
        public bool HasChoices => choices != null && choices.Count > 0;

        public int advanceToLine;

        public bool repeat;

        public bool repeated;

        public string loreEntry;

        public bool switchToDOLOS;

        public bool glitch;
    }
}
