using UnityEngine;
using System;

namespace SUNSET16.Core
{
    public class TabletUIController : Singleton<TabletUIController>
    {
        [Header("Tablet Settings")]
        [Tooltip("Whether the tablet UI is currently visible.")]
        [SerializeField] private bool _isVisible = false;
        public bool IsInitialized { get; private set; }
        public bool IsVisible
        {
            get => _isVisible;
            private set => _isVisible = value;
        }
        private string _currentTaskName = "";
        private string _currentInstructions = "No task assigned.";
        private TaskDifficulty _currentDifficulty;
        private bool _currentTaskCompleted;
        public event Action<bool> OnTabletToggled;

        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsInitialized)
            {
                Initialize();
            }
            else if (GameManager.Instance != null)
            {
                GameManager.Instance.OnInitializationComplete += Initialize;
            }
        }

        private void Initialize()
        {
            if (TaskManager.Instance != null)
            {
                TaskManager.Instance.OnTaskSpawned += OnTaskSpawned;
                TaskManager.Instance.OnTaskCompleted += OnTaskCompleted;
            }

            SaveManager.Instance.OnSaveDeleted += OnSaveDeleted;

            IsInitialized = true;
            Debug.Log("[TABLETUICONTROLLER] Initialized (instructions-only mode)");
        }

        private void OnTaskSpawned(TaskData taskData)
        {
            _currentTaskName = taskData.taskName;
            _currentInstructions = taskData.instructions;
            _currentDifficulty = taskData.difficulty;
            _currentTaskCompleted = false;
            Debug.Log($"[TABLETUICONTROLLER] Updated display: '{taskData.taskName}' ({taskData.difficulty})");
        }

        private void OnTaskCompleted(int day)
        {
            _currentTaskCompleted = true;
            Debug.Log($"[TABLETUICONTROLLER] Task marked complete for Day {day}");
        }

        private void OnSaveDeleted()
        {
            _currentTaskName = "";
            _currentInstructions = "No task assigned.";
            _currentTaskCompleted = false;
            IsVisible = false;
            Debug.Log("[TABLETUICONTROLLER] Display reset (save deleted)");
        }

        public void ToggleTablet()
        {
            IsVisible = !IsVisible;
            OnTabletToggled?.Invoke(IsVisible);
            Debug.Log($"[TABLETUICONTROLLER] Tablet {(IsVisible ? "opened" : "closed")}");
        }

        public void OpenTablet()
        {
            if (!IsVisible)
            {
                IsVisible = true;
                OnTabletToggled?.Invoke(true);
            }
        }

        public void CloseTablet()
        {
            if (IsVisible)
            {
                IsVisible = false;
                OnTabletToggled?.Invoke(false);
            }
        }

        public void SetInstructions(string taskName, string instructions, TaskDifficulty difficulty)
        {
            _currentTaskName = taskName;
            _currentInstructions = instructions;
            _currentDifficulty = difficulty;
            _currentTaskCompleted = false;
        }

        private void OnGUI()
        {
            if (!IsInitialized || !IsVisible) return;

            float panelWidth = 350f;
            float panelHeight = 250f;
            float x = (Screen.width - panelWidth) / 2f;
            float y = (Screen.height - panelHeight) / 2f;

            GUILayout.BeginArea(new Rect(x, y, panelWidth, panelHeight), GUI.skin.box);

            GUILayout.Label("=== TABLET ===", GUI.skin.box);

            if (!string.IsNullOrEmpty(_currentTaskName))
            {
                GUILayout.Label($"Task: {_currentTaskName}");
                GUILayout.Label($"Difficulty: {_currentDifficulty}");
                GUILayout.Label($"Status: {(_currentTaskCompleted ? "COMPLETED" : "IN PROGRESS")}");
                GUILayout.Space(5);
                GUILayout.Label("--- Instructions ---");
                GUILayout.Label(_currentInstructions);
            }
            else
            {
                GUILayout.Label("No task assigned.");
                GUILayout.Label("Tasks appear during Morning phase after choosing your pill.");
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Close Tablet"))
            {
                CloseTablet();
            }

            GUILayout.EndArea();
        }

        private void OnDestroy()
        {
            if (TaskManager.Instance != null)
            {
                TaskManager.Instance.OnTaskSpawned -= OnTaskSpawned;
                TaskManager.Instance.OnTaskCompleted -= OnTaskCompleted;
            }

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.OnSaveDeleted -= OnSaveDeleted;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnInitializationComplete -= Initialize;
            }
        }
    }
}