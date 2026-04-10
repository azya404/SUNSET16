/*
task interaction — press E to open the task overlay in the boiler room

follows the same pattern as ComputerInteraction (BedroomScene):
  fade to black → overlay panel activates → fade in → player locked
  Dylan's task UI children live inside taskOverlayCanvas while the overlay is open
  Dylan (or the placeholder close button) calls CloseOverlay() when done
  fade to black → hide overlay → TaskManager.CompleteCurrentTask() → nextTaskObject activates → fade in → unlock

two task objects in BoilerRoomScene:
  Task1Object (active by default) 
    taskIndex = 1
    nextTaskObject → drag Task2Object GO here
  Task2Object (inactive by default)
    taskIndex = 2
    nextTaskObject → leave empty
    activated automatically when Task1Object's CloseOverlay() runs

HANDOFF (Dylan):
  1. add your task UI as child GameObjects of taskOverlayCanvas
  2. call CloseOverlay() on this component when the task is done
     (wire it to a close/complete button, or call it from your task logic)
  3. TaskManager.CompleteCurrentTask() is called for you inside CloseOverlay() — do not call it yourself
  4. Task 2 becomes available after Task 1 closes (Task2Object activates via nextTaskObject)
  5. completing Task 2 triggers DayManager.TaskCompleted() → Night phase begins (TaskManager handles this)

replaces the separate TaskWorldObject.cs from the Phase 8 plan doc —
this combines the world trigger + overlay lifecycle into one component,
exactly like ComputerInteraction does for the computer terminal
*/
using System.Collections;
using UnityEngine;
using SUNSET16.Core;

namespace SUNSET16.Interaction
{
    public class TaskInteraction : MonoBehaviour, IInteractable
    {
        [Header("Task Overlay")]
        [Tooltip("Parent overlay panel GO. Activated on interact, deactivated on close. Dylan's task UI lives here as children.")]
        [SerializeField] private GameObject taskOverlayCanvas;

        [Header("Fade")]
        [Tooltip("CanvasGroup on BoilerRoomScene's FadePanel — separate from BedroomScene's PillChoiceFade.")]
        [SerializeField] private CanvasGroup fadePanel;
        [SerializeField] private float fadeDuration = 0.5f;

        [Header("Task Sequence")]
        [Tooltip("Which task this represents (1 or 2). Informational — TaskManager tracks the actual index internally.")]
        [SerializeField] private int taskIndex = 1;
        [Tooltip("The next task's GO to enable after this task closes. Drag Task2Object here on Task1Object. Leave empty on Task2.")]
        [SerializeField] private GameObject nextTaskObject;

        [Header("Audio")]
        [Tooltip("The scene's room ambience AudioSource (e.g. BoilerRoomAmbience). Fades down on overlay open.")]
        [SerializeField] private AudioSource roomAmbient;
        [Tooltip("AudioSource for the puzzle theme. Created in scene (no clip set, not play on awake, Loop ON).")]
        [SerializeField] private AudioSource puzzleThemeSource;
        [SerializeField] private AudioClip puzzleThemeClip;
        [Tooltip("Volume to duck room ambient to during puzzle (0 = full silence, 0.05 = barely audible).")]
        [SerializeField] private float ambientDuckedVolume = 0.05f;

        [Header("Settings")]
        [SerializeField] private string interactionPrompt = "Press E to work";

        private bool _overlayActive = false;
        private float _originalAmbientVolume;

        // --- IInteractable -----------------------------------------------------------

        public void Interact()
        {
            if (_overlayActive)
            {
                Debug.LogWarning($"[TASK{taskIndex}] Overlay already open — ignoring duplicate interaction");
                return;
            }

            StartCoroutine(OpenSequence());
        }

        public string GetInteractionPrompt() => interactionPrompt;

        public bool GetLocked()
        {
            return enabled == false;
        }

        // --- Overlay open ------------------------------------------------------------

        private IEnumerator OpenSequence()
        {
            _overlayActive = true;
            if (PlayerController.Instance != null) PlayerController.Instance.LockMovement(true);

            // duck room ambient in parallel with screen fade to black
            _originalAmbientVolume = roomAmbient != null ? roomAmbient.volume : 1f;
            StartCoroutine(FadeAudio(roomAmbient, _originalAmbientVolume, ambientDuckedVolume, fadeDuration));
            yield return StartCoroutine(Fade(0f, 1f));             // fade to black

            if (taskOverlayCanvas != null) taskOverlayCanvas.SetActive(true);

            // fade in puzzle theme after overlay is visible
            if (puzzleThemeSource != null && puzzleThemeClip != null)
            {
                puzzleThemeSource.clip = puzzleThemeClip;
                puzzleThemeSource.volume = 0f;
                puzzleThemeSource.Play();
                StartCoroutine(FadeAudio(puzzleThemeSource, 0f, 1f, fadeDuration));
            }

            yield return StartCoroutine(Fade(1f, 0f));             // fade in — overlay revealed

            Debug.Log($"[TASK{taskIndex}] Overlay open — waiting for Dylan's task logic / close button");
        }

        // --- Overlay close -----------------------------------------------------------

        /// <summary>
        /// Call this from the overlay's Close/Complete button or from task-completion logic.
        /// Hides the overlay, marks the task complete in TaskManager, activates next task if any.
        /// </summary>
        public void CloseOverlay()
        {
            if (!_overlayActive)
            {
                Debug.LogWarning($"[TASK{taskIndex}] CloseOverlay called but no overlay is open");
                return;
            }
            StartCoroutine(CloseSequence());
        }

        private IEnumerator CloseSequence()
        {
            // fade out puzzle theme in parallel with screen fade to black
            StartCoroutine(FadeAudio(puzzleThemeSource, puzzleThemeSource != null ? puzzleThemeSource.volume : 1f, 0f, fadeDuration));
            yield return StartCoroutine(Fade(0f, 1f));             // fade to black

            if (taskOverlayCanvas != null) taskOverlayCanvas.SetActive(false);

            // tell TaskManager this task is done
            // Task 1 → TaskManager increments its internal index, does NOT call DayManager yet
            // Task 2 → TaskManager calls DayManager.TaskCompleted() → Night phase begins
            if (TaskManager.Instance != null)
                TaskManager.Instance.CompleteCurrentTask();
            else
                Debug.LogWarning($"[TASK{taskIndex}] TaskManager not found — CompleteCurrentTask skipped");

            // reveal the next task object (Task1 activates Task2)
            if (nextTaskObject != null)
                nextTaskObject.SetActive(true);

            // permanently disable self — player cannot re-interact after task completion
            var sys = GetComponent<InteractionSystem>();
            if (sys != null) sys.SetInteractionEnabled(false);

            yield return StartCoroutine(Fade(1f, 0f));             // fade in — back to game world

            // restore room ambient and stop puzzle theme after overlay is hidden
            StartCoroutine(FadeAudio(roomAmbient, ambientDuckedVolume, _originalAmbientVolume, fadeDuration));
            if (puzzleThemeSource != null) puzzleThemeSource.Stop();

            if (PlayerController.Instance != null) PlayerController.Instance.LockMovement(false);
            _overlayActive = false;

            Debug.Log($"[TASK{taskIndex}] Complete — overlay closed, next task {(nextTaskObject != null ? "activated" : "none")}");
        }

        // --- Fade helpers ------------------------------------------------------------

        private IEnumerator Fade(float from, float to)
        {
            if (fadePanel == null)
            {
                Debug.LogWarning($"[TASK{taskIndex}] FadePanel not assigned — skipping fade");
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