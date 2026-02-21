/*
ScriptableObject that configures a single hidden room puzzle
each hidden room has one puzzle, and each puzzle has one PuzzleData asset

this is the data-driven approach where puzzle configuration
lives in Unity assets rather than being hardcoded in scripts
you create these in Unity: right-click -> Create -> SUNSET16 -> Puzzle Data

PuzzleManager uses these to:
1. look up which puzzle to spawn for a given room (by puzzleId)
2. instantiate the puzzle prefab (which should implement IPuzzle)
3. award the lore reward (LoreEntryData) when the puzzle is solved

the puzzlePrefab should be a GameObject with a script implementing IPuzzle
for example: a prefab with WirePuzzleController.cs that implements IPuzzle

TODO: might want to add difficulty settings or time limits per puzzle
TODO: might want to add a preview image that shows in the tablet before entering the room
*/
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
