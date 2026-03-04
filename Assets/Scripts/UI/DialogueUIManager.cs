/*
albert's computer terminal dialogue system - handles all the branching conversation UI

the choices are INERT - they branch the dialogue tree for flavor/lore but dont touch
any game state, pill tracking, endings, or anything outside this script. purely cosmetic
branching. which sequence even shows up is determined by ComputerInteraction based on
day/pill state/task completion, not here

session rules:
- player can exit with Escape or the Close button at literally any point (unlike the
  task overlay which makes you wait). this was intentional
- exiting and re-entering resets to line 0 - sessions are cheap, no resume
- full scene unload clears the session from ComputerInteraction's side

while dialogue is open, movement is locked which also means InteractionSystem rejects
E presses, so world interactions are naturally blocked without any extra checks here
DOLOS checks IsDialogueActive before triggering so they cant overlap

typewriter effect runs per character with a configurable delay. clicking Continue
while its still typing skips to the end of the current line, second click advances

TODO: portrait animations - speakerPortrait slot is there but static right now
TODO: typewriter sound effects - the coroutine is the right place to add those
TODO: albert voice lines? would be cool, no timeline for this
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using SUNSET16.Core;

namespace SUNSET16.UI
{
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

        //DOLOSManager checks this before firing any announcement
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

            //player can bail out of albert at any time, no forced reading
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                HideDialogue();
            }
        }

        // ─── Public API ───────────────────────────────────────────────────────────

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

            //DOLOS cannot interrupt albert (but albert must not start during DOLOS — caller guards this)
            _lines     = sequence.lines;
            _lineIndex = 0;

            IsDialogueActive = true;
            dialoguePanel?.SetActive(true);

            if (PlayerController.Instance != null)
                PlayerController.Instance.LockMovement(true);

            Debug.Log($"[DIALOGUE] Starting sequence '{sequence.sequenceId}'");

            _playCoroutine = StartCoroutine(PlayFromCurrentLine());
        }

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

        public void OnAdvanceClicked()
        {
            if (!IsDialogueActive) return;

            if (_isTypewriting)
            {
                //typewriter still running - skip to end of this line immediately
                if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine);
                _isTypewriting = false;
                if (_lines != null && _lineIndex < _lines.Length)
                    dialogueBodyText.text = _lines[_lineIndex].text;

                //show whatever controls belong on this line (choices or continue button)
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

        public void OnChoiceSelected(int choiceIndex)
        {
            if (!IsDialogueActive || _lines == null) return;
            if (_lineIndex >= _lines.Length)         return;

            DialogueLine currentLine = _lines[_lineIndex];
            if (currentLine.choices == null || choiceIndex >= currentLine.choices.Length) return;

            DialogueChoice choice = currentLine.choices[choiceIndex];
            Debug.Log($"[DIALOGUE] Choice selected: '{choice.choiceText}' → line {choice.nextLineIndex}");

            HideAllChoiceButtons();

            //nextLineIndex < 0 is the convention for "this choice ends the conversation"
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

            if (speakerNameText != null)
                speakerNameText.text = line.speakerName ?? "";

            //only enable portrait if the line actually has one assigned
            if (speakerPortrait != null)
            {
                speakerPortrait.sprite  = line.portrait;
                speakerPortrait.enabled = line.portrait != null;
            }

            //hide controls while the typewriter is doing its thing
            if (advanceButton != null) advanceButton.SetActive(false);
            HideAllChoiceButtons();

            //run the typewriter, then wait for it to fully finish before showing controls
            _typewriterCoroutine = StartCoroutine(TypewriterEffect(line.text));
            yield return _typewriterCoroutine;
            _typewriterCoroutine = null;

            ShowControlsForCurrentLine();

            if (line.HasChoices)
            {
                //hand off control to OnChoiceSelected - coroutine stops here
                yield break;
            }

            if (line.autoAdvanceDelay > 0f)
            {
                //auto-advance after a delay - no player input needed
                yield return new WaitForSeconds(line.autoAdvanceDelay);
                _lineIndex++;
                if (_lineIndex < _lines.Length)
                    _playCoroutine = StartCoroutine(PlayFromCurrentLine());
                else
                    FinishDialogue();
            }
            else
            {
                //wait for player to click Continue - handled by OnAdvanceClicked
                _waitingForAdvance = true;
            }
        }

        private IEnumerator TypewriterEffect(string fullText)
        {
            _isTypewriting    = true;
            dialogueBodyText.text = "";

            //append one character at a time with a small delay between each
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
                //choices and continue are mutually exclusive - never show both
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

