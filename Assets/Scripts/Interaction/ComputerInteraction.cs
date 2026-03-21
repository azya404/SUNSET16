/*
the computer terminal in Andy's room - press E to start Albert's dialogue

PREREQUISITE: mirror interaction (pill choice) must be completed before computer is available.
on Start(), if mirror not done: SetInteractionEnabled(false) on InteractionSystem - no prompt,
no trigger, completely suppressed (same pattern as MirrorInteraction disabling itself post-overlay).
when PillStateManager.OnPillTaken fires: re-enable + RefreshPrompt() in case player is already
standing in the trigger zone.

flow when player presses E (mirror done):
  1. screen fades to black  (PillChoiceFade CanvasGroup referenced directly - no duplication)
  2. ComputerCanvas activates, CutscenePanel (Frame 1 - deskview) activates behind black
  3. fade in - Frame 1 revealed, player movement locked
  4. hold cutsceneDuration seconds
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
using SUNSET16.Core;
using SUNSET16.UI;
using UnityEngine.UIElements;

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
        [Tooltip("Frame 1 child - static deskview image (albert_deskview_cutscene). Inactive by default.")]
        [SerializeField] private GameObject cutscenePanel;
        [Tooltip("Frame 2 child - chat room background (computer_overlay_bg). Teammate's DialogueUI lives here. Inactive by default.")]
        [SerializeField] private GameObject overlayPanel;

        [Header("Fade")]
        [Tooltip("CanvasGroup on PillChoiceFade - referenced directly, no duplication needed.")]
        [SerializeField] private CanvasGroup fadePanel;
        [SerializeField] private float fadeDuration = 0.5f;

        [Header("Timing")]
        [SerializeField] private float cutsceneDuration = 2f;

        [Header("Settings")]
        [SerializeField] private string interactionPrompt = "Press E to use computer";
        [SerializeField] private string lockedPrompt = "Maybe I should check the mirror first...";

        private InteractionSystem _interactionSystem;
        private CRTBarrelWarpController _barrelWarp;
        private bool _mirrorCompleted = false;
        private bool _sequenceActive  = false;
        private bool _sequenceCreated = false;
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
        public string GetInteractionPrompt() => _mirrorCompleted ? interactionPrompt : lockedPrompt;

        // --- Computer sequence -------------------------------------------------------

        private IEnumerator ComputerSequence()
        {
            _sequenceActive = true;
            if (PlayerController.Instance != null) PlayerController.Instance.LockMovement(true);

            // fade to black (covers game view)
            yield return StartCoroutine(Fade(0f, 1f));

            // activate canvas + Frame 1 behind black panel
            if (computerCanvas != null) computerCanvas.SetActive(true);
            if (cutscenePanel  != null) cutscenePanel.SetActive(true);

            // fade in - reveal Frame 1
            yield return StartCoroutine(Fade(1f, 0f));

            // hold deskview for set duration
            yield return new WaitForSeconds(cutsceneDuration);

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

            // hide everything
            _barrelWarp?.SetWarpActive(false);
            if (overlayPanel   != null) overlayPanel.SetActive(false);
            if (computerCanvas != null) computerCanvas.SetActive(false);

            // fade in - back to game view
            yield return StartCoroutine(Fade(1f, 0f));

            if (PlayerController.Instance != null) PlayerController.Instance.LockMovement(false);
            _sequenceActive = false;
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
            DayPhase phase = DayManager.Instance.CurrentPhase;

            if (pill == PillChoice.NotTaken)
                index += dayOffset;
            if (phase == DayPhase.Night)
                index += 2*dayOffset;
                
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
                    repeated         = false
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
