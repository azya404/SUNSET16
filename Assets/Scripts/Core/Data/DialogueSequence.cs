/*
ScriptableObject that holds a full Albert conversation as an array of DialogueLines
one of these per conversation - create them in the editor under SUNSET16/Dialogue Sequence

ComputerInteraction picks which one to use based on the current day (daySequences[0] = day 1 etc)
sequenceId is there for identification if we need to look one up by name later
*/
using System.Collections.Generic;
using UnityEngine;

namespace SUNSET16.Core
{
    [CreateAssetMenu(fileName = "NewDialogueSequence", menuName = "SUNSET16/Dialogue Sequence")]
    public class DialogueSequence : ScriptableObject
    {
        [Tooltip("Unique ID used to identify and select this sequence (e.g. 'albert_day1_pill').")]
        public string sequenceId;

        [Tooltip("All dialogue lines in this tree, played in order unless a choice branches the index.")]
        public List<DialogueLine> lines;
    }
}
