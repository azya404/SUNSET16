using UnityEngine;
using SUNSET16.Core;

namespace SUNSET16.Core
{
    public class ServerRoomController : MonoBehaviour
    {
        [Header("Puzzle ID")]
        [SerializeField] private string puzzleId = "puzzle_room_2";

        [Header("Computer Screen")]
        [SerializeField] private SpriteRenderer computerScreen;
        [SerializeField] private Sprite notDoneSprite;
        [SerializeField] private Sprite successSprite;

        private void Start()
        {
            if (PuzzleManager.Instance != null)
                PuzzleManager.Instance.OnPuzzleCompleted += OnPuzzleCompleted;
        }

        private void OnDestroy()
        {
            if (PuzzleManager.Instance != null)
                PuzzleManager.Instance.OnPuzzleCompleted -= OnPuzzleCompleted;
        }

        private void OnPuzzleCompleted(string completedPuzzleId)
        {
            if (completedPuzzleId != puzzleId) return;

            if (computerScreen != null && successSprite != null)
            {
                computerScreen.sprite = successSprite;
                Debug.Log("[SERVERROOMCONTROLLER] Computer screen swapped to success state");
            }
        }
    }
}

