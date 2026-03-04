/*
bathroom mirror interaction — player presses E to decide pill choice
shows animated overlay, player clicks Take Pill or Hide Pill

button click sequence:
  1. disable buttons immediately (prevent double-click)
  2. fade screen to black (PillChoiceFade CanvasGroup alpha 0 → 1)
  3. play pill-specific SFX and wait for clip to finish
  4. record the pill choice (fires OnPillTaken → AudioManager crossfades music)
  5. fade screen back in (alpha 1 → 0)
  6. unfreeze player, close overlay

ambient pause/resume stubbed out as TODOs — wired in 4f when AudioManager gets
ambient support

DOLOS and movement-lock blocking handled upstream by InteractionSystem

replaces TechDemo/MirrorInteraction.cs
*/
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SUNSET16.Core;

namespace SUNSET16.Interaction
{
    public class MirrorInteraction : MonoBehaviour, IInteractable
    {
        [Header("UI References")]
        [Tooltip("Canvas or panel containing the Take Pill / Hide Pill buttons.")]
        [SerializeField] private GameObject mirrorOverlayCanvas;
        [Tooltip("CanvasGroup on the PillChoiceFade child GO — fades to black on button click.")]
        [SerializeField] private CanvasGroup pillChoiceFade;

        [Header("SFX")]
        [Tooltip("Sound played when the player takes the pill.")]
        [SerializeField] private AudioClip pillTakeSFX;
        [Tooltip("Sound played when the player hides the pill.")]
        [SerializeField] private AudioClip pillHideSFX;

        [Header("Fade Settings")]
        [SerializeField] private float fadeOutDuration = 0.8f;
        [SerializeField] private float fadeInDuration  = 1.2f;

        [Header("Settings")]
        [SerializeField] private string interactionPrompt = "Press E to look in mirror";

        private bool _isOverlayActive = false;

        // ─── Lifecycle ────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (mirrorOverlayCanvas != null)
                mirrorOverlayCanvas.SetActive(false);
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
            // prevent double-click while sequence is running
            SetButtonsInteractable(false);

            // 1. fade screen to black AND fade mirror audio out simultaneously
            AudioManager.Instance?.FadeMirrorAmbientOut(fadeOutDuration); //fire and forget
            yield return StartCoroutine(FadePillOverlay(0f, 1f, fadeOutDuration)); //wait for screen

            // 2. play pill-specific SFX and wait for it to finish
            AudioClip clip = (choice == PillChoice.Taken) ? pillTakeSFX : pillHideSFX;
            if (clip != null)
            {
                AudioManager.Instance?.PlaySFX(clip);
                yield return new WaitForSeconds(clip.length);
            }

            // 3. record the choice — fires OnPillTaken → AudioManager crossfades music
            PillStateManager.Instance?.TakePill(choice);

            // 4. fade back in
            yield return StartCoroutine(FadePillOverlay(1f, 0f, fadeInDuration));

            // 5. close overlay and unfreeze player
            CloseOverlay();
        }

        private IEnumerator FadePillOverlay(float from, float to, float duration)
        {
            if (pillChoiceFade == null) yield break;

            // block input while fading to black, unblock while fading back in
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

            if (mirrorOverlayCanvas != null)
                mirrorOverlayCanvas.SetActive(true);

            // ensure fade layer starts transparent and non-blocking
            if (pillChoiceFade != null)
            {
                pillChoiceFade.alpha          = 0f;
                pillChoiceFade.blocksRaycasts = false;
            }

            if (PlayerController.Instance != null)
                PlayerController.Instance.LockMovement(true);

            AudioManager.Instance?.PauseAmbient();      //pause Albert's theme (preserves position)
            AudioManager.Instance?.PlayMirrorAmbient(); //start mirror scene audio
            Debug.Log("[MIRROR] Pill-choice overlay shown");
        }

        private void CloseOverlay()
        {
            if (!_isOverlayActive) return;

            _isOverlayActive = false;

            if (mirrorOverlayCanvas != null)
                mirrorOverlayCanvas.SetActive(false);

            if (PlayerController.Instance != null)
                PlayerController.Instance.LockMovement(false);

            AudioManager.Instance?.StopMirrorAmbient(); //safety stop in case fade didn't finish
            AudioManager.Instance?.ResumeAmbient();     //resume Albert's theme from where it paused
            Debug.Log("[MIRROR] Pill-choice overlay closed");
        }
    }
}
