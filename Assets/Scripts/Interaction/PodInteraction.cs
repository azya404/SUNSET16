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
        [SerializeField] private string day1ToDay2Video = "CutsceneDay2Morning.mp4";
        [Tooltip("Video filename in StreamingAssets to play when sleeping Day 2 Night -> Day 3 Morning.")]
        [SerializeField] private string day2ToDay3Video = "CutsceneDay3Morning.mp4";

        [Header("Prompts")]
        [SerializeField] private string sleepPrompt = "Sleep";
        [SerializeField] private List<string> wrongPhasePrompt = new List<string>();

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

            StartCoroutine(SleepSequence());
        }

        public string GetInteractionPrompt()
        {
            if (DayManager.Instance != null && DayManager.Instance.CurrentPhase != DayPhase.Night)
                return wrongPhasePrompt[Random.Range(0, wrongPhasePrompt.Count)];

            return sleepPrompt;
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

            // fade to black via PodFadeCanvas (Sort Order 11, always active — reliable)
            yield return StartCoroutine(cutscenePlayer.FadeOut());

            // capture day before advancing — needed to pick the right cutscene
            int dayBefore = DayManager.Instance.CurrentDay;

            // advance phase — Night -> next day Morning (gates enforced inside AdvancePhase)
            DayManager.Instance.AdvancePhase();

            // Reset Dialogue
            DialogueUIManager.Instance.ResetDialogue();

            // play sleep cutscene if one is configured for this transition (screen is already black)
            string videoFile = dayBefore == 1 ? day1ToDay2Video
                             : dayBefore == 2 ? day2ToDay3Video
                             : null;
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