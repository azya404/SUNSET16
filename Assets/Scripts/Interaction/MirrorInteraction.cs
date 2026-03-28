/*
bathroom mirror interaction — player presses E to decide pill choice
shows animated overlay, player clicks Consume Pill or Conceal Pill

PROXIMITY AUDIO
  on enter range  → bedroom ambient softens to ambientSoftenVolume
  on exit range   → bedroom ambient restores (if overlay not active)

OVERLAY OPEN
  bedroom ambient fades to 0 then pauses
  mirror ambient fades in from 0

BUTTON CLICK SEQUENCE
  1. disable buttons (prevent double-click)
  2. screen fades to black + mirror ambient fades out (parallel)
  3. pill SFX fades in on the black screen
  4. SFX plays at full volume
  5. SFX fades out + screen fades back in (parallel)
  6. record pill choice (fires OnPillTaken → AudioManager crossfades music)
  7. close overlay + resume bedroom ambient with fade in
  8. disable interaction prompt (choice made for today)

INTERACTION PROMPT
  disabled after choice — re-enabled when DayManager fires Morning phase

DOLOS and movement-lock blocking handled upstream by InteractionSystem

replaces TechDemo/MirrorInteraction.cs
*/
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SUNSET16.Core;
using SUNSET16.UI;

namespace SUNSET16.Interaction
{
    public class MirrorInteraction : MonoBehaviour, IInteractable
    {
        [Header("UI References")]
        [Tooltip("Canvas or panel containing the Consume Pill / Conceal Pill buttons.")]
        [SerializeField] private GameObject mirrorOverlayCanvas;
        [Tooltip("CanvasGroup on the PillChoiceFade child GO — fades to black on button click.")]
        [SerializeField] private CanvasGroup pillChoiceFade;
        [Tooltip("The Consume Pill button — hidden on Day 2 (forced refuse day).")]
        [SerializeField] private Button takePillButton;
        [Tooltip("The Conceal Pill button — hidden on Day 1 (forced take day).")]
        [SerializeField] private Button concealPillButton;

        [Header("SFX")]
        [Tooltip("Sound played when the player takes the pill.")]
        [SerializeField] private AudioClip pillTakeSFX;
        [Tooltip("Sound played when the player hides the pill.")]
        [SerializeField] private AudioClip pillHideSFX;

        [Header("Screen Fade Settings")]
        [SerializeField] private float fadeOutDuration = 0.8f;
        [SerializeField] private float fadeInDuration  = 1.2f;

        [Header("SFX Fade Settings")]
        [Tooltip("How long the pill SFX fades in after the screen goes black.")]
        [SerializeField] private float sfxFadeInDuration  = 0.4f;
        [Tooltip("How long the pill SFX fades out (synced with screen fade-in).")]
        [SerializeField] private float sfxFadeOutDuration = 0.6f;

        [Header("Mirror Ambient")]
        [Tooltip("How long the mirror ambient fades in when the overlay opens.")]
        [SerializeField] private float mirrorAmbientFadeInDuration = 0.6f;

        [Header("Proximity Audio")]
        [Tooltip("Volume bedroom ambient softens to when player is near the mirror.")]
        [SerializeField] private float ambientSoftenVolume   = 0.3f;
        [Tooltip("Duration of the ambient soften/restore fade.")]
        [SerializeField] private float ambientSoftenDuration = 1.0f;

        [Header("Settings")]
        [SerializeField] private string interactionPrompt = "Press E to look in mirror";

        private bool              _isOverlayActive    = false;
        private InteractionSystem _interactionSystem;

        // ─── Lifecycle ────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (mirrorOverlayCanvas != null)
                mirrorOverlayCanvas.SetActive(false);

            _interactionSystem = GetComponent<InteractionSystem>();
        }

        private void Start()
        {
            //subscribe to day changes so we can re-enable the prompt on a new morning
            if (DayManager.Instance != null)
                DayManager.Instance.OnPhaseChanged += OnDayPhaseChanged;
        }

        private void OnDestroy()
        {
            if (DayManager.Instance != null)
                DayManager.Instance.OnPhaseChanged -= OnDayPhaseChanged;
        }

        // ─── Proximity Audio ──────────────────────────────────────────────────────

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            //only soften if the player hasnt made their choice yet
            if (PillStateManager.Instance != null && PillStateManager.Instance.HasTakenPillToday()) return;
            AudioManager.Instance?.SoftenAmbient(ambientSoftenVolume, ambientSoftenDuration);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            //dont restore if overlay is active - the overlay handles ambient itself
            if (!_isOverlayActive)
                AudioManager.Instance?.RestoreAmbient(ambientSoftenDuration);
        }

        // ─── IInteractable ────────────────────────────────────────────────────────

        public void Interact()
        {
            if (_isOverlayActive)
            {
                Debug.LogWarning("[MIRROR] Overlay already active — ignoring");
                return;
            }

            if (PillStateManager.Instance != null && PillStateManager.Instance.HasTakenPillToday())
            {
                Debug.Log("[MIRROR] Pill choice already made today — interaction skipped");
                return;
            }

            ShowOverlay();
        }

        public string GetInteractionPrompt() => interactionPrompt;

        // ─── Button Callbacks (wired via Inspector OnClick) ───────────────────────

        public void OnTakePillClicked()
        {
            StartCoroutine(PillChoiceSequence(PillChoice.Taken));
        }

        public void OnHidePillClicked()
        {
            StartCoroutine(PillChoiceSequence(PillChoice.NotTaken));
        }

        // ─── Pill Choice Sequence ─────────────────────────────────────────────────

        private IEnumerator PillChoiceSequence(PillChoice choice)
        {
            SetButtonsInteractable(false);

            // 1. screen fades to black AND mirror ambient fades out simultaneously
            AudioManager.Instance?.FadeMirrorAmbientOut(fadeOutDuration); //fire and forget
            yield return StartCoroutine(FadePillOverlay(0f, 1f, fadeOutDuration));

            // 2. pill SFX fades in on the black screen
            AudioClip clip = (choice == PillChoice.Taken) ? pillTakeSFX : pillHideSFX;
            if (clip != null)
            {
                float fullVolumeDuration = Mathf.Max(0f, clip.length - sfxFadeInDuration - sfxFadeOutDuration);

                AudioManager.Instance?.PlayPillSFX(clip);
                AudioManager.Instance?.FadePillSFXIn(sfxFadeInDuration); //fire and forget
                yield return new WaitForSeconds(sfxFadeInDuration + fullVolumeDuration);

                // 3. SFX fades out AND screen fades back in simultaneously
                AudioManager.Instance?.FadePillSFXOut(sfxFadeOutDuration); //fire and forget
                yield return StartCoroutine(FadePillOverlay(1f, 0f, fadeInDuration));
            }
            else
            {
                //no SFX - just fade screen back in
                yield return StartCoroutine(FadePillOverlay(1f, 0f, fadeInDuration));
            }

            // 4. record choice - fires OnPillTaken → AudioManager crossfades music
            PillStateManager.Instance?.TakePill(choice);

            // 5. close overlay, resume ambient, disable prompt
            CloseOverlay();
        }

        private IEnumerator FadePillOverlay(float from, float to, float duration)
        {
            if (pillChoiceFade == null) yield break;

            pillChoiceFade.blocksRaycasts = (to >= 1f);

            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                pillChoiceFade.alpha = Mathf.Lerp(from, to, timer / duration);
                yield return null;
            }
            pillChoiceFade.alpha = to;
        }

        private void SetButtonsInteractable(bool state)
        {
            if (mirrorOverlayCanvas == null) return;
            foreach (var btn in mirrorOverlayCanvas.GetComponentsInChildren<Button>(true))
                btn.interactable = state;
        }

        // ─── Internal ─────────────────────────────────────────────────────────────

        private void ShowOverlay()
        {
            _isOverlayActive = true;

            //hide the interaction prompt immediately so it doesnt show during the sequence
            _interactionSystem?.SetInteractionEnabled(false);

            if (mirrorOverlayCanvas != null)
                mirrorOverlayCanvas.SetActive(true);

            //enforce scripted choices: hide the button the player isnt allowed to press
            //day 1 = forced take (conceal hidden), day 2 = forced refuse (take hidden)
            //days 3+ = free choice, both buttons visible
            if (PillStateManager.Instance != null && DayManager.Instance != null)
            {
                int today = DayManager.Instance.CurrentDay;
                if (PillStateManager.Instance.IsForcedChoice(today))
                {
                    PillChoice forced = PillStateManager.Instance.GetForcedChoice(today);
                    if (takePillButton   != null) takePillButton.gameObject.SetActive(forced == PillChoice.Taken);
                    if (concealPillButton != null) concealPillButton.gameObject.SetActive(forced == PillChoice.NotTaken);
                    Debug.Log($"[MIRROR] Day {today} forced choice: showing only {forced} button");
                }
                else
                {
                    //free choice day — make sure both buttons are visible
                    if (takePillButton   != null) takePillButton.gameObject.SetActive(true);
                    if (concealPillButton != null) concealPillButton.gameObject.SetActive(true);
                }
            }

            if (pillChoiceFade != null)
            {
                pillChoiceFade.alpha          = 0f;
                pillChoiceFade.blocksRaycasts = false;
            }

            if (PlayerController.Instance != null)
                PlayerController.Instance.LockMovement(true);

            //smooth bedroom ambient fade out, then mirror ambient fades in
            AudioManager.Instance?.FadeOutAndPauseAmbient();
            AudioManager.Instance?.FadeMirrorAmbientIn(mirrorAmbientFadeInDuration);

            Debug.Log("[MIRROR] Pill-choice overlay shown");
        }

        private void CloseOverlay()
        {
            if (!_isOverlayActive) return;

            _isOverlayActive = false;

            //disable interaction FIRST — before unlocking movement to avoid any physics re-trigger
            if (_interactionSystem != null)
                _interactionSystem.SetInteractionEnabled(false);
            else
                Debug.LogError("[MIRROR] _interactionSystem is NULL — prompt will not be hidden. Check MirrorInteract GO has InteractionSystem component.");

            if (mirrorOverlayCanvas != null)
                mirrorOverlayCanvas.SetActive(false);

            //restore both buttons for the next time the overlay opens (next day)
            if (takePillButton   != null) takePillButton.gameObject.SetActive(true);
            if (concealPillButton != null) concealPillButton.gameObject.SetActive(true);

            if (PlayerController.Instance != null)
                PlayerController.Instance.LockMovement(false);

            AudioManager.Instance?.StopMirrorAmbient();       //safety stop
            AudioManager.Instance?.ResumeAmbientWithFadeIn(); //bedroom fades back in

            Debug.Log("[MIRROR] Pill-choice overlay closed");
            DOLOSManager.Instance.TriggerAnnouncement();
        }

        // ─── Day Change ───────────────────────────────────────────────────────────

        private void OnDayPhaseChanged(DayPhase phase)
        {
            if (phase == DayPhase.Morning)
            {
                //new day — re-enable so prompt shows and E key works again
                _interactionSystem?.SetInteractionEnabled(true);
                Debug.Log("[MIRROR] New day — mirror interaction re-enabled");
            }
        }
    }
}
