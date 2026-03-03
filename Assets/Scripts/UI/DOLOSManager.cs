/*
DOLOS - the ship's public address system
makes announcements that play over everything else on the ship

one-directional by design: the player can still move around during an announcement
but they cant interact with objects, open doors, open the map, or start albert dialogue
while its active. InteractionSystem and DoorController both check IsAnnouncementActive
before doing anything

only one announcement at a time - if something tries to trigger a new one while another
is already playing it just gets silently dropped. no queue, no interruption
DOLOS also wont fire if albert dialogue is open - theyre mutually exclusive so they
dont step on each other

settings queue: PauseMenuController can call QueueSettings() if the player tries to
open settings mid-announcement. DOLOS will fire OnSettingsRequested the moment it
finishes so the menu opens naturally right after, instead of interrupting or being lost

TODO: actual audio playback - announcementAudioSource is wired up but the clips
arent in yet, itll just show text for now
TODO: visual panel for the announcement text - panel exists but needs art
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using SUNSET16.Core;

namespace SUNSET16.UI
{
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

        public event Action<string> OnAnnouncementStarted;
        public event Action<string> OnAnnouncementEnded;

        //PauseMenuController subscribes to this so it knows when its safe to open settings
        public event Action OnSettingsRequested;

        // ─── Lifecycle ────────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            if (announcementPanel != null)
                announcementPanel.SetActive(false);
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        public void TriggerAnnouncement(DOLOSAnnouncement announcement)
        {
            if (announcement == null)
            {
                Debug.LogWarning("[DOLOS] Cannot trigger null announcement.");
                return;
            }

            //already running - drop the new one, dont interrupt
            if (IsAnnouncementActive)
            {
                Debug.Log($"[DOLOS] Already active — dropping '{announcement.announcementId}'");
                return;
            }

            //albert takes exclusive attention priority - dont talk over him
            if (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueActive)
            {
                Debug.Log($"[DOLOS] Albert dialogue active — dropping '{announcement.announcementId}'");
                return;
            }

            _announcementCoroutine = StartCoroutine(PlayAnnouncement(announcement));
        }

        public void StopAnnouncement()
        {
            if (_announcementCoroutine != null)
            {
                StopCoroutine(_announcementCoroutine);
                _announcementCoroutine = null;
            }

            FinishAnnouncement(null);
        }

        public void QueueSettings()
        {
            //only meaningful if DOLOS is actually speaking right now
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

            //honor any settings request that came in while we were speaking
            if (_settingsQueued)
            {
                _settingsQueued = false;
                OnSettingsRequested?.Invoke();
                Debug.Log("[DOLOS] Honoring queued settings request");
            }
        }
    }
}

