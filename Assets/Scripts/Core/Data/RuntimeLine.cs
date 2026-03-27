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
    public class RuntimeLine
    {
        public string speakerName;

        public Sprite portrait;

        public string text;

        public bool sendDelay = true;

        public int delayRepeats;
        public float autoAdvanceDelay = 0f;

        public List<RuntimeChoice> choices;

        //true if this line has choices, false if its just a continue prompt
        public bool HasChoices => choices != null && choices.Count > 0;

        public int advanceToLine;

        public bool repeat;

        public bool repeated;
        public string loreEntry;
        public bool switchToDOLOS;
    }
}
