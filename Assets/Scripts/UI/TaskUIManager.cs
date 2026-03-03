/*
task overlay - the full-screen split UI that blocks the world when the player presses E on a task or puzzle object

left 40% is the "tablet" pane: task name header + static instructions
the layout is set up in Unity, nothing is hardcoded here - just fill in the text fields
right 60% is where the puzzle Canvas prefab gets instantiated at runtime
the puzzle prefab is handed in by whatever world object triggered the overlay,
so this script has no idea what the puzzle is - it just parents it into puzzleArea and stretches it to fill

no early exit - by design the player HAS to finish the task to close the overlay
escape does nothing, we block it in PauseMenuController's priority chain
TaskManager used to call LockMovement but that got pulled out - movement locking lives here now

IsOverlayActive is a public flag checked by MapUIController and PauseMenuController
so they know to back off while a task is running

lives in CoreScene as a Singleton (DontDestroyOnLoad)

TODO: slide-in/out animation for opening and closing
TODO: actual tablet art for the left panel (right now its just text on a placeholder rect)
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SUNSET16.Core;

namespace SUNSET16.UI
{
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

        // checked by MapUIController and PauseMenuController before they do anything
        public bool IsOverlayActive { get; private set; }

        private GameObject _currentPuzzleUI;

        // ─── Lifecycle ────────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            if (overlayRoot != null) overlayRoot.SetActive(false);
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        public void ShowOverlay(string taskName, string instructions, GameObject puzzleUIPrefab = null)
        {
            if (IsOverlayActive)
            {
                Debug.LogWarning("[TASKUI] Overlay already active — ignoring ShowOverlay call.");
                return;
            }

            IsOverlayActive = true;

            // populate the tablet pane with whatever the world object passed in
            if (taskNameText         != null) taskNameText.text         = taskName    ?? "";
            if (taskInstructionsText != null) taskInstructionsText.text = instructions ?? "";

            // instantiate the puzzle prefab into the right region
            SetPuzzleUI(puzzleUIPrefab);

            if (overlayRoot != null) overlayRoot.SetActive(true);

            // lock movement - no early exit while overlay is open
            if (PlayerController.Instance != null)
                PlayerController.Instance.LockMovement(true);

            Debug.Log($"[TASKUI] Overlay opened for task: '{taskName}'");
        }

        // called by the puzzle UI prefab when the player finishes - not before
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

        // shortcut for puzzle UIs to call when the player solves the day's main task
        public void CompleteTaskAndClose()
        {
            if (HUDController.Instance != null)
                HUDController.Instance.ShowStatus("Task Complete!");

            if (TaskManager.Instance != null)
                TaskManager.Instance.CompleteCurrentTask();

            HideOverlay();
        }

        // shortcut for hidden-room puzzle UIs
        public void CompletePuzzleAndClose(string puzzleId)
        {
            if (HUDController.Instance != null)
                HUDController.Instance.ShowStatus("Puzzle Solved!");

            if (PuzzleManager.Instance != null)
                PuzzleManager.Instance.CompletePuzzle(puzzleId);

            HideOverlay();
        }

        public RectTransform GetPuzzleArea() => puzzleArea;

        // ─── Internal ─────────────────────────────────────────────────────────────

        private void SetPuzzleUI(GameObject prefab)
        {
            DestroyPuzzleUI();

            if (prefab == null || puzzleArea == null) return;

            _currentPuzzleUI = Instantiate(prefab, puzzleArea);

            // stretch to fill the puzzle area so whatever prefab we get looks right
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
