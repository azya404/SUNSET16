using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using SUNSET16.Core;

namespace SUNSET16.UI
{
    /// <summary>
    /// Albert — the ship's AI computer terminal dialogue system.
    ///
    /// Albert conversations are branching and interactive. The player can select from
    /// up to 3 choices per line. Choices are SESSION-SCOPED AND INERT: they never
    /// affect game state, pill tracking, endings, or any system outside this dialogue.
    ///
    /// Session rules:
    ///   • Player can exit at any time with Escape or the Close button.
    ///   • Exiting and re-entering the same session resets to line 0.
    ///   • Leaving the bedroom scene fully erases the session (ComputerInteraction handles this).
    ///   • Which sequence is active is determined by ComputerInteraction based on
    ///     current day, pill state, and task completion.
    ///
    /// During dialogue:
    ///   • Player movement is locked.
    ///   • World interactions are blocked (movement is locked → InteractionSystem rejects E).
    ///   • DOLOS cannot trigger (DOLOSManager checks IsDialogueActive).
    ///
    /// Lives in CoreScene (DontDestroyOnLoad via Singleton).
    /// Called directly by: ComputerInteraction (ShowDialogue / HideDialogue).
    /// </summary>
    public class DialogueUIManager : Singleton<DialogueUIManager>
    {
        [Header("Panel")]
        [SerializeField] private GameObject  dialoguePanel;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Content")]
        [SerializeField] private TMP_Text speakerNameText;
        [SerializeField] private Image    speakerPortrait;
        [SerializeField] private TMP_Text dialogueBodyText;

        [Header("Controls")]
        [SerializeField] private GameObject advanceButton;     // "▶ Continue" — visible after typewriter finishes
        [SerializeField] private GameObject closeButton;       // "Close" — always visible (player can exit)

        [Header("Choice Buttons (max 3)")]
        [SerializeField] private GameObject[] choiceButtonRoots = new GameObject[3];  // Parent GOs per choice
        [SerializeField] private TMP_Text[]   choiceButtonTexts = new TMP_Text[3];    // Labels per choice

        [Header("Typewriter")]
        [SerializeField] private float typewriterCharDelay = 0.03f;

        // ─── Runtime State ────────────────────────────────────────────────────────

        private DialogueLine[] _lines;
        private int             _lineIndex;
        private bool            _isTypewriting;
        private bool            _waitingForAdvance;
        private Coroutine       _playCoroutine;
        private Coroutine       _typewriterCoroutine;

        /// <summary>True while a dialogue sequence is open. DOLOS checks this before triggering.</summary>
        public bool IsDialogueActive { get; private set; }

        // ─── Lifecycle ────────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            HideAllChoiceButtons();
        }

        private void Update()
        {
            if (!IsDialogueActive) return;

            // Player can exit Albert at any time via Escape
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                HideDialogue();
            }
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Begin a dialogue session from a ScriptableObject sequence.
        /// Silently ignored if dialogue is already active.
        /// </summary>
        public void ShowDialogue(DialogueSequence sequence)
        {
            if (sequence == null || sequence.lines == null || sequence.lines.Length == 0)
            {
                Debug.LogWarning("[DIALOGUE] Sequence is null or empty — nothing to show.");
                return;
            }

            if (IsDialogueActive)
            {
                Debug.LogWarning("[DIALOGUE] Already active — ignoring ShowDialogue call.");
                return;
            }

            // DOLOS cannot interrupt Albert (but Albert must not start during DOLOS — caller guards this)
            _lines     = sequence.lines;
            _lineIndex = 0;

            IsDialogueActive = true;
            dialoguePanel?.SetActive(true);

            if (PlayerController.Instance != null)
                PlayerController.Instance.LockMovement(true);

            Debug.Log($"[DIALOGUE] Starting sequence '{sequence.sequenceId}'");

            _playCoroutine = StartCoroutine(PlayFromCurrentLine());
        }

        /// <summary>
        /// Close the dialogue immediately (player-initiated via Escape or Close button).
        /// Resets line index — re-opening the same session will restart from line 0.
        /// </summary>
        public void HideDialogue()
        {
            if (!IsDialogueActive) return;

            StopAllCoroutines();
            _playCoroutine      = null;
            _typewriterCoroutine = null;

            IsDialogueActive    = false;
            _isTypewriting      = false;
            _waitingForAdvance  = false;

            dialoguePanel?.SetActive(false);
            HideAllChoiceButtons();

            if (PlayerController.Instance != null)
                PlayerController.Instance.LockMovement(false);

            Debug.Log("[DIALOGUE] Closed by player");
        }

        // ─── Button Callbacks (wired via Inspector OnClick) ───────────────────────

        /// <summary>
        /// Called by the "Continue" button.
        /// • If typewriter is still running: shows full text instantly.
        /// • If text is complete: advances to next line.
        /// </summary>
        public void OnAdvanceClicked()
        {
            if (!IsDialogueActive) return;

            if (_isTypewriting)
            {
                // Skip to end of current line
                if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine);
                _isTypewriting = false;
                if (_lines != null && _lineIndex < _lines.Length)
                    dialogueBodyText.text = _lines[_lineIndex].text;

                // Show appropriate controls for this line
                ShowControlsForCurrentLine();
                return;
            }

            if (_waitingForAdvance)
            {
                _waitingForAdvance = false;
                _lineIndex++;
                if (_lineIndex < _lines.Length)
                    _playCoroutine = StartCoroutine(PlayFromCurrentLine());
                else
                    FinishDialogue();
            }
        }

        /// <summary>
        /// Called by a choice button (index 0, 1, or 2).
        /// Branches to the target line defined in DialogueChoice.nextLineIndex.
        /// </summary>
        public void OnChoiceSelected(int choiceIndex)
        {
            if (!IsDialogueActive || _lines == null) return;
            if (_lineIndex >= _lines.Length)         return;

            DialogueLine currentLine = _lines[_lineIndex];
            if (currentLine.choices == null || choiceIndex >= currentLine.choices.Length) return;

            DialogueChoice choice = currentLine.choices[choiceIndex];
            Debug.Log($"[DIALOGUE] Choice selected: '{choice.choiceText}' → line {choice.nextLineIndex}");

            HideAllChoiceButtons();

            if (choice.nextLineIndex < 0)
            {
                FinishDialogue();
                return;
            }

            _lineIndex = choice.nextLineIndex;
            if (_lineIndex < _lines.Length)
                _playCoroutine = StartCoroutine(PlayFromCurrentLine());
            else
                FinishDialogue();
        }

        // ─── Internal Playback ────────────────────────────────────────────────────

        private IEnumerator PlayFromCurrentLine()
        {
            if (_lines == null || _lineIndex >= _lines.Length)
            {
                FinishDialogue();
                yield break;
            }

            DialogueLine line = _lines[_lineIndex];

            // Speaker
            if (speakerNameText != null)
                speakerNameText.text = line.speakerName ?? "";

            // Portrait
            if (speakerPortrait != null)
            {
                speakerPortrait.sprite  = line.portrait;
                speakerPortrait.enabled = line.portrait != null;
            }

            // Hide controls while typewriting
            if (advanceButton != null) advanceButton.SetActive(false);
            HideAllChoiceButtons();

            // Typewriter
            _typewriterCoroutine = StartCoroutine(TypewriterEffect(line.text));
            yield return _typewriterCoroutine;
            _typewriterCoroutine = null;

            // After typewriter finishes
            ShowControlsForCurrentLine();

            if (line.HasChoices)
            {
                // Wait for player to click a choice button — handled by OnChoiceSelected
                yield break;
            }

            if (line.autoAdvanceDelay > 0f)
            {
                yield return new WaitForSeconds(line.autoAdvanceDelay);
                _lineIndex++;
                if (_lineIndex < _lines.Length)
                    _playCoroutine = StartCoroutine(PlayFromCurrentLine());
                else
                    FinishDialogue();
            }
            else
            {
                // Wait for player to click Continue — handled by OnAdvanceClicked
                _waitingForAdvance = true;
            }
        }

        private IEnumerator TypewriterEffect(string fullText)
        {
            _isTypewriting    = true;
            dialogueBodyText.text = "";

            foreach (char c in fullText)
            {
                dialogueBodyText.text += c;
                yield return new WaitForSeconds(typewriterCharDelay);
            }

            _isTypewriting = false;
        }

        private void ShowControlsForCurrentLine()
        {
            if (_lines == null || _lineIndex >= _lines.Length) return;

            DialogueLine line = _lines[_lineIndex];

            if (line.HasChoices)
            {
                if (advanceButton != null) advanceButton.SetActive(false);
                ShowChoiceButtons(line.choices);
            }
            else
            {
                if (advanceButton != null) advanceButton.SetActive(true);
            }
        }

        private void ShowChoiceButtons(DialogueChoice[] choices)
        {
            for (int i = 0; i < choiceButtonRoots.Length; i++)
            {
                bool show = i < choices.Length;
                if (choiceButtonRoots[i] != null)
                    choiceButtonRoots[i].SetActive(show);

                if (show && choiceButtonTexts[i] != null)
                    choiceButtonTexts[i].text = choices[i].choiceText;
            }
        }

        private void HideAllChoiceButtons()
        {
            foreach (var root in choiceButtonRoots)
                if (root != null) root.SetActive(false);
        }

        private void FinishDialogue()
        {
            IsDialogueActive   = false;
            _isTypewriting     = false;
            _waitingForAdvance = false;

            dialoguePanel?.SetActive(false);
            HideAllChoiceButtons();

            if (PlayerController.Instance != null)
                PlayerController.Instance.LockMovement(false);

            Debug.Log("[DIALOGUE] Sequence complete");
        }
    }
}
