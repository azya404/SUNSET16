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
using System.Collections.Generic;
using UnityEditor;

namespace SUNSET16.UI
{
    public class DialogueUIManager : Singleton<DialogueUIManager>
    {
        [Header("Panel")]
        [SerializeField] private GameObject  dialoguePanel;
        [SerializeField] private GameObject dialogueParent;
        [SerializeField] private Transform responseButtonContainer;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;
        [SerializeField] private GameObject AlbertDelay;

        [Header("Prefabs")]
        [SerializeField] private GameObject  AlbertMessage;
        [SerializeField] private GameObject  PlayerMessage;

        [Header("Content")]
        [SerializeField] private TMP_Text speakerNameText;
        [SerializeField] private Image    speakerPortrait;
        [SerializeField] private TMP_Text dialogueBodyText;
        [SerializeField] private Sprite    messagebox;
        [SerializeField] private Sprite    playerMessagebox;

        [Header("Controls")]
        [SerializeField] private GameObject advanceButton;     // "▶ Continue" — visible after typewriter finishes
        [SerializeField] private GameObject closeButton;       // "Close" — always visible (player can exit)

        [Header("Choice Buttons (max 4)")]
        [SerializeField] private GameObject[] choiceButtonRoots = new GameObject[4];  // Parent GOs per choice
        [SerializeField] private TMP_Text[]   choiceButtonTexts = new TMP_Text[4];    // Labels per choice

        [Header("Typewriter")]
        [SerializeField] private float typewriterCharDelay = 0.03f;
        [SerializeField] private float AlbertDelayAmt = 0.5f;
        [SerializeField] private float playerDelay = 0.5f;

        [Header("UI Coordinates")]
        [SerializeField] private int albertX;
        [SerializeField] private int albertY;
        [SerializeField] private int playerX;
        [SerializeField] private int playerY;
        [SerializeField] private int offset;

        [Header("Sound Effects")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip keyboard;
        [SerializeField] private AudioClip menuClick;
        [SerializeField] private AudioClip msgSend;
        [SerializeField] private AudioClip msgGet;

        // ─── Runtime State ────────────────────────────────────────────────────────

        private List<RuntimeLine> _lines;
        private int             _lineIndex;
        private bool            _isTypewriting;
        private bool            _waitingForAdvance;
        private Coroutine       _playCoroutine;
        private Coroutine       _messageCoroutine;
        private Coroutine       _typewriterCoroutine;
        private bool            started = false;
        private bool            _isResponding = false;

        private int             messageNum = 0;
        private List<GameObject> messages = new List<GameObject>();

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
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                HideDialogue();
            }
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        public void ShowDialogue(RuntimeSequence sequence)
        {
            if (sequence == null || sequence.lines == null || sequence.lines.Count == 0)
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

            IsDialogueActive = true;
            dialoguePanel?.SetActive(true);

            dialogueParent = GameObject.FindGameObjectWithTag("MessagingUI");
            responseButtonContainer = dialogueParent.transform.GetChild(1);
            AlbertDelay = dialogueParent.transform.GetChild(2).gameObject;
            AlbertDelay.SetActive(false);
            advanceButton = dialogueParent.transform.GetChild(4).gameObject;

            if (PlayerController.Instance != null)
                PlayerController.Instance.LockMovement(true);

            Debug.Log($"[DIALOGUE] Starting sequence '{sequence.sequenceId}'");

            if (!started)
            {
                _lineIndex = 0;
                _playCoroutine = StartCoroutine(PlayFromCurrentLine());
                started = true;
            }
            else
                ShowControlsForCurrentLine();
        }

        public void HideDialogue()
        {
            if (!IsDialogueActive || _isTypewriting || _isResponding) return;

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
                if (_lines != null && _lineIndex < _lines.Count)
                    dialogueBodyText.text = _lines[_lineIndex].text;

                //show whatever controls belong on this line (choices or continue button)
                ShowControlsForCurrentLine();
                return;
            }

            if (_waitingForAdvance)
            {
                _waitingForAdvance = false;
                if (_lines[_lineIndex].advanceToLine > 0)
                    _lineIndex = _lines[_lineIndex].advanceToLine;
                else
                    _lineIndex++;
                if (_lineIndex < _lines.Count)
                    _playCoroutine = StartCoroutine(PlayFromCurrentLine());
                else
                    FinishDialogue();
            }
        }

        public void OnChoiceSelected(int choiceIndex)
        {
            if (!IsDialogueActive || _lines == null) return;
            if (_lineIndex >= _lines.Count)         return;

            RuntimeLine currentLine = _lines[_lineIndex];
            if (currentLine.choices == null || choiceIndex >= currentLine.choices.Count) return;

            RuntimeChoice choice = currentLine.choices[choiceIndex];
            Debug.Log($"[DIALOGUE] Choice selected: '{choice.choiceText}' → line {choice.nextLineIndex}");

            audioSource.PlayOneShot(menuClick);
            _messageCoroutine = StartCoroutine(SendMessage(choiceIndex, currentLine, choice));
            HideAllChoiceButtons();
        }

        // ─── Internal Playback ────────────────────────────────────────────────────

        private IEnumerator PlayFromCurrentLine()
        {
            if (_lines == null || _lineIndex >= _lines.Count || _lineIndex == -1)
            {
                FinishDialogue();
                yield break;
            }

            RuntimeLine line = _lines[_lineIndex];

            if (!line.repeated && line.text != "")
            {
                if (line.sendDelay)
                {
                    AlbertDelay.SetActive(true);
                    dialogueBodyText = AlbertDelay.GetComponentInChildren<TextMeshProUGUI>();
                    typewriterCharDelay = AlbertDelayAmt;
                    //typewriterCharDelay = 1f;
                    for(int i = 0; i <= line.delayRepeats; i++)
                    {
                        _typewriterCoroutine = StartCoroutine(TypewriterEffect("..."));
                        yield return _typewriterCoroutine;
                        _typewriterCoroutine = null;
                    }

                    AlbertDelay.SetActive(false);
                    typewriterCharDelay = 0.03f;
                }

                line.repeated = true;

                GameObject message;
                
                int messageY;
                if (messageNum < 5)
                {
                    messageY = albertY - (messageNum * offset);
                }
                else
                {
                    messageY = albertY - (4 * offset);
                    Debug.Log("Shifting A");
                    foreach (GameObject mes in messages)
                    {
                        RectTransform rt = mes.GetComponent<RectTransform>();
                        rt.anchoredPosition += new Vector2(0, offset);
                        if (rt.anchoredPosition.y > albertY)
                        {
                            Destroy(mes.gameObject);
                        }
                    }
                    StartCoroutine(RemoveCells());
                }

                Vector3 messagePos = new Vector3(albertX, messageY, 0);

                message = Instantiate(AlbertMessage, dialogueParent.transform);
                messages.Add(message);
                message.transform.localPosition = messagePos;
                dialogueBodyText = message.GetComponentInChildren<TextMeshProUGUI>();
                audioSource.PlayOneShot(msgGet);

                messageNum++;

                if (speakerNameText != null)
                    speakerNameText.text = line.speakerName ?? "";

                //only enable portrait if the line actually has one assigned
                if (speakerPortrait != null)
                {
                    speakerPortrait.sprite  = line.portrait;
                    speakerPortrait.enabled = line.portrait != null;
                }

                if (line.advanceToLine == -1)
                    _isResponding = false;

                //hide controls while the typewriter is doing its thing
                if (advanceButton != null) advanceButton.SetActive(false);
                HideAllChoiceButtons();

                //run the typewriter, then wait for it to fully finish before showing controls
                /*_typewriterCoroutine = StartCoroutine(TypewriterEffect(line.text));
                yield return _typewriterCoroutine;
                _typewriterCoroutine = null;*/
                dialogueBodyText.text = line.text;
            }

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
                if (line.advanceToLine > 0)
                    _lineIndex = line.advanceToLine;
                else
                    _lineIndex++;
                if (_lineIndex < _lines.Count)
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

        private IEnumerator SendMessage(int choiceIndex, RuntimeLine currentLine, RuntimeChoice choice)
        {
            GameObject message;
            
            int messageY;
            if (messageNum < 5)
            {
                messageY = playerY - ((messageNum-1) * offset);
            }
            else
            {
                messageY = playerY - (3 * offset);
                Debug.Log("Shifting P");
                foreach (GameObject mes in messages)
                {
                    RectTransform rt = mes.GetComponent<RectTransform>();
                    rt.anchoredPosition += new Vector2(0, offset);
                    if (rt.anchoredPosition.y > albertY)
                    {
                        Destroy(mes.gameObject);
                    }
                }
                StartCoroutine(RemoveCells());
            }

            Vector3 messagePos = new Vector3(playerX, messageY, 0);

            message = Instantiate(PlayerMessage, dialogueParent.transform);
            messages.Add(message);
            dialogueBodyText = message.GetComponentInChildren<TextMeshProUGUI>();
            message.GetComponentInChildren<TextMeshProUGUI>().text = choice.choiceText;

            _typewriterCoroutine = StartCoroutine(TypewriterEffect(choice.choiceText));
            yield return _typewriterCoroutine;
            _typewriterCoroutine = null;

            audioSource.PlayOneShot(msgSend);
            RectTransform mrt = message.GetComponent<RectTransform>();
            _isResponding = true;
            while (mrt.anchoredPosition.y < messageY)
            {
                yield return new WaitForSeconds(0.01f);
                mrt.anchoredPosition += new Vector2(0, 10);
            }

            Transform box = message.transform.GetChild(1);
            Image img = box.GetComponent<Image>();
            img.sprite = playerMessagebox;
            Transform pfp = message.transform.GetChild(0);
            pfp.gameObject.SetActive(true);
            message.transform.localPosition = messagePos;

            messageNum++;

            if (currentLine.repeat)
                currentLine.choices.Remove(currentLine.choices[choiceIndex]);

            //yield return new WaitForSeconds(playerDelay);

            //nextLineIndex < 0 is the convention for "this choice ends the conversation"
            if (choice.nextLineIndex < 0)
            {
                FinishDialogue();
                yield break;
            }

            _lineIndex = choice.nextLineIndex;
            if (_lineIndex < _lines.Count)
                _playCoroutine = StartCoroutine(PlayFromCurrentLine());
            else
                FinishDialogue();
        }

        private IEnumerator TypewriterEffect(string fullText)
        {
            _isTypewriting    = true;
            dialogueBodyText.text = "";

            if (fullText != "...")
            {
                audioSource.clip = keyboard;
                audioSource.Play();
            }
            
            //append one character at a time with a small delay between each
            foreach (char c in fullText)
            {
                dialogueBodyText.text += c;
                yield return new WaitForSeconds(typewriterCharDelay);
            }

            audioSource.Stop();
            _isTypewriting = false;
        }

        public IEnumerator RemoveCells()
        {
            yield return 0;

            messages.RemoveAll(item => item == null);
        }

        private void ShowControlsForCurrentLine()
        {
            if (_lines == null || _lineIndex >= _lines.Count) return;

            RuntimeLine line = _lines[_lineIndex];

            if (line.HasChoices)
            {
                _isResponding = false;
                //choices and continue are mutually exclusive - never show both
                if (advanceButton != null) advanceButton.SetActive(false);
                ShowChoiceButtons(line.choices);
            }
            else if (line.autoAdvanceDelay <= 0)
            {
                if (advanceButton != null) advanceButton.SetActive(true);
            }
        }

        private void ShowChoiceButtons(List<RuntimeChoice> choices)
        {
            for (int i = 0; i < choiceButtonRoots.Length; i++)
            {
                bool show = i < choices.Count;
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
            _isResponding = false;

            dialoguePanel?.SetActive(false);
            HideAllChoiceButtons();

            if (PlayerController.Instance != null)
                PlayerController.Instance.LockMovement(false);

            Debug.Log("[DIALOGUE] Sequence complete");
        }

        public void menuSound()
        {
            audioSource.PlayOneShot(menuClick);
        }
    }
}

