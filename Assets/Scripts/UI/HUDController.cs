/*
the persistent in-game overlay - lives in CoreScene via DontDestroyOnLoad

three things it shows:
- day counter (top corner, updates automatically via DayManager.OnDayChanged)
- fading message panel (bottom-center) - used for locked door feedback ("The door is locked.")
  and sleepy messages when the player tries doors they shouldnt at night
- brief status text (top-center) - things like "Task Complete!" that auto-clear quickly

DoorController calls ShowMessage/ShowSleepyMessage directly on this
SaveManager events (OnGameLoaded, OnSaveDeleted) also feed in so the counter
stays accurate across saves and resets

design note on sleepy messages: they only fire when the player ATTEMPTS a door at night,
NOT when night starts. we deliberately didnt add a "you feel sleepy" popup at phase change
because it felt too hand-holdy. the door attempt is a natural moment for that feedback

TODO: actual panel animations - the CanvasGroup fade logic is in place but the
message panel itself still needs art/background treatment
TODO: day counter visual polish - its just raw text right now
*/
using UnityEngine;
using TMPro;
using System;
using System.Collections;
using SUNSET16.Core;

namespace SUNSET16.UI
{
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

        public void ShowMessage(string message, float duration = 0f)
        {
            if (messageText == null || messageCanvasGroup == null) return;

            if (duration <= 0f) duration = messageDisplayDuration;

            //interrupt any in-progress message and start fresh
            if (_messageCoroutine != null)
                StopCoroutine(_messageCoroutine);

            _messageCoroutine = StartCoroutine(ShowMessageCoroutine(message, duration));
        }

        public void ShowSleepyMessage()
        {
            if (sleepyMessages == null || sleepyMessages.Length == 0) return;

            string msg = sleepyMessages[UnityEngine.Random.Range(0, sleepyMessages.Length)];
            ShowMessage(msg, messageDisplayDuration);
        }

        public void ShowStatus(string status)
        {
            if (statusText == null) return;

            statusText.text = status;
            CancelInvoke(nameof(ClearStatus));
            Invoke(nameof(ClearStatus), statusDisplayDuration);
        }

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

        public void SetHUDVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        // ─── Internal ─────────────────────────────────────────────────────────────

        private IEnumerator ShowMessageCoroutine(string message, float duration)
        {
            messageText.text = message;

            //fade in - lerp alpha from 0 to 1 over messageFadeDuration seconds
            float elapsed = 0f;
            while (elapsed < messageFadeDuration)
            {
                elapsed += Time.deltaTime;
                messageCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / messageFadeDuration);
                yield return null;
            }
            messageCanvasGroup.alpha = 1f;

            //hold at full opacity
            yield return new WaitForSeconds(duration);

            //fade out - same thing in reverse
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
