using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SUNSET16.Core;

namespace SUNSET16.UI
{
    /// <summary>
    /// Task and Puzzle Overlay — the full-screen split UI that appears when the player
    /// presses E on a world-space task or puzzle object.
    ///
    /// Layout:
    ///   Left 40%  — TabletArea: static task instructions (never changes mid-task).
    ///   Right 60% — PuzzleArea: interactive puzzle Canvas prefab instantiated at runtime.
    ///   Background — semi-transparent black overlay, blocks world click-through.
    ///
    /// Rules (by design):
    ///   • No early exit. Once the overlay is open, the player works until task is complete.
    ///   • Puzzle UI is provided as a Canvas prefab by the interactable task/puzzle object.
    ///   • DOLOS announcements are suppressed while the overlay is active.
    ///   • Player movement is locked for the duration.
    ///
    /// Called directly by: task/puzzle world objects (IInteractable.Interact()).
    /// Calls back to: TaskManager.CompleteCurrentTask() when puzzle signals completion,
    ///                PuzzleManager.CompletePuzzle(id) for puzzle objects.
    ///
    /// Lives in CoreScene (DontDestroyOnLoad via Singleton).
    /// </summary>
    public class TaskUIManager : Singleton<TaskUIManager>
    {
        [Header("Overlay Root")]
        [SerializeField] private GameObject overlayRoot;         // Parent of everything; hidden by default

        [Header("Layout Regions")]
        [SerializeField] private RectTransform tabletArea;       // Left 40%
        [SerializeField] private RectTransform puzzleArea;       // Right 60%

        [Header("Tablet (Left Pane)")]
        [SerializeField] private TMP_Text taskNameText;          // Task name header
        [SerializeField] private TMP_Text taskInstructionsText;  // Static instruction body (full from spawn)

        [Header("Background Overlay")]
        [SerializeField] private Image backgroundOverlay;        // Semi-transparent black, raycast blocker

        // ─── Runtime State ────────────────────────────────────────────────────────

        /// <summary>True while the task/puzzle overlay is open. PauseMenu and Map check this.</summary>
        public bool IsOverlayActive { get; private set; }

        private GameObject _currentPuzzleUI;

        // ─── Lifecycle ────────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            if (overlayRoot != null) overlayRoot.SetActive(false);
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Open the task overlay.
        /// Called by IInteractable.Interact() on a world-space task or puzzle object.
        ///
        /// <param name="taskName">Header shown at the top of the tablet pane.</param>
        /// <param name="instructions">Full static instructions for this task (shown immediately).</param>
        /// <param name="puzzleUIPrefab">Canvas prefab instantiated into the right region.
        ///                              Pass null if the task has no interactive UI element yet.</param>
        /// </summary>
        public void ShowOverlay(string taskName, string instructions, GameObject puzzleUIPrefab = null)
        {
            if (IsOverlayActive)
            {
                Debug.LogWarning("[TASKUI] Overlay already active — ignoring ShowOverlay call.");
                return;
            }

            IsOverlayActive = true;

            // Populate tablet pane
            if (taskNameText         != null) taskNameText.text         = taskName    ?? "";
            if (taskInstructionsText != null) taskInstructionsText.text = instructions ?? "";

            // Instantiate puzzle UI into right region
            SetPuzzleUI(puzzleUIPrefab);

            if (overlayRoot != null) overlayRoot.SetActive(true);

            // Lock player movement — no early exit while overlay is open
            if (PlayerController.Instance != null)
                PlayerController.Instance.LockMovement(true);

            Debug.Log($"[TASKUI] Overlay opened for task: '{taskName}'");
        }

        /// <summary>
        /// Close the task overlay and clean up.
        /// Called by the puzzle UI component when the puzzle/task is completed.
        /// Not intended to be called before task completion.
        /// </summary>
        public void HideOverlay()
        {
            if (!IsOverlayActive) return;

            IsOverlayActive = false;

            DestroyPuzzleUI();

            if (overlayRoot != null) overlayRoot.SetActive(false);

            if (PlayerController.Instance != null)
                PlayerController.Instance.LockMovement(false);

            Debug.Log("[TASKUI] Overlay closed (task/puzzle complete)");
        }

        /// <summary>
        /// Convenience: complete the current day's task and close the overlay.
        /// Call this from the puzzle UI prefab once the player solves the puzzle.
        /// </summary>
        public void CompleteTaskAndClose()
        {
            if (HUDController.Instance != null)
                HUDController.Instance.ShowStatus("Task Complete!");

            if (TaskManager.Instance != null)
                TaskManager.Instance.CompleteCurrentTask();

            HideOverlay();
        }

        /// <summary>
        /// Convenience: complete a hidden-room puzzle and close the overlay.
        /// </summary>
        public void CompletePuzzleAndClose(string puzzleId)
        {
            if (HUDController.Instance != null)
                HUDController.Instance.ShowStatus("Puzzle Solved!");

            if (PuzzleManager.Instance != null)
                PuzzleManager.Instance.CompletePuzzle(puzzleId);

            HideOverlay();
        }

        /// <summary>Returns the RectTransform of the puzzle area (right 60% region).</summary>
        public RectTransform GetPuzzleArea() => puzzleArea;

        // ─── Internal ─────────────────────────────────────────────────────────────

        private void SetPuzzleUI(GameObject prefab)
        {
            DestroyPuzzleUI();

            if (prefab == null || puzzleArea == null) return;

            _currentPuzzleUI = Instantiate(prefab, puzzleArea);

            // Stretch to fill the puzzle area
            RectTransform rt = _currentPuzzleUI.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin  = Vector2.zero;
                rt.anchorMax  = Vector2.one;
                rt.offsetMin  = Vector2.zero;
                rt.offsetMax  = Vector2.zero;
            }
        }

        private void DestroyPuzzleUI()
        {
            if (_currentPuzzleUI != null)
            {
                Destroy(_currentPuzzleUI);
                _currentPuzzleUI = null;
            }
        }
    }
}
