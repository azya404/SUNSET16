using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Dialogue Asset")]
public class Dialogue : ScriptableObject
{
    // First node of the conversation
    public List<DialogueNode> nodes;
}
