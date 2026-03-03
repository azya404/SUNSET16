using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using SUNSET16.Core;

namespace SUNSET16.UI
{
    /// <summary>
    /// Pause Menu — toggled with Escape during normal gameplay.
    ///
    /// Escape priority order:
    ///   1. DOLOS active         → ignore (do nothing)
    ///   2. Albert dialogue active → ignored (DialogueUIManager handles its own Escape)
    ///   3. Task overlay active  → ignored (no early exit from tasks)
    ///   4. Map is open          → close map (don't pause)
    ///   5. Settings open        → close settings (don't pause)
    ///   6. Pause menu open      → resume (close pause menu)
    ///   7. Default              → open pause menu
    ///
    /// Also subscribes to DOLOSManager.OnSettingsRequested to open settings after
    /// an announcement ends (if the player pressed the settings key during DOLOS).
    ///
    /// Lives in CoreScene. Does NOT need to be a Singleton — no external system calls it.
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        [Header("Pause Panel")]
        [SerializeField] private GameObject pauseMenuPanel;

        [Header("Settings Panel Reference")]
        [SerializeField] private SettingsPanel settingsPanelRef;
        [SerializeField] private GameObject    settingsPanelGO;   // The GO to check/toggle

        [Header("Save Feedback")]
        [SerializeField] private TMP_Text  saveStatusText;
        [SerializeField] private float     saveStatusDuration = 2f;

        private bool _isPaused   = false;
        private bool _isGameOver = false;

        // ─── Lifecycle ────────────────────────────────────────────────────────────

        private void Start()
        {
            if (pauseMenuPanel  != null) pauseMenuPanel.SetActive(false);
            if (settingsPanelGO != null) settingsPanelGO.SetActive(false);
            if (saveStatusText  != null) saveStatusText.text = "";

            if (GameManager.Instance != null && GameManager.Instance.IsInitialized)
                Subscribe();
            else if (GameManager.Instance != null)
                GameManager.Instance.OnInitializationComplete += Subscribe;
        }

        private void Subscribe()
        {
            DayManager.Instance.OnGameComplete   += HandleGameOver;
            DayManager.Instance.OnGameEndedEarly += HandleGameEndedEarly;

            // Honor settings requests that were queued during a DOLOS announcement
            if (DOLOSManager.Instance != null)
                DOLOSManager.Instance.OnSettingsRequested += OpenSettings;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnInitializationComplete -= Subscribe;

            if (DayManager.Instance != null)
            {
                DayManager.Instance.OnGameComplete   -= HandleGameOver;
                DayManager.Instance.OnGameEndedEarly -= HandleGameEndedEarly;
            }

            if (DOLOSManager.Instance != null)
                DOLOSManager.Instance.OnSettingsRequested -= OpenSettings;
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;
            if (_isGameOver) return;

            HandleEscapeInput();
        }

        // ─── Escape Priority Chain ────────────────────────────────────────────────

        private void HandleEscapeInput()
        {
            // 1. DOLOS active — player cannot pause during announcements
            if (DOLOSManager.Instance != null && DOLOSManager.Instance.IsAnnouncementActive)
                return;

            // 2. Albert dialogue active — DialogueUIManager.Update() handles Escape for dialogue
            if (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueActive)
                return;

            // 3. Task overlay active — no early exit; design decision
            if (TaskUIManager.Instance != null && TaskUIManager.Instance.IsOverlayActive)
                return;

            // 4. Map is open — close map, don't pause
            if (MapUIController.Instance != null && MapUIController.Instance.IsMapOpen)
            {
                MapUIController.Instance.CloseMap();
                return;
            }

            // 5. Settings panel is open — close settings, don't pause
            if (settingsPanelGO != null && settingsPanelGO.activeSelf)
            {
                settingsPanelGO.SetActive(false);
                return;
            }

            // 6 & 7. Toggle pause
            TogglePause();
        }

        private void TogglePause()
        {
            _isPaused = !_isPaused;
            Time.timeScale = _isPaused ? 0f : 1f;
            pauseMenuPanel?.SetActive(_isPaused);

            if (PlayerController.Instance != null)
                PlayerController.Instance.LockMovement(_isPaused);

            Debug.Log($"[PAUSE] {(_isPaused ? "Paused" : "Resumed")}");
        }

        // ─── Button Callbacks (wired via Inspector OnClick) ───────────────────────

        public void OnResumeClicked()
        {
            if (_isPaused) TogglePause();
        }

        public void OnSaveGameClicked()
        {
            SaveManager.Instance?.SaveGame();
            StartCoroutine(ShowSaveFeedback("Game saved!"));
        }

        public void OnSettingsClicked()
        {
            OpenSettings();
        }

        public void OnQuitToMainMenuClicked()
        {
            // Restore normal time before scene change
            Time.timeScale = 1f;
            _isPaused      = false;
            SceneManager.LoadScene("MainMenu");
        }

        // ─── Internal ─────────────────────────────────────────────────────────────

        private void OpenSettings()
        {
            if (settingsPanelGO != null)
                settingsPanelGO.SetActive(true);
        }

        private void HandleGameOver()
        {
            _isGameOver = true;
            // If paused when game ends, restore time and hide the menu
            if (_isPaused)
            {
                Time.timeScale = 1f;
                _isPaused      = false;
                if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            }
        }

        private void HandleGameEndedEarly(int day)
        {
            HandleGameOver();
        }

        private IEnumerator ShowSaveFeedback(string message)
        {
            if (saveStatusText != null)
            {
                saveStatusText.text = message;
                // WaitForSecondsRealtime: unaffected by Time.timeScale = 0 (game is paused)
                yield return new WaitForSecondsRealtime(saveStatusDuration);
                saveStatusText.text = "";
            }
        }
    }
}
