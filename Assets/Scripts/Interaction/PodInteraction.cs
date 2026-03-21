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
  2. screen fades to black (PillChoiceFade CanvasGroup - same panel as MirrorInteraction/ComputerInteraction)
  3. DayManager.AdvancePhase() called - day increments, phase set to Morning, events fire
  4. brief pause at black (sleepHoldDuration)
  5. screen fades back in - player wakes up in bedroom on new morning
  6. _isSleeping guard cleared, interaction re-enabled

TODO: add sleep sound effect or ambient audio crossfade before fade
TODO: night computer session requirement before sleep (design not finalised yet)
TODO: different prompts/behaviour based on pill state (off-pill insomnia flavour text?)
*/
using System.Collections;
using UnityEngine;
using SUNSET16.Core;

namespace SUNSET16.Interaction
{
    public class PodInteraction : MonoBehaviour, IInteractable
    {
        [Header("Fade")]
        [Tooltip("CanvasGroup on PillChoiceFade - same panel used by MirrorInteraction and ComputerInteraction. Assign in Inspector.")]
        [SerializeField] private CanvasGroup fadePanel;
        [SerializeField] private float fadeDuration = 1f;

        [Header("Timing")]
        [Tooltip("How long the screen holds at black between fade out and fade in.")]
        [SerializeField] private float sleepHoldDuration = 0.5f;

        [Header("Prompts")]
        [SerializeField] private string sleepPrompt = "Sleep";
        [SerializeField] private string wrongPhasePrompt = "I should wait until tonight...";

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
                return wrongPhasePrompt;

            return sleepPrompt;
        }

        // --- Sleep Sequence ----------------------------------------------------------

        private IEnumerator SleepSequence()
        {
            _isSleeping = true;

            if (_interactionSystem != null)
                _interactionSystem.SetInteractionEnabled(false);

            // fade to black
            yield return StartCoroutine(Fade(0f, 1f));

            // advance phase - Night -> next day Morning (gates enforced inside AdvancePhase)
            DayManager.Instance.AdvancePhase();

            // hold at black
            yield return new WaitForSeconds(sleepHoldDuration);

            // fade back in
            yield return StartCoroutine(Fade(1f, 0f));

            _isSleeping = false;

            if (_interactionSystem != null)
                _interactionSystem.SetInteractionEnabled(true);
        }

        private IEnumerator Fade(float from, float to)
        {
            if (fadePanel == null)
            {
                Debug.LogWarning("[POD] fadePanel not assigned - skipping fade");
                yield break;
            }

            fadePanel.gameObject.SetActive(true);
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadePanel.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
                yield return null;
            }

            fadePanel.alpha = to;

            if (to == 0f)
                fadePanel.gameObject.SetActive(false);
        }
    }
}