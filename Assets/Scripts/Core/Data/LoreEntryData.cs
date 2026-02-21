/*
ScriptableObject that holds data for a single lore entry (the USB drive content)
when the player solves a hidden room puzzle, they get a USB drive as a reward
that USB drive contains a lore entry - a piece of the story about the space station

we use ScriptableObjects here instead of hardcoding text in scripts cos:
1. designers can create/edit lore entries in the Unity Inspector without touching code
2. each lore entry is its own asset file that can be referenced by PuzzleData
3. easy to add new lore without recompiling (just right-click -> Create -> SUNSET16 -> Lore Entry Data)

the lore entries tell the hidden story of the space station:
- why the pills exist, what theyre really doing
- the truth about the "tasks" the player does every day
- hints about the escape pod and how to reach the good ending

TabletUIController displays these when the player opens their tablet
PuzzleManager links them as rewards via PuzzleData.loreReward

TODO: might want to add a Sprite icon field for visual representation in the UI
TODO: might want to add an AudioClip for a narrated version of the lore
*/
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
