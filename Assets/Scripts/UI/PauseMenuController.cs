/*
pause menu - Escape key with a priority chain so it always does the right thing

priority order when Escape is pressed:
1. DOLOS announcement running -> do nothing, DOLOS owns the screen right now
2. Albert dialogue active -> do nothing, DialogueUIManager handles its own Escape
3. task overlay active -> do nothing, no early exit from tasks by design
4. map is open -> close map (don't pause)
5. settings panel is open -> close settings (don't pause)
6. pause menu is open -> resume
7. nothing else -> open pause menu

NOT a Singleton - nothing external ever needs to call PauseMenuController directly
it just sits in the scene and listens to Escape, no references needed

subscribes to DOLOSManager.OnSettingsRequested so if the player tries to open settings
during an announcement it gets queued and fires as soon as the announcement ends

WaitForSecondsRealtime for save feedback because Time.timeScale is 0 while paused
using WaitForSeconds here would wait forever

TODO: pause menu art
TODO: keybindings display
*/
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using SUNSET16.Core;

namespace SUNSET16.UI
{
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

            // open settings after a DOLOS announcement ends if the player tried during it
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
            // restore time before changing scenes or the game will load paused
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
            // if we happened to be paused when the game ended, clean up
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
