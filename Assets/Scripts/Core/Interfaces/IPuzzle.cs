namespace SUNSET16.Core
{
    public interface IPuzzle
    {
        string PuzzleId { get; }
        bool IsSolved { get; }
        void InitializePuzzle(PuzzleData puzzleData);
        void SolvePuzzle();
    }
}
