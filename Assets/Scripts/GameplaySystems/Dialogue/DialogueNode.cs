using System.Collections.Generic;

[System.Serializable]
public class DialogueNode
{
    public int id;
    public string dialogueText;
    public bool repeat;
    public int repeatNum;
    public bool anotherMessage;
    public int otherMessageID;
    public List<DialogueResponse> responses;
}
