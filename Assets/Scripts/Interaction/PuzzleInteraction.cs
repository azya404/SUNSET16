/*
puzzle interaction — press E on the console to open the puzzle overlay
mirrors TaskInteraction exactly but calls PuzzleManager instead of TaskManager
one-time only — disables itself after puzzle is completed
*/
using System.Collections;
using UnityEngine;
using SUNSET16.Core;

namespace SUNSET16.Interaction
{
    public class PuzzleInteraction : MonoBehaviour, IInteractable
    {
        [Header("Puzzle ID")]
        [SerializeField] private string puzzleId = "";

        [Header("Puzzle Overlay")]
        [Tooltip("Parent overlay panel GO. Activated on interact, deactivated on close.")]
        [SerializeField] private GameObject puzzleOverlayCanvas;

        [Header("Fade")]
        [SerializeField] private CanvasGroup fadePanel;
        [SerializeField] private float fadeDuration = 0.5f;

        [Header("Audio")]
        [SerializeField] private AudioSource roomAmbient;
        [SerializeField] private AudioSource puzzleThemeSource;
        [SerializeField] private AudioClip puzzleThemeClip;
        [SerializeField] private float ambientDuckedVolume = 0.05f;


        [Header("Settings")]
        [SerializeField] private string interactionPrompt = "Press E to interact";

        private bool _overlayActive = false;
        private float _originalAmbientVolume;

        public void Interact()
        {
            if (_overlayActive) return;
            StartCoroutine(OpenSequence());
        }

        public string GetInteractionPrompt() => interactionPrompt;

        public bool GetLocked()
        {
            return enabled == false;
        }

        private IEnumerator OpenSequence()
        {
            _overlayActive = true;
            if (PlayerController.Instance != null) PlayerController.Instance.LockMovement(true);

            // fade room ambient and ship theme to silence in parallel with screen fade to black
            _originalAmbientVolume = roomAmbient != null ? roomAmbient.volume : 1f;
            StartCoroutine(FadeAudio(roomAmbient, _originalAmbientVolume, 0f, fadeDuration));
            AudioManager.Instance?.FadeAndPauseHallwayAmbient(fadeDuration);
            yield return StartCoroutine(Fade(0f, 1f));

            // room ambient is now silent - pause it so position is preserved if needed
            if (roomAmbient != null && roomAmbient.isPlaying) roomAmbient.Pause();

            if (puzzleOverlayCanvas != null) puzzleOverlayCanvas.SetActive(true);

            if (puzzleThemeSource != null && puzzleThemeClip != null)
            {
                puzzleThemeSource.clip = puzzleThemeClip;
                puzzleThemeSource.volume = 0f;
                puzzleThemeSource.Play();
                StartCoroutine(FadeAudio(puzzleThemeSource, 0f, 1f, fadeDuration));
            }

            yield return StartCoroutine(Fade(1f, 0f));
        }

        public void CloseOverlay()
        {
            if (!_overlayActive) return;
            PuzzleManager.Instance.DonePuzzle();
            StartCoroutine(CloseSequence());
        }

        private IEnumerator CloseSequence()
        {
            StartCoroutine(FadeAudio(puzzleThemeSource, puzzleThemeSource != null ? puzzleThemeSource.volume : 1f, 0f, fadeDuration));
            yield return StartCoroutine(Fade(0f, 1f));

            if (puzzleOverlayCanvas != null) puzzleOverlayCanvas.SetActive(false);

            if (PuzzleManager.Instance != null)
                PuzzleManager.Instance.CompletePuzzle(puzzleId);
            else
                Debug.LogWarning("[PUZZLEINTERACTION] PuzzleManager not found");

            var sys = GetComponent<InteractionSystem>();
            if (sys != null) sys.SetInteractionEnabled(false);

            yield return StartCoroutine(Fade(1f, 0f));

            // unpause and fade in room ambient and ship theme, stop puzzle theme
            if (roomAmbient != null) roomAmbient.UnPause();
            StartCoroutine(FadeAudio(roomAmbient, 0f, _originalAmbientVolume, fadeDuration));
            AudioManager.Instance?.FadeInAndResumeHallwayAmbient(fadeDuration);
            if (puzzleThemeSource != null) puzzleThemeSource.Stop();

            if (PlayerController.Instance != null) PlayerController.Instance.LockMovement(false);
            _overlayActive = false;
        }

        private IEnumerator Fade(float from, float to)
        {
            if (fadePanel == null) yield break;
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

        private IEnumerator FadeAudio(AudioSource source, float from, float to, float duration)
        {
            if (source == null) yield break;
            float elapsed = 0f;
            source.volume = from;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            source.volume = to;
        }
    }
}
