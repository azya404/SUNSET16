using UnityEngine;
using TMPro;
using System;
using System.Collections;
using SUNSET16.Core;

namespace SUNSET16.UI
{
    /// <summary>
    /// Persistent in-game HUD overlay.
    /// Displays: day counter, fading messages (locked door feedback, sleepy messages),
    /// and brief status notifications (Task Complete, Puzzle Solved).
    ///
    /// Lives in CoreScene (DontDestroyOnLoad via Singleton).
    /// Called directly by: DoorController (ShowMessage / ShowSleepyMessage).
    /// Called indirectly via events: TaskManager, PuzzleManager, DayManager.
    /// </summary>
    public class HUDController : Singleton<HUDController>
    {
        [Header("Day Counter")]
        [SerializeField] private TMP_Text dayCounterText;

        [Header("Fading Message (bottom-center — door locked / sleepy feedback)")]
        [SerializeField] private TMP_Text        messageText;
        [SerializeField] private CanvasGroup     messageCanvasGroup;
        [SerializeField] private float           messageFadeDuration    = 0.3f;
        [SerializeField] private float           messageDisplayDuration = 3f;

        [Header("Status Notification (brief top-center — Task Complete, etc.)")]
        [SerializeField] private TMP_Text        statusText;
        [SerializeField] private float           statusDisplayDuration  = 2f;

        [Header("Sleepy Message Pool")]
        [SerializeField] private string[] sleepyMessages = new string[]
        {
            "I'm so tired...",
            "I should head back to my room...",
            "My bed is calling me...",
            "I can barely keep my eyes open...",
            "Just need to rest..."
        };

        private Coroutine _messageCoroutine;
        private bool      _isGameOver = false;

        // ─── Lifecycle ────────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();

            if (messageCanvasGroup != null)
                messageCanvasGroup.alpha = 0f;

            if (messageText != null)
                messageText.text = "";

            if (statusText != null)
                statusText.text = "";
        }

        private void Start()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsInitialized)
                Subscribe();
            else if (GameManager.Instance != null)
                GameManager.Instance.OnInitializationComplete += Subscribe;
        }

        private void Subscribe()
        {
            DayManager.Instance.OnDayChanged     += HandleDayChanged;
            DayManager.Instance.OnGameComplete   += HandleGameOver;
            DayManager.Instance.OnGameEndedEarly += HandleGameEndedEarly;

            SaveManager.Instance.OnGameLoaded    += RefreshDayCounter;
            SaveManager.Instance.OnSaveDeleted   += ResetDayCounter;

            HandleDayChanged(DayManager.Instance.CurrentDay);
            Debug.Log("[HUD] Subscribed and initialised");
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnInitializationComplete -= Subscribe;

            if (DayManager.Instance != null)
            {
                DayManager.Instance.OnDayChanged     -= HandleDayChanged;
                DayManager.Instance.OnGameComplete   -= HandleGameOver;
                DayManager.Instance.OnGameEndedEarly -= HandleGameEndedEarly;
            }

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.OnGameLoaded  -= RefreshDayCounter;
                SaveManager.Instance.OnSaveDeleted -= ResetDayCounter;
            }
        }

        // ─── Event Handlers ───────────────────────────────────────────────────────

        private void HandleDayChanged(int day)
        {
            if (dayCounterText != null)
                dayCounterText.text = $"Day {day}";
        }

        private void HandleGameOver()
        {
            _isGameOver = true;
            SetHUDVisible(false);
        }

        private void HandleGameEndedEarly(int day)
        {
            _isGameOver = true;
            SetHUDVisible(false);
        }

        private void RefreshDayCounter()
        {
            if (DayManager.Instance != null)
                HandleDayChanged(DayManager.Instance.CurrentDay);
        }

        private void ResetDayCounter()
        {
            if (dayCounterText != null)
                dayCounterText.text = "Day 1";
            ClearMessage();
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Show a fading message. Called by DoorController for specific locked-door strings.
        /// Auto-fades after duration. Interrupts any currently showing message.
        /// </summary>
        public void ShowMessage(string message, float duration = 0f)
        {
            if (messageText == null || messageCanvasGroup == null) return;

            if (duration <= 0f) duration = messageDisplayDuration;

            if (_messageCoroutine != null)
                StopCoroutine(_messageCoroutine);

            _messageCoroutine = StartCoroutine(ShowMessageCoroutine(message, duration));
        }

        /// <summary>
        /// Show a random message from the sleepy pool.
        /// Called by DoorController when a night-restricted door is attempted.
        /// </summary>
        public void ShowSleepyMessage()
        {
            if (sleepyMessages == null || sleepyMessages.Length == 0) return;

            string msg = sleepyMessages[UnityEngine.Random.Range(0, sleepyMessages.Length)];
            ShowMessage(msg, messageDisplayDuration);
        }

        /// <summary>
        /// Brief status notification (e.g. "Task Complete!", "Puzzle Solved!").
        /// Auto-clears after statusDisplayDuration.
        /// </summary>
        public void ShowStatus(string status)
        {
            if (statusText == null) return;

            statusText.text = status;
            CancelInvoke(nameof(ClearStatus));
            Invoke(nameof(ClearStatus), statusDisplayDuration);
        }

        /// <summary>Immediately clear any active fading message.</summary>
        public void ClearMessage()
        {
            if (_messageCoroutine != null)
            {
                StopCoroutine(_messageCoroutine);
                _messageCoroutine = null;
            }

            if (messageCanvasGroup != null) messageCanvasGroup.alpha = 0f;
            if (messageText != null)        messageText.text = "";
        }

        /// <summary>Show or hide the entire HUD (e.g. during cutscenes or game-over).</summary>
        public void SetHUDVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        // ─── Internal ─────────────────────────────────────────────────────────────

        private IEnumerator ShowMessageCoroutine(string message, float duration)
        {
            messageText.text = message;

            // Fade in
            float elapsed = 0f;
            while (elapsed < messageFadeDuration)
            {
                elapsed += Time.deltaTime;
                messageCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / messageFadeDuration);
                yield return null;
            }
            messageCanvasGroup.alpha = 1f;

            // Hold
            yield return new WaitForSeconds(duration);

            // Fade out
            elapsed = 0f;
            while (elapsed < messageFadeDuration)
            {
                elapsed += Time.deltaTime;
                messageCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / messageFadeDuration);
                yield return null;
            }
            messageCanvasGroup.alpha = 0f;
            messageText.text = "";
            _messageCoroutine = null;
        }

        private void ClearStatus()
        {
            if (statusText != null)
                statusText.text = "";
        }
    }
}
