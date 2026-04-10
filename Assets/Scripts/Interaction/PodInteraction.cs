/*
the sleep pod in Andy's bedroom - press E to sleep and advance to the next day

only available during Night phase. if player tries to interact during Morning, Interact() blocks
with a debug log (the InteractionSystem prompt will still show, but no action fires).

on-pill nights (Day 1, Day 3+): DayManager gates are clear after tasks - sleep advances cleanly.
off-pill nights (Day 2+): DayManager gates require hidden room entry + puzzle completion first.
if gates aren't met, AdvancePhase() silently returns and the fade plays but day does not advance.
this is acceptable during development - once puzzle assets and scenes are fully built the gate
will naturally enforce itself without any changes here.

flow when player presses E (Night phase, gates clear):
  1. _isSleeping guard set to true, interaction disabled
  2. screen fades to black via BedroomCutscenePlayer.FadeOut() (PodFadeCanvas Sort Order 11)
  3. DayManager.AdvancePhase() called - day increments, phase set to Morning, events fire
  4. sleep cutscene plays if one is configured for this day transition
  5. RoomManager reloads the bedroom (screen stays black)
  6. brief pause at black (sleepHoldDuration)
  7. screen fades back in via BedroomCutscenePlayer.FadeIn()
  8. _isSleeping guard cleared, interaction re-enabled

TODO: add sleep sound effect or ambient audio crossfade before fade
TODO: night computer session requirement before sleep (design not finalised yet)
TODO: different prompts/behaviour based on pill state (off-pill insomnia flavour text?)
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SUNSET16.Core;
using SUNSET16.UI;

namespace SUNSET16.Interaction
{
    public class PodInteraction : MonoBehaviour, IInteractable
    {
        [Header("Timing")]
        [Tooltip("How long the screen holds at black between fade out and fade in.")]
        [SerializeField] private float sleepHoldDuration = 0.5f;

        [Header("Sleep Cutscenes")]
        [Tooltip("BedroomCutscenePlayer GO in BedroomScene — shared with MirrorInteraction.")]
        [SerializeField] private BedroomCutscenePlayer cutscenePlayer;
        [Tooltip("Video filename in StreamingAssets to play when sleeping Day 1 Night -> Day 2 Morning.")]
        [SerializeField] private string day1ToDay2Video    = "CutsceneDay2Morning.mp4";
        [Tooltip("Video filename in StreamingAssets to play when sleeping Day 2 Night -> Day 3 Morning.")]
        [SerializeField] private string day2ToDay3Video    = "CutsceneDay3Morning.mp4";
        [Tooltip("Day 3 Night -> Day 4 Morning — player took pill on Day 3.")]
        [SerializeField] private string day3TookVideo      = "CutsceneDay4Morning.mp4";
        [Tooltip("Day 3 Night -> Day 4 Morning — player refused pill on Day 3.")]
        [SerializeField] private string day3RefusedVideo   = "CutsceneDay4Reveal.mp4";
        [Tooltip("Day 4 Night -> Day 5 Morning — player took pill on Day 4.")]
        [SerializeField] private string day4TookVideo      = "CutsceneDay5Morning.mp4";
        [Tooltip("Day 4 Night -> Day 5 Morning — player refused pill on Day 4.")]
        [SerializeField] private string day4RefusedVideo   = "CutsceneDay5Reveal.mp4";

        [Header("Prompts")]
        [SerializeField] private string sleepPrompt = "Sleep";
        [SerializeField] private List<string> wrongPhasePrompt = new List<string>();
        [SerializeField] private List<string> noNightChatPrompt = new List<string>();
        [SerializeField] private List<string> goodEnding = new List<string>();

        private InteractionSystem _interactionSystem;
        private bool _isSleeping;

        // --- Lifecycle ---------------------------------------------------------------

        private void Awake()
        {
            _interactionSystem = GetComponent<InteractionSystem>();
        }

        // --- IInteractable -----------------------------------------------------------

        public void Interact()
        {
            if (_isSleeping)
            {
                Debug.LogWarning("[POD] Sleep sequence already running - ignoring interaction");
                return;
            }

            if (DayManager.Instance == null)
            {
                Debug.LogWarning("[POD] DayManager not found - cannot advance phase");
                return;
            }

            if (DayManager.Instance.CurrentPhase != DayPhase.Night)
            {
                Debug.Log("[POD] Not night phase - sleep blocked");
                return;
            }

            if (DayManager.Instance != null && DayManager.Instance.CurrentPhase == DayPhase.Night && DialogueUIManager.Instance != null && !DialogueUIManager.Instance.HasCompletedTodayNightSequence)
            {
                Debug.Log("[POD] Night chat not completed - sleep blocked");
                return;
            }

            if (DayManager.Instance != null && PillStateManager.Instance != null && DayManager.Instance.CurrentPhase == DayPhase.Night && PillStateManager.Instance.GetPillsRefusedCount() == 3)
            {
                Debug.Log("[POD] Good ending achieved - sleep blocked");
                return;
            }

            StartCoroutine(SleepSequence());
        }

        public string GetInteractionPrompt()
        {
            if (DayManager.Instance != null && DayManager.Instance.CurrentPhase != DayPhase.Night)
                return wrongPhasePrompt[Random.Range(0, wrongPhasePrompt.Count)];

            if (DayManager.Instance != null && DayManager.Instance.CurrentPhase == DayPhase.Night && DialogueUIManager.Instance != null && !DialogueUIManager.Instance.HasCompletedTodayNightSequence)
                return noNightChatPrompt[Random.Range(0, noNightChatPrompt.Count)];

            if (DayManager.Instance != null && PillStateManager.Instance != null && DayManager.Instance.CurrentPhase == DayPhase.Night && PillStateManager.Instance.GetPillsRefusedCount() == 3)
                return goodEnding[Random.Range(0, goodEnding.Count)];

            return sleepPrompt;
        }

        public bool GetLocked()
        {
            if (DayManager.Instance != null && PillStateManager.Instance != null && DayManager.Instance.CurrentPhase == DayPhase.Night && PillStateManager.Instance.GetPillsRefusedCount() == 3)
                return true;
            else if (DayManager.Instance.CurrentPhase == DayPhase.Night && DialogueUIManager.Instance != null)
                return DialogueUIManager.Instance.HasCompletedTodayNightSequence == false;
            else
                return DayManager.Instance.CurrentPhase != DayPhase.Night;
        }

        // --- Sleep Sequence ----------------------------------------------------------

        private IEnumerator SleepSequence()
        {
            _isSleeping = true;

            if (_interactionSystem != null)
                _interactionSystem.SetInteractionEnabled(false);

            if (cutscenePlayer == null)
            {
                Debug.LogError("[POD] cutscenePlayer not assigned — sleep sequence aborted. Assign BedroomCutscenePlayer in Inspector.");
                _isSleeping = false;
                if (_interactionSystem != null) _interactionSystem.SetInteractionEnabled(true);
                yield break;
            }

            // stop music immediately so cutscene audio is not competing with background tracks
            AudioManager.Instance?.StopMusicImmediate();

            // fade to black via PodFadeCanvas (Sort Order 11, always active — reliable)
            yield return StartCoroutine(cutscenePlayer.FadeOut());

            // capture day before advancing — needed to pick the right cutscene
            int dayBefore = DayManager.Instance.CurrentDay;

            // advance phase — Night -> next day Morning (gates enforced inside AdvancePhase)
            DayManager.Instance.AdvancePhase();

            // Reset Dialogue
            DialogueUIManager.Instance.ResetDialogue();

            // pick cutscene based on day and pill choices
            // days 1-2 are fixed; days 3-4 branch on the choice made that day
            // day 4->5 also needs day 3's choice to resolve the correct branch
            string videoFile = null;
            if (PillStateManager.Instance != null)
            {
                PillChoice todayChoice = PillStateManager.Instance.GetPillChoice(dayBefore);
                if (dayBefore == 1)
                {
                    videoFile = day1ToDay2Video;
                }
                else if (dayBefore == 2)
                {
                    videoFile = day2ToDay3Video;
                }
                else if (dayBefore == 3)
                {
                    videoFile = todayChoice == PillChoice.Taken ? day3TookVideo : day3RefusedVideo;
                }
                else if (dayBefore == 4)
                {
                    PillChoice day3Choice = PillStateManager.Instance.GetPillChoice(3);
                    bool day3Took = day3Choice == PillChoice.Taken;
                    bool day4Took = todayChoice == PillChoice.Taken;

                    // same choice both days = ending reached, no transition cutscene
                    if (day3Took && day4Took)   videoFile = null; // Bad Ending Day 4
                    else if (!day3Took && !day4Took) videoFile = null; // Good Ending Day 4
                    else if (day3Took && !day4Took)  videoFile = day4RefusedVideo;  // P then N -> Reveal Day 5
                    else                             videoFile = day4TookVideo;     // N then P -> Morning Day 5
                }
            }
            if (!string.IsNullOrEmpty(videoFile))
                yield return StartCoroutine(cutscenePlayer.PlayVideo(videoFile));

            // reload bedroom — scene refreshes while screen stays black
            RoomManager.Instance.LoadRoom(RoomManager.Instance.GetCurrentRoomName());

            // hold at black so the room finishes loading
            yield return new WaitForSeconds(sleepHoldDuration);

            // fade back in to reveal the new morning
            yield return StartCoroutine(cutscenePlayer.FadeIn());

            _isSleeping = false;

            if (_interactionSystem != null)
                _interactionSystem.SetInteractionEnabled(true);
        }
    }
}