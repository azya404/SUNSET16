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
using System.Collections.Generic;
using SUNSET16.Core;

namespace SUNSET16.UI
{
    public class DOLOSManager : Singleton<DOLOSManager>
    {
        [Header("Announcement Display")]
        [SerializeField] private GameObject  announcementPanel;
        [SerializeField] private TMP_Text    announcementText;
        [SerializeField] private TMP_Text    skipText;
        [SerializeField] private Image       announcementGradient;
        [SerializeField] private Image       speakerIcon;          // Optional — DOLOS icon/portrait

        [Header("Audio")]
        [SerializeField] private AudioSource announcementAudioSource;

        [Header("Announcements")]
        [SerializeField] private DOLOSAnnouncement[] announcements;

        [SerializeField] private string testAnnouncementId;

        // ─── State ────────────────────────────────────────────────────────────────

        public bool IsAnnouncementActive { get; private set; }

        private bool      _settingsQueued = false;
        private Coroutine _announcementCoroutine;

        private Dictionary<string, DOLOSAnnouncement> _announcementsDict = new Dictionary<string, DOLOSAnnouncement>();
        private DOLOSAnnouncement _currentAnnouncement;
        private bool _showingLines;
        private bool _linesSkipped;
        private Coroutine _linesCoroutine;
        private string _fullText;
        public int taskCompleteCount = 0;
        private AudioClip _tempClip;
        private bool _hasAnnouncedComputer = false;

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
            
            Debug.Log("Populating DOLOS dictionary");
            foreach (DOLOSAnnouncement announcement in announcements)
            {
                string id = announcement.announcementId;
                _announcementsDict[id] = announcement;
                Debug.Log("Loaded " + id + " into the dictionary");
            }
        }

        private void Update()
        {
            if (!IsAnnouncementActive) return;

            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (!_linesSkipped)
                {
                    //null guard: _linesCoroutine is null if announcementText ref was missing in Inspector
                    if (_linesCoroutine != null) StopCoroutine(_linesCoroutine);
                    if (announcementText != null) announcementText.text = _fullText;
                    if (skipText != null) skipText.text = "[Press Space to close]";
                    _linesSkipped = true;
                }
                else
                    StopAnnouncement();
            }
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        public void TriggerAnnouncement()
        {
            DetermineAnnouncement();
            if (_currentAnnouncement == null)
            {
                Debug.LogWarning("[DOLOS] Cannot trigger null announcement.");
                return;
            }

            //already running - drop the new one, dont interrupt
            if (IsAnnouncementActive)
            {
                Debug.Log($"[DOLOS] Already active — dropping '{_currentAnnouncement.announcementId}'");
                return;
            }

            //albert takes exclusive attention priority - dont talk over him
            if (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueActive)
            {
                Debug.Log($"[DOLOS] Albert dialogue active — dropping '{_currentAnnouncement.announcementId}'");
                return;
            }

            _announcementCoroutine = StartCoroutine(PlayAnnouncement(_currentAnnouncement));
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

        public void DetermineAnnouncement()
        {
            string roomName = RoomManager.Instance.GetCurrentRoomName();
            int day = DayManager.Instance.CurrentDay;
            DayPhase phase = DayManager.Instance.CurrentPhase;
            bool takenPill = PillStateManager.Instance.HasTakenPillToday();
            bool hasChatted = DialogueUIManager.Instance.announcementTriggered;
            string id = "";

            // Naming convention for announcementID: dolos_day[#]_[phase]_[event]
            // Example: dolos_day1_morning_wakeup
            if (roomName.Contains("Bedroom"))
            {
                if (phase == DayPhase.Morning)
                {
                    if (takenPill)
                    {
                        if (hasChatted)
                        {
                            // Play announcement after chatting with Albert
                            id = "dolos_day" + day + "_morning_computer";
                            _hasAnnouncedComputer = true;
                        }
                        else
                        {
                            // Play announcement after taking pill
                            id = "dolos_day" + day + "_morning_mirror";
                        }
                    }
                    else
                    {
                        // Play morning announcement
                        id = "dolos_day" + day + "_morning_wakeup";
                    }
                }
                else
                {
                    // Play night announcement
                    id = "dolos_day" + day + "_night_bedroom";
                }
            }
            else
            {
                if (taskCompleteCount == 2)
                {
                    // Play announcement for completing all tasks
                    id = "dolos_day" + day + "_task2";
                    taskCompleteCount = 0;
                }
                else
                {
                    // Play announcement for completing one task
                    id = "dolos_day" + day + "_task1";
                }
            }

            if (id != "")
                _currentAnnouncement = _announcementsDict[id];
            else
            {
                Debug.Log("[DOLOS] DOLOS Announcement not set");
                _currentAnnouncement = null;
            }
        }

        // ─── Internal ─────────────────────────────────────────────────────────────

        private IEnumerator PlayAnnouncement(DOLOSAnnouncement announcement)
        {
            IsAnnouncementActive = true;

            if (announcementText  != null) 
            {
                announcementText.fontSize = announcement.fontSize;
                skipText.text = "[Press Space to skip]";
                _fullText = announcement.text;
                _linesCoroutine = StartCoroutine(LineByLine(announcement));
            }
            if (announcementPanel != null) announcementPanel.SetActive(true);

            if (announcementAudioSource != null && announcement.audioClip != null)
            {
                // This deals with the playing of the announcement. If you want to send off the clip to the audio mixer you
                // could probably do that here along with the audio clip's intended volume?
                _tempClip = announcement.audioClip;
                // Send off temp clip to the audio mixer? Use announcement.volume to take value from slider in the DOLOS announcement object
                announcementAudioSource.clip = _tempClip;
                announcementAudioSource.volume = announcement.volume; // This will need to be removed when the audio mixer is implemented
                announcementAudioSource.Play();
            }

            //use CrossFadeAlpha(0,0,true) to instantly set alpha to 0 — avoids deprecated
            //canvasRenderer.SetAlpha which can leave renderer-level alpha stuck at 0
            //null guards here so a missing Inspector ref doesnt crash and lock IsAnnouncementActive
            announcementGradient?.CrossFadeAlpha(0f, 0f, true);
            if (announcementText != null) announcementText.CrossFadeAlpha(0f, 0f, true);
            skipText?.CrossFadeAlpha(0f, 0f, true);

            yield return null;

            announcementGradient?.CrossFadeAlpha(1.0f, 2f, false);
            if (announcementText != null) announcementText.CrossFadeAlpha(1.0f, 2f, false);
            skipText?.CrossFadeAlpha(1.0f, 2f, false);

            Debug.Log($"[DOLOS] Playing '{announcement.announcementId}': {announcement.text}");
            OnAnnouncementStarted?.Invoke(announcement.announcementId);

            yield return new WaitForSeconds(announcement.displayDuration);

            FinishAnnouncement(announcement.announcementId);
        }
        
        private IEnumerator LineByLine(DOLOSAnnouncement announcement)
        {
            Debug.Log("[DOLOS] Playing Line by Line");
            _showingLines = true;
            _linesSkipped = false;
            announcementText.text = "";
            int linesShown = 0;

            foreach(char c in announcement.text)
            {
                announcementText.text += c;

                if (c == '\n')
                {
                    Debug.Log("[DOLOS] Announement text: " + announcementText.text);
                    yield return new WaitForSeconds(announcement.lineDurations[linesShown]);
                    linesShown++;
                    announcementText.text = "";
                }
            }
            _showingLines = false;
        }

        private IEnumerator FadeOut()
        {
            announcementGradient.CrossFadeAlpha(0f, 2, false);
            announcementText.CrossFadeAlpha(0f, 2, false);
            skipText.CrossFadeAlpha(0f, 2, false);

            yield return new WaitForSeconds(2);

            if (announcementPanel != null) announcementPanel.SetActive(false);
            if (announcementText  != null) announcementText.text = "";
        }

        private void FinishAnnouncement(string announcementId)
        {
            IsAnnouncementActive = false;

            StartCoroutine(FadeOut());

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

        [ContextMenu("TestVoiceline")]
        public void TestVoiceline()
        {
            _announcementCoroutine = StartCoroutine(PlayAnnouncement(_announcementsDict[testAnnouncementId]));
        }
    }
}
