using UnityEngine;

namespace SUNSET16.Core
{
    [CreateAssetMenu(fileName = "NewPuzzleData", menuName = "SUNSET16/Puzzle Data")]
    public class PuzzleData : ScriptableObject
    {
        [Header("puzzle identity")]
        [Tooltip("a unique id for this puzzle (like 'puzzle_room1') so we can reference it in code")]
        public string puzzleId;

        [Tooltip("the name that shows up for this puzzle in the game")]
        public string puzzleName;

        [Header("puzzle content")]
        [Tooltip("the puzzle prefab that gets spawned in the hidden room (it should implement IPuzzle)")]
        public GameObject puzzlePrefab;

        [Header("rewards")]
        [Tooltip("the lore entry that gets unlocked after finishing this puzzle (like the usb drive content)")]
        public LoreEntryData loreReward;
    }
}
