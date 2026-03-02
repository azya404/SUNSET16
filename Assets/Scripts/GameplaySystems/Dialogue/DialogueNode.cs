using System.Collections.Generic;

[System.Serializable]
public class DialogueNode
{
    public string dialogueText;
    public bool repeat;
    public int repeatNum;
    public List<DialogueResponse> responses;
}
