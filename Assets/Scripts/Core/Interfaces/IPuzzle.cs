/*
interface for hidden room puzzles
any puzzle we create needs to implement this so PuzzleManager can work with it
without knowing the specific puzzle type (same polymorphism idea as IInteractable)

puzzles are the OPTIONAL content in SUNSET16 - only accessible if you refuse the pill
they reward you with USB drives containing lore about the space station
and are key to unlocking the good ending (you need to discover the truth to escape)

PuzzleManager spawns the puzzle prefab, calls InitializePuzzle() with the PuzzleData,
and then waits for IsSolved to become true (or SolvePuzzle() to be called)

we dont have actual puzzle implementations yet (like the wire puzzle or memory game)
those will be their own scripts that implement this interface
for example: WirePuzzleController : MonoBehaviour, IPuzzle

TODO: add an OnPuzzleSolved event so PuzzleManager can listen for completion
      instead of checking IsSolved every frame (event-driven > polling)
TODO: add a ResetPuzzle() method for if the player wants to retry? SHOULD THIS BE ALLOWED????
*/
namespace SUNSET16.Core
{
    public interface IPuzzle
    {
        string PuzzleId { get; }  //unique id like "puzzle_day_1" - matches the PuzzleData.puzzleId
        bool IsSolved { get; }    //true once the player has completed this puzzle
        void InitializePuzzle(PuzzleData puzzleData); //called by PuzzleManager when spawning, sets up the puzzle with its data
        void SolvePuzzle();       //called when the puzzle is completed - triggers rewards and cleanup
    }
}
