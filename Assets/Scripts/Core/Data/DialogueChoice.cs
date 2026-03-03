/*
a branching choice shown as a button during an Albert dialogue
choiceText is what appears on the button, nextLineIndex is where we jump to

nextLineIndex -1 means end the conversation immediately
nextLineIndex >=0 jumps to that line in the parent DialogueSequence array

choices dont affect any game state - theyre purely for navigating the dialogue tree
*/
namespace SUNSET16.Core
{
    [System.Serializable]
    public class DialogueChoice
    {
        public string choiceText;

        //-1 = close dialogue, anything else = jump to that line index
        public int nextLineIndex = -1;
    }
}
