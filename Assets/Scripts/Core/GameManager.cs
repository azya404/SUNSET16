using UnityEngine;
using System; //need to actions later on

namespace SUNSET16.Core
{
    public class GameManager : Singleton<GameManager>
    {
        public bool IsInitialized { get; private set; }
        public event Action OnInitializationComplete;
        public event Action OnApplicationPaused;
        public event Action OnApplicationResumed;
        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            Debug.Log("[GAMEMANAGER] ===== INITIALIZATION SEQUENCE START =====");
            if (DayManager.Instance == null ||
                PillStateManager.Instance == null ||
                SettingsManager.Instance == null ||
                SaveManager.Instance == null)
            {
                Debug.LogError("[GAMEMANAGER] CRITICAL: One or more managers missing from CoreScene!");
                return;
            }

            Debug.Log("[GAMEMANAGER] Phase 1: Initializing state managers...");
            DayManager.Instance.Initialize();
            PillStateManager.Instance.Initialize();
            SettingsManager.Instance.Initialize();
            PillStateManager.Instance.OnEndingReached += HandleEndingReached;
            DayManager.Instance.OnGameEndedEarly += HandleGameEndedEarly;
            DayManager.Instance.OnGameComplete += HandleGameComplete;

            Debug.Log("[GAMEMANAGER] Phase 2: Initializing save system...");
            SaveManager.Instance.Initialize();

            if (SaveManager.Instance.SaveExists)
            {
                Debug.Log("[GAMEMANAGER] Loading saved game...");
                SaveManager.Instance.LoadGame();
            }

            IsInitialized = true;
            OnInitializationComplete?.Invoke();
            Debug.Log("[GAMEMANAGER] ===== INITIALIZATION COMPLETE =====");
        }

        protected override void OnApplicationQuit()
        {
            if (IsInitialized)
            {
                Debug.Log("[GAMEMANAGER] Application quitting - auto-saving...");
                SaveManager.Instance.SaveGame();
            }
            base.OnApplicationQuit();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!IsInitialized)
                return;

            if (pauseStatus)
            {
                Debug.Log("[GAMEMANAGER] Application paused - auto-saving...");
                SaveManager.Instance.SaveGame();
                OnApplicationPaused?.Invoke();
            }
            else
            {
                Debug.Log("[GAMEMANAGER] Application resumed");
                OnApplicationResumed?.Invoke();
            }
        }

        public void QuitGame()
        {
            SaveManager.Instance.SaveGame();
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        private void HandleEndingReached(string ending)
        {
            Debug.Log($"[GAMEMANAGER] Ending reached: {ending}");
        }

        private void HandleGameEndedEarly(int day)
        {
            Debug.Log($"[GAMEMANAGER] Game ended early on Day {day}");
        }

        private void HandleGameComplete()
        {
            Debug.Log("[GAMEMANAGER] Game reached natural completion (Day 5)");
        }
    }
}