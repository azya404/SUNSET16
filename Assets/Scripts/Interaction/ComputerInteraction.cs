/*
the computer terminal in Andy's room - press E to start Albert's dialogue

PREREQUISITE: mirror interaction (pill choice) must be completed before computer is available.
on Start(), if mirror not done: SetInteractionEnabled(false) on InteractionSystem - no prompt,
no trigger, completely suppressed (same pattern as MirrorInteraction disabling itself post-overlay).
when PillStateManager.OnPillTaken fires: re-enable + RefreshPrompt() in case player is already
standing in the trigger zone.

flow when player presses E (mirror done):
  1. screen fades to black  (PillChoiceFade CanvasGroup referenced directly - no duplication)
  2. ComputerCanvas activates, CutscenePanel (Frame 1 - loading video) activates behind black
  3. fade in - Frame 1 revealed, video plays
  4. wait for video to finish naturally (cutsceneDuration used as fallback if no video assigned)
  5. fade to black, swap: CutscenePanel off, OverlayPanel (Frame 2 - chat room bg) on
  6. fade in - Frame 2 revealed, teammate's DialogueUI children are live
  7. ShowDialogue() called - teammate drives from here
  8. teammate calls CloseOverlay() when session ends
  9. fade to black, OverlayPanel off, ComputerCanvas off, fade in, movement unlocked

assign one DialogueSequence per day in the Inspector (index 0 = day 1, index 1 = day 2 etc)

replaces TechDemo/ComputerInteraction.cs which just logged to console and did nothing

TODO: different sequences based on pill state too (not just day)?
TODO: computer screen glow effect when player is in range
*/
using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using SUNSET16.Core;
using SUNSET16.UI;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace SUNSET16.Interaction
{
    public class ComputerInteraction : MonoBehaviour, IInteractable
    {
        [Header("Dialogue Sequences")]
        [Tooltip("One DialogueSequence ScriptableObject per game day (index 0 = Day 1, index 1 = Day 2, ...).")]
        [SerializeField] private DialogueSequence[] daySequences;

        [Header("Computer Canvas")]
        [Tooltip("The parent ComputerCanvas GO. Activated at start of sequence, deactivated at end.")]
        [SerializeField] private GameObject computerCanvas;
        [Tooltip("Frame 1 child - loading video panel. Inactive by default.")]
        [SerializeField] private GameObject cutscenePanel;
        [Tooltip("Frame 2 child - chat room background (computer_overlay_bg). Teammate's DialogueUI lives here. Inactive by default.")]
        [SerializeField] private GameObject overlayPanel;

        [Header("Cutscene Video")]
        [Tooltip("VideoPlayer on CutsceneImage. If unassigned, falls back to cutsceneDuration.")]
        [SerializeField] private VideoPlayer cutsceneVideo;
        [Tooltip("Filename only — file must be in Assets/StreamingAssets/. No path, no subfolders.")]
        [SerializeField] private string cutsceneVideoFileName = "ComputerLoadV4.mp4";
        [Tooltip("Fallback duration in seconds — only used if cutsceneVideo is not assigned.")]
        [SerializeField] private float cutsceneDuration = 2f;

        [Header("Fade")]
        [Tooltip("CanvasGroup on PillChoiceFade - referenced directly, no duplication needed.")]
        [SerializeField] private CanvasGroup fadePanel;
        [SerializeField] private float fadeDuration = 0.5f;

        [Header("Bedroom Content")]
        [Tooltip("GOs to hide while the computer overlay is open. Drag in the root bedroom scene objects (bg, props, player, etc).")]
        [SerializeField] private GameObject[] bedroomContent;

        [Header("Settings")]
        [SerializeField] private string interactionPrompt = "Press E to use computer";
        [SerializeField] private List<string> lockedPrompt = new List<string>();

        private InteractionSystem _interactionSystem;
        private CRTBarrelWarpController _barrelWarp;
        private bool _mirrorCompleted = false;
        private bool _sequenceActive  = false;
        private bool _sequenceCreated = false;
        private bool _endingLocked    = false; // true after night session closes on the ending day
        RuntimeSequence runtimeSequence;

        // --- Lifecycle ---------------------------------------------------------------

        private void Awake()
        {
            _interactionSystem = GetComponent<InteractionSystem>();
            _barrelWarp = GetComponent<CRTBarrelWarpController>();
        }

        private void Start()
        {
            if (PillStateManager.Instance != null)
            {
                // check if mirror was already done before this script started
                _mirrorCompleted = PillStateManager.Instance.HasTakenPillToday();
                PillStateManager.Instance.OnPillTaken += OnPillChoiceMade;

                // if reloading scene after ending day night session already completed → lock immediately
                if (PillStateManager.Instance.IsEndingReached
                    && DayManager.Instance != null
                    && DayManager.Instance.CurrentPhase == DayPhase.Night
                    && DialogueUIManager.Instance != null
                    && DialogueUIManager.Instance.GetFinishedDialogue())
                {
                    _endingLocked = true;
                }
            }
            else
            {
                Debug.LogWarning("[COMPUTER] PillStateManager not found on Start - mirror prerequisite skipped");
            }

            // InteractionSystem stays enabled so the locked hint prompt shows when player enters range
            // Interact() guards against actually running the sequence until mirror is done
        }

        private void OnDestroy()
        {
            if (PillStateManager.Instance != null)
                PillStateManager.Instance.OnPillTaken -= OnPillChoiceMade;
        }

        // --- IInteractable -----------------------------------------------------------

        public void Interact()
        {
            // night session on ending day is complete — computer is sealed
            if (_endingLocked)
            {
                Debug.Log("[COMPUTER] Ending night session complete — computer sealed");
                return;
            }

            // mirror not done - hint prompt is already showing via GetInteractionPrompt(), just block action
            if (!_mirrorCompleted)
            {
                Debug.Log("[COMPUTER] Mirror not completed - computer locked");
                return;
            }

            if (_sequenceActive)
            {
                Debug.LogWarning("[COMPUTER] Sequence already running - ignoring interaction");
                return;
            }

            // dialogue already open, dont stack another on top
            if (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueActive)
            {
                Debug.LogWarning("[COMPUTER] Dialogue already active - ignoring interaction");
                return;
            }

            // DOLOS is talking, not a great time to open Albert's terminal
            if (DOLOSManager.Instance != null && DOLOSManager.Instance.IsAnnouncementActive)
            {
                Debug.LogWarning("[COMPUTER] DOLOS active - ignoring interaction");
                return;
            }

            StartCoroutine(ComputerSequence());
        }

        // returns locked hint when mirror not done, normal prompt when ready
        public string GetInteractionPrompt() => _mirrorCompleted ? interactionPrompt : lockedPrompt[Random.Range(0, lockedPrompt.Count)];

        public bool GetLocked()
        {
            return _mirrorCompleted == false;
        }

        // --- Computer sequence -------------------------------------------------------

        private IEnumerator ComputerSequence()
        {
            _sequenceActive = true;
            if (PlayerController.Instance != null) PlayerController.Instance.LockMovement(true);

            InteractionHotbarController.Instance.characterState(false);

            // fade to black (covers game view)
            yield return StartCoroutine(Fade(0f, 1f));

            // hide bedroom scene behind the fade so it doesn't bleed through
            SetBedroomVisible(false);

            // activate canvas + Frame 1 behind black panel
            if (computerCanvas != null) computerCanvas.SetActive(true);
            if (cutscenePanel  != null) cutscenePanel.SetActive(true);

            // prepare and start video while screen is still black — this guarantees frame 0
            // is written into the RenderTexture before the fade reveals it, preventing the
            // stale last-frame flash on repeat plays.
            // falls back to cutsceneDuration fade-in if no VideoPlayer assigned.
            if (cutsceneVideo != null)
            {
                // reset fully so replays always start from the beginning
                cutsceneVideo.Stop();
                cutsceneVideo.time = 0;

                // StreamingAssets URL must be set at runtime — cannot assign in Inspector for StreamingAssets files
                cutsceneVideo.url = Application.streamingAssetsPath + "/" + cutsceneVideoFileName;
                cutsceneVideo.Prepare();
                yield return new WaitUntil(() => cutsceneVideo.isPrepared);

                // use a named handler so we can unsubscribe after — prevents stacking
                // delegates across multiple computer sessions (would cause early finish on replays)
                bool videoEnded = false;
                VideoPlayer.EventHandler onLoopPoint = null;
                onLoopPoint = _ => videoEnded = true;
                cutsceneVideo.loopPointReached += onLoopPoint;

                cutsceneVideo.Play();
                yield return null; // one frame so frame 0 is written into the RenderTexture

                // NOW fade in — RT shows frame 0, not the stale last frame
                yield return StartCoroutine(Fade(1f, 0f));

                // wait for the rest of the video
                yield return new WaitUntil(() => videoEnded);
                cutsceneVideo.loopPointReached -= onLoopPoint; // clean up delegate
            }
            else
            {
                yield return StartCoroutine(Fade(1f, 0f));
                yield return new WaitForSeconds(cutsceneDuration);
            }

            // fade to black
            yield return StartCoroutine(Fade(0f, 1f));

            // swap: Frame 1 off, Frame 2 on
            if (cutscenePanel != null) cutscenePanel.SetActive(false);
            if (overlayPanel  != null) overlayPanel.SetActive(true);

            _barrelWarp?.SetWarpActive(true);

            // fade in - reveal Frame 2, teammate's UI children are now live
            yield return StartCoroutine(Fade(1f, 0f));

            // hand off to DialogueUIManager
            // if no SOs assigned yet, log warning and leave overlay open - expected during development
            DialogueSequence sequence = GetSequenceForToday();
            if (sequence != null && DialogueUIManager.Instance != null)
            {
                if (!_sequenceCreated)
                {
                    runtimeSequence = CreateRuntimeSequence(sequence);
                    Debug.Log("sequence created!");
                    _sequenceCreated = true;
                }
                DialogueUIManager.Instance.ShowDialogue(runtimeSequence);
            }
            else
                Debug.LogWarning("[COMPUTER] No dialogue sequence for today - overlay open, no dialogue started (expected if SOs not yet assigned)");
        }

        /// <summary>
        /// Called by teammate (or DialogueUIManager) when the computer session ends.
        /// Fades out, hides all computer canvases, fades back in, unlocks movement.
        /// </summary>
        public void CloseOverlay()
        {
            if (!_sequenceActive)
            {
                Debug.LogWarning("[COMPUTER] CloseOverlay called but no sequence active - ignoring");
                return;
            }
            StartCoroutine(CloseSequence());
        }

        private IEnumerator CloseSequence()
        {
            // fade to black
            yield return StartCoroutine(Fade(0f, 1f));

            _barrelWarp?.SetWarpActive(false);

            // hide everything
            if (overlayPanel   != null) overlayPanel.SetActive(false);
            if (computerCanvas != null) computerCanvas.SetActive(false);

            // restore bedroom scene before fading back in
            SetBedroomVisible(true);
            InteractionHotbarController.Instance.characterState(true);

            // fade in - back to game view
            yield return StartCoroutine(Fade(1f, 0f));

            if (PlayerController.Instance != null) PlayerController.Instance.LockMovement(false);
            _sequenceActive = false;

            // ending day night session just closed — seal the computer permanently
            // this is the trigger point: player has had their final Albert conversation,
            // now only the pod (bad ending) or the exit door (good ending) will respond
            if (PillStateManager.Instance != null && PillStateManager.Instance.IsEndingReached
                && DayManager.Instance != null && DayManager.Instance.CurrentPhase == DayPhase.Night
                && DialogueUIManager.Instance != null && DialogueUIManager.Instance.GetFinishedDialogue())
            {
                _endingLocked = true;
                _interactionSystem?.SetInteractionEnabled(false);
                Debug.Log("[COMPUTER] Ending night session complete — computer sealed, ending exit now available");
            }

            if (!DialogueUIManager.Instance.announcementTriggered && DayManager.Instance.CurrentPhase == DayPhase.Morning)
            {
                DialogueUIManager.Instance.announcementTriggered = true;
                DOLOSManager.Instance.TriggerAnnouncement();
            }
        }

        // --- Bedroom visibility ------------------------------------------------------

        private void SetBedroomVisible(bool visible)
        {
            if (bedroomContent == null) return;
            foreach (var go in bedroomContent)
                if (go != null) go.SetActive(visible);
        }

        // --- Fade helper -------------------------------------------------------------

        private IEnumerator Fade(float from, float to)
        {
            if (fadePanel == null)
            {
                Debug.LogWarning("[COMPUTER] FadePanel (PillChoiceFade CanvasGroup) not assigned - skipping fade");
                yield break;
            }
            float elapsed = 0f;
            fadePanel.alpha = from;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadePanel.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
                yield return null;
            }
            fadePanel.alpha = to;
        }

        // --- Mirror prerequisite -----------------------------------------------------

        private void OnPillChoiceMade(int day, PillChoice choice)
        {
            _mirrorCompleted = true;

            // InteractionSystem was never disabled - just refresh the prompt text
            // RefreshPrompt() calls ShowPrompt() -> GetInteractionPrompt() which now returns the real prompt
            // if player is already standing in the trigger zone this updates the text immediately
            _interactionSystem?.RefreshPrompt();

            Debug.Log($"[COMPUTER] Mirror complete (Day {day}: {choice}) - computer now available");
        }


        // --- Internal ----------------------------------------------------------------

        private DialogueSequence GetSequenceForToday()
        {
            if (daySequences == null || daySequences.Length == 0) return null;

            // fallback to first entry if DayManager isnt up yet
            if (DayManager.Instance == null) return daySequences[0];

            int dayOffset = daySequences.Length/4;
            int index = DayManager.Instance.CurrentDay - 1; // day 1 -> index 0
            PillChoice pill = PillStateManager.Instance.GetPillChoice(DayManager.Instance.CurrentDay);
            int pillsTaken = PillStateManager.Instance.GetPillsTakenCount();
            int pillsRefused = PillStateManager.Instance.GetPillsRefusedCount();
            DayPhase phase = DayManager.Instance.CurrentPhase;

            // Override dialogue tree for final day
            if ((DayManager.Instance.CurrentDay == 4) && (phase == DayPhase.Night) && ((pillsRefused == 3) || (pillsTaken == 3)))
                index++;

            Debug.Log("Index before adjustment: " + index);
            if (pill == PillChoice.NotTaken)
                index += dayOffset;
            if (phase == DayPhase.Night)
                index += 2*dayOffset;

            Debug.Log("[DIALOGUE] Day offset: " + dayOffset + ", Index: " + index + ", Pill choice: " + pill + ", Phase: " + phase);
            Debug.Log("[DIALOGUE] Dialogue Tree: " + daySequences[index].sequenceId);

            if (index < 0 || index >= daySequences.Length) return null;

            return daySequences[index];
        }

        private RuntimeSequence CreateRuntimeSequence(DialogueSequence so)
        {
            var runtime = new RuntimeSequence
            {
                sequenceId = so.sequenceId,
                lines      = new System.Collections.Generic.List<RuntimeLine>()
            };

            foreach (DialogueLine dl in so.lines)
            {
                var rl = new RuntimeLine
                {
                    speakerName      = dl.speakerName,
                    portrait         = dl.portrait,
                    text             = dl.text,
                    sendDelay        = dl.sendDelay,
                    delayRepeats     = dl.delayRepeats,
                    autoAdvanceDelay = dl.autoAdvanceDelay,
                    advanceToLine    = dl.advanceToLine,
                    repeat           = dl.repeat,
                    repeated         = false,
                    loreEntry        = dl.loreEntry,
                    switchToDOLOS    = dl.switchToDOLOS,
                    glitch           = dl.glitch
                };

                if (dl.choices != null && dl.choices.Count > 0)
                {
                    rl.choices = new System.Collections.Generic.List<RuntimeChoice>();
                    foreach (DialogueChoice dc in dl.choices)
                    {
                        rl.choices.Add(new RuntimeChoice
                        {
                            choiceText    = dc.choiceText,
                            nextLineIndex = dc.nextLineIndex,
                            offPillChoice = dc.offPillChoice,
                            showOnRepeat  = dc.showOnRepeat
                        });
                    }
                }

                runtime.lines.Add(rl);
            }

            return runtime;
        }
    }
}
