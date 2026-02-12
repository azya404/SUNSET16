using UnityEngine;
namespace SUNSET16.Core
{
    [CreateAssetMenu(fileName = "NewLoreEntry", menuName = "SUNSET16/Lore Entry Data")]
    public class LoreEntryData : ScriptableObject
    {
        [Header("lore identity")]
        [Tooltip("a unique name for this lore entry (like 'lore_escape_plan') so we can reference it in code")]
        public string loreId;

        [Tooltip("the title that shows up when the player opens this lore entry")]
        public string title;

        [Header("lore content")]
        [Tooltip("the main text for this lore entry")]
        [TextArea(5, 15)]
        public string content;

        [Header("metadata")]
        [Tooltip("if this lore is connected to a specific day, put the day number here (leave 0 if not tied to one)")]
        [Range(0, 5)]
        public int associatedDay;

        [Tooltip("a short preview of the lore that shows up in the list before the player clicks it")]
        public string preview;
    }
}
