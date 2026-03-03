using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using SUNSET16.Core;

namespace SUNSET16.UI
{
    /// <summary>
    /// DOLOS — the ship's public address system.
    ///
    /// DOLOS makes one-directional announcements (audio + text). The player can move
    /// freely during an announcement, but the following are locked for its duration:
    ///   • Scene transitions (DoorController checks IsAnnouncementActive)
    ///   • Object interactions (InteractionSystem checks IsAnnouncementActive)
    ///   • Map overlay (MapUIController checks IsAnnouncementActive)
    ///   • Albert dialogue (DialogueUIManager checks IsAnnouncementActive before starting)
    ///
    /// Only one announcement plays at a time. New triggers are silently dropped if active.
    /// DOLOS will not fire if Albert dialogue is currently active.
    ///
    /// Settings queue: if the player presses a queued settings action while DOLOS is
    /// speaking, OnSettingsRequested fires when the announcement ends.
    ///
    /// Lives in CoreScene (DontDestroyOnLoad via Singleton).
    /// </summary>
    public class DOLOSManager : Singleton<DOLOSManager>
    {
        [Header("Announcement Display")]
        [SerializeField] private GameObject  announcementPanel;
        [SerializeField] private TMP_Text    announcementText;
        [SerializeField] private Image       speakerIcon;          // Optional — DOLOS icon/portrait

        [Header("Audio")]
        [SerializeField] private AudioSource announcementAudioSource;

        // ─── State ────────────────────────────────────────────────────────────────

        public bool IsAnnouncementActive { get; private set; }

        private bool      _settingsQueued = false;
        private Coroutine _announcementCoroutine;

        // ─── Events ───────────────────────────────────────────────────────────────

        /// <summary>Fires when an announcement begins playing.</summary>
        public event Action<string> OnAnnouncementStarted;

        /// <summary>Fires when an announcement finishes. Delivers the announcementId.</summary>
        public event Action<string> OnAnnouncementEnded;

        /// <summary>
        /// Fires at end of announcement if the player queued a settings-open while DOLOS was active.
        /// PauseMenuController should subscribe to open settings on this event.
        /// </summary>
        public event Action OnSettingsRequested;

        // ─── Lifecycle ────────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            if (announcementPanel != null)
                announcementPanel.SetActive(false);
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Attempt to play an announcement. Silently dropped if:
        ///   • Another announcement is already active.
        ///   • Albert dialogue is currently active.
        /// </summary>
        public void TriggerAnnouncement(DOLOSAnnouncement announcement)
        {
            if (announcement == null)
            {
                Debug.LogWarning("[DOLOS] Cannot trigger null announcement.");
                return;
            }

            if (IsAnnouncementActive)
            {
                Debug.Log($"[DOLOS] Already active — dropping '{announcement.announcementId}'");
                return;
            }

            // Albert takes exclusive audio/attention priority
            if (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueActive)
            {
                Debug.Log($"[DOLOS] Albert dialogue active — dropping '{announcement.announcementId}'");
                return;
            }

            _announcementCoroutine = StartCoroutine(PlayAnnouncement(announcement));
        }

        /// <summary>
        /// Immediately stop any active announcement (e.g. for scene changes or game-over).
        /// </summary>
        public void StopAnnouncement()
        {
            if (_announcementCoroutine != null)
            {
                StopCoroutine(_announcementCoroutine);
                _announcementCoroutine = null;
            }

            FinishAnnouncement(null);
        }

        /// <summary>
        /// Call when the player attempts to open settings while DOLOS is speaking.
        /// Settings will be opened automatically once the announcement ends.
        /// </summary>
        public void QueueSettings()
        {
            if (IsAnnouncementActive)
                _settingsQueued = true;
        }

        // ─── Internal ─────────────────────────────────────────────────────────────

        private IEnumerator PlayAnnouncement(DOLOSAnnouncement announcement)
        {
            IsAnnouncementActive = true;

            if (announcementText  != null) announcementText.text = announcement.text;
            if (announcementPanel != null) announcementPanel.SetActive(true);

            if (announcementAudioSource != null && announcement.audioClip != null)
            {
                announcementAudioSource.clip = announcement.audioClip;
                announcementAudioSource.Play();
            }

            Debug.Log($"[DOLOS] Playing '{announcement.announcementId}': {announcement.text}");
            OnAnnouncementStarted?.Invoke(announcement.announcementId);

            yield return new WaitForSeconds(announcement.displayDuration);

            FinishAnnouncement(announcement.announcementId);
        }

        private void FinishAnnouncement(string announcementId)
        {
            IsAnnouncementActive = false;

            if (announcementPanel != null) announcementPanel.SetActive(false);
            if (announcementText  != null) announcementText.text = "";

            if (announcementAudioSource != null && announcementAudioSource.isPlaying)
                announcementAudioSource.Stop();

            if (announcementId != null)
            {
                OnAnnouncementEnded?.Invoke(announcementId);
                Debug.Log($"[DOLOS] Finished '{announcementId}'");
            }

            // Honor queued settings request
            if (_settingsQueued)
            {
                _settingsQueued = false;
                OnSettingsRequested?.Invoke();
                Debug.Log("[DOLOS] Honoring queued settings request");
            }
        }
    }
}
