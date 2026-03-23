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
using SUNSET16.Interaction;
using System.Collections.Generic;

namespace SUNSET16.UI
{
    public class DialogueUIManager : Singleton<DialogueUIManager>
    {
        [Header("Panel")]
        //[SerializeField] private GameObject  dialoguePanel;
        [SerializeField] private GameObject dialogueParent;
        [SerializeField] private Transform responseButtonContainer;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;
        [SerializeField] private GameObject AlbertDelay;
        [SerializeField] private Transform loreButtonContainer;
        [SerializeField] private Image loreImage;

        [Header("Prefabs")]
        [SerializeField] private GameObject  AlbertMessage;
        [SerializeField] private GameObject  PlayerMessage;

        [Header("Content")]
        [SerializeField] private TMP_Text speakerNameText;
        [SerializeField] private Image    speakerPortrait;
        [SerializeField] private TMP_Text dialogueBodyText;
        [SerializeField] private Sprite   messagebox;
        [SerializeField] private Sprite   playerMessagebox;

        [Header("Controls")]
        [SerializeField] private GameObject advanceButton;     // "▶ Continue" — visible after typewriter finishes
        [SerializeField] private GameObject closeButton;       // "Close" — always visible (player can exit)
        [SerializeField] private GameObject chatButton;
        [SerializeField] private GameObject loreButton;

        [Header("Choice Buttons (max 5)")]
        [SerializeField] private GameObject[] choiceButtonRoots = new GameObject[5];  // Parent GOs per choice
        [SerializeField] private TMP_Text[]   choiceButtonTexts = new TMP_Text[5];    // Labels per choice
        [SerializeField] private Image[]      choiceButtonImages = new Image[5];
        [SerializeField] private float        glitchInterval;
        [SerializeField] private float        dispProbability;
        [SerializeField] private float        dispIntensity;
        [SerializeField] private float        colorProbability;
        [SerializeField] private float        colorIntensity;

        [Header("Lore Buttons (max 5)")]
        [SerializeField] private GameObject[] loreButtonRoots = new GameObject[5];  // Parent GOs per choice
        [SerializeField] private TMP_Text[]   loreButtonTexts = new TMP_Text[5];    // Labels per choice
        [SerializeField] private Image[]      loreButtonImages = new Image[5];

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
        private bool            _started = false;
        private bool            _finished = false;
        private bool            _isResponding = false;
        private int             _messageNum = 0;
        private List<GameObject> _messages = new List<GameObject>();
        private DayPhase        _phase;
        private bool            _chatOpen = true;
        private List<LoreEntryData> _loreEntries;
        private int             _selectedEntry;
        private int             _entryPage = 1;
        private int             _buttonPage = 1;
        private Color           _baseColor = new Color(1f, 1f, 1f, 1f);
        private Color           _disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        private bool            _clickDisabled = false;

        //DOLOSManager checks this before firing any announcement
        public bool IsDialogueActive { get; private set; }

        // set to true when the morning computer session closes (either finished or player exited)
        // DoorController reads this for the bedroom exit gate
        public bool HasCompletedTodaySequence { get; private set; }
        // same for the night session
        public bool HasCompletedTodayNightSequence { get; private set; }

        // ─── Lifecycle ────────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            //dialoguePanel = GameObject.FindGameObjectWithTag("DialoguePanel");
            //if (dialoguePanel != null) dialoguePanel.SetActive(false);
            HideAllChoiceButtons();
        }

        private void Start()
        {
            if (DayManager.Instance != null)
                DayManager.Instance.OnPhaseChanged += OnPhaseChanged;
        }

        private void OnDestroy()
        {
            if (DayManager.Instance != null)
                DayManager.Instance.OnPhaseChanged -= OnPhaseChanged;
        }

        private void OnPhaseChanged(DayPhase phase)
        {
            // reset both flags at the start of each new phase so they reflect the current session
            HasCompletedTodaySequence      = false;
            HasCompletedTodayNightSequence = false;
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
            Debug.Log("dialogue started!");
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
                //dialoguePanel?.SetActive(true);

            if (_phase != DayManager.Instance.CurrentPhase)
            {
                _started = false;
                _messageNum = 0;
            }

            if (!_started)
            {
                _phase = DayManager.Instance.CurrentPhase;
                dialogueParent = GameObject.FindGameObjectWithTag("MessagingUI");
                responseButtonContainer = dialogueParent.transform.GetChild(1);
                for (int i = 0; i < 5; i++)
                {
                    int index = i;
                    Debug.Log("Index: " + i);
                    choiceButtonRoots[i] = responseButtonContainer.transform.GetChild(i).gameObject;
                    choiceButtonRoots[i].GetComponent<Button>().onClick.AddListener(() => OnChoiceSelected(index));
                    choiceButtonRoots[i].GetComponent<Button>().onClick.AddListener(MenuSound);
                    choiceButtonTexts[i] = choiceButtonRoots[i].GetComponentInChildren<TextMeshProUGUI>();
                }
                AlbertDelay = dialogueParent.transform.GetChild(2).gameObject;
                AlbertDelay.SetActive(false);
                closeButton = dialogueParent.transform.GetChild(3).gameObject;
                closeButton.GetComponent<Button>().onClick.AddListener(HideDialogue);
                closeButton.GetComponent<Button>().onClick.AddListener(MenuSound);
                advanceButton = dialogueParent.transform.GetChild(4).gameObject;
                advanceButton.GetComponent<Button>().onClick.AddListener(OnAdvanceClicked);

                choiceButtonImages[0] = responseButtonContainer.GetChild(0).GetComponent<Image>();
                Material globalMat = choiceButtonImages[0].material;
                globalMat.SetFloat("_GlitchInterval", glitchInterval);
                globalMat.SetFloat("_DispProbability", dispProbability);
                globalMat.SetFloat("_DispIntensity", dispIntensity);
                globalMat.SetFloat("_ColorProbability", colorProbability);
                globalMat.SetFloat("_ColorIntensity", colorIntensity);
                for (int i = 0; i < 5; i++)
                {
                    choiceButtonImages[i] = responseButtonContainer.GetChild(i).GetComponent<Image>();
                    choiceButtonImages[i].material = Instantiate(choiceButtonImages[i].material);
                    choiceButtonImages[i].material.SetFloat("_DispGlitchOn", 0f);
                    choiceButtonImages[i].material.SetFloat("_ColorGlitchOn", 0f);
                }

                chatButton = dialogueParent.transform.GetChild(5).gameObject;
                chatButton.GetComponent<Button>().onClick.AddListener(SwapToChat);
                chatButton.GetComponent<Button>().onClick.AddListener(MenuSound);
                loreButton = dialogueParent.transform.GetChild(6).gameObject;
                loreButton.GetComponent<Button>().onClick.AddListener(SwapToLore);
                loreButton.GetComponent<Button>().onClick.AddListener(MenuSound);
                loreButtonContainer = dialogueParent.transform.GetChild(7);
                loreButtonContainer.gameObject.SetActive(false);
                loreImage = dialogueParent.transform.GetChild(8).GetComponent<Image>();
                loreImage.gameObject.SetActive(false);
                for (int i = 0; i < 5; i++)
                {
                    int index = i;
                    Debug.Log("Index: " + i);
                    loreButtonRoots[i] = loreButtonContainer.transform.GetChild(i).gameObject;
                    loreButtonRoots[i].GetComponent<Button>().onClick.AddListener(() => OnEntrySelected(index));
                    loreButtonRoots[i].GetComponent<Button>().onClick.AddListener(MenuSound);
                    loreButtonTexts[i] = loreButtonRoots[i].GetComponentInChildren<TextMeshProUGUI>();
                }


                if (PlayerController.Instance != null)
                    PlayerController.Instance.LockMovement(true);

                Debug.Log($"[DIALOGUE] Starting sequence '{sequence.sequenceId}'");

                _lineIndex = 0;
                _playCoroutine = StartCoroutine(PlayFromCurrentLine());
                _started = true;
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

            if (_lines[_lineIndex].advanceToLine == -1 || _lines[_lineIndex].text == "")
            {
                FinishDialogue();
                return;
            }

            HideAllChoiceButtons();

            if (PlayerController.Instance != null)
                PlayerController.Instance.LockMovement(false);

            Debug.Log("[DIALOGUE] Closed by player");

            // mark this phase's session as done so DoorController bedroom gate passes
            if (DayManager.Instance != null)
            {
                if (DayManager.Instance.CurrentPhase == DayPhase.Morning)
                    HasCompletedTodaySequence = true;
                else if (DayManager.Instance.CurrentPhase == DayPhase.Night)
                    HasCompletedTodayNightSequence = true;
            }

            // Trigger the fade-out and canvas cleanup in ComputerInteraction
            ComputerInteraction computer = FindObjectOfType<ComputerInteraction>();
            if (computer != null)
                computer.CloseOverlay();
            else
                Debug.LogWarning("[DIALOGUE] ComputerInteraction not found — overlay may not close");
        }

        public void ResetDialogue()
        {
            IsDialogueActive   = false;
            _isTypewriting     = false;
            _waitingForAdvance = false;
            _isResponding      = false;
            _started           = false;
            _finished          = false;
            _clickDisabled     = false;
            _messageNum        = 0;
            _entryPage         = 1;
            _buttonPage        = 1;
            _chatOpen          = true;
            _lines.Clear();
            foreach (var msg in _messages) if (msg != null) Destroy(msg);
            _messages.Clear();
            Debug.Log("[DIALOGUE] Messages reset!");
        }

        public bool GetFinishedDialogue()
        {
            if (!RoomManager.Instance.GetCurrentRoomName().Contains("Bedroom"))
                return true;
            return _finished;
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
                else if (_lines[_lineIndex].advanceToLine != -1)
                    _lineIndex++;
                if (_lineIndex < _lines.Count)
                    _playCoroutine = StartCoroutine(PlayFromCurrentLine());
                else
                    FinishDialogue();
            }
        }

        public void OnChoiceSelected(int choiceIndex)
        {
            Debug.Log("Choice clicked! " + choiceIndex);
            if (!IsDialogueActive || _lines == null) return;
            if (_lineIndex >= _lines.Count)         return;
            Debug.Log("First checkpoint");
            _lines[_lineIndex].repeated = true;

            RuntimeLine currentLine = _lines[_lineIndex];
            if (currentLine.choices == null || choiceIndex >= currentLine.choices.Count) return;
            Debug.Log("Second checkpoint");

            RuntimeChoice choice = currentLine.choices[choiceIndex];
            Debug.Log($"[DIALOGUE] Choice selected: '{choice.choiceText}' → line {choice.nextLineIndex}");

            for (int i = 0; i < 5; i++)
            {
                choiceButtonImages[i].material.SetFloat("_DispGlitchOn", 0f);
                choiceButtonImages[i].material.SetFloat("_ColorGlitchOn", 0f);
            }
            _messageCoroutine = StartCoroutine(SendMessage(choiceIndex, currentLine, choice));

            HideAllChoiceButtons();
        }

        public void MenuSound()
        {
            if (!_clickDisabled)
                audioSource.PlayOneShot(menuClick);
        }

        public void SwapToChat()
        {
            // IF IN LORE ENTRIES:
            if (!_chatOpen)
            {
                // Deactivate entry buttons
                /*foreach (GameObject entry in loreButtonRoots)
                    entry.SetActive(false);*/
                loreButtonContainer.gameObject.SetActive(false);
                // Deactivate lore entry
                loreImage.gameObject.SetActive(false);
                // Activate appropriate response buttons
                ShowControlsForCurrentLine();
                // Activate all messages
                foreach (GameObject msg in _messages)
                    msg.SetActive(true);
            }
        }

        public void SwapToLore()
        {
            // IF IN CHAT:
            if (_chatOpen)
            {
                // Deactivate all response buttons
                /*foreach (GameObject choice in choiceButtonRoots)
                    choice.SetActive(false);*/
                responseButtonContainer.gameObject.SetActive(false);
                // Deactivate all messages
                foreach (GameObject msg in _messages)
                    msg.SetActive(false);
                // Activate appropriate entry buttons
                for (int i = 0; i < 4; i++)
                {
                    if (i < _loreEntries.Count)
                        break;
                    loreButtonRoots[i].SetActive(true);
                }
                // Display last opened lore entry (nothing if none have been selected)
                loreImage = _loreEntries[_selectedEntry].content[_entryPage];
            }
        }

        public void OnEntrySelected(int index)
        {
            int entryNum = index * _buttonPage;
            _entryPage = 1;
            loreImage = _loreEntries[entryNum].content[1];
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

                GameObject message;
                
                int messageY;
                if (_messageNum < 5)
                {
                    messageY = albertY - (_messageNum * offset);
                }
                else
                {
                    Debug.Log("Amount of objects in message (should be 5): " + _messages.Count);
                    messageY = albertY - (4 * offset);
                    Debug.Log("Shifting A");
                    foreach (GameObject mes in _messages)
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
                _messages.Add(message);
                message.transform.localPosition = messagePos;
                dialogueBodyText = message.GetComponentInChildren<TextMeshProUGUI>();
                audioSource.PlayOneShot(msgGet);

                _messageNum++;

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
            if (_messageNum < 5)
            {
                messageY = playerY - ((_messageNum-1) * offset);
            }
            else
            {
                messageY = playerY - (3 * offset);
                Debug.Log("Shifting P");
                foreach (GameObject mes in _messages)
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
            _messages.Add(message);
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

            _messageNum++;

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

            _messages.RemoveAll(item => item == null);
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
                //if (advanceButton != null) advanceButton.SetActive(true);
                line.repeated = true;
                _isResponding = false;
            }
        }

        private void ShowChoiceButtons(List<RuntimeChoice> choices)
        {
            closeButton.GetComponent<Image>().color = _baseColor;
            _clickDisabled = false;

            for (int i = 0; i < choiceButtonRoots.Length; i++)
            {
                bool show = i < choices.Count;

                if (show && i == 4)
                    Debug.Log("Show of repeat: " + choices[i].showOnRepeat + ", Repeated: " + _lines[_lineIndex].repeated);
                if ((choiceButtonRoots[i] != null) && show && choices[i].showOnRepeat && (!_lines[_lineIndex].repeated))
                    show = false;

                if (choiceButtonRoots[i] != null)
                    choiceButtonRoots[i].SetActive(show);

                if (show && choiceButtonTexts[i] != null)
                    choiceButtonTexts[i].text = choices[i].choiceText;

                if (show && choices[i].offPillChoice)
                {
                    choiceButtonImages[i].material.SetFloat("_DispGlitchOn", 1f);
                    choiceButtonImages[i].material.SetFloat("_ColorGlitchOn", 1f);
                }
            }
        }

        private void HideAllChoiceButtons()
        {
            closeButton.GetComponent<Image>().color = _disabledColor;
            _clickDisabled = true;

            foreach (var root in choiceButtonRoots)
                if (root != null) root.SetActive(false);
        }

        private void FinishDialogue()
        {
            _finished = true;
            //dialoguePanel?.SetActive(false);
            HideAllChoiceButtons();

            if (PlayerController.Instance != null)
                PlayerController.Instance.LockMovement(false);

            Debug.Log("[DIALOGUE] Sequence complete");

            // mark this phase's session as done so DoorController bedroom gate passes
            if (DayManager.Instance != null)
            {
                if (DayManager.Instance.CurrentPhase == DayPhase.Morning)
                    HasCompletedTodaySequence = true;
                else if (DayManager.Instance.CurrentPhase == DayPhase.Night)
                    HasCompletedTodayNightSequence = true;
            }

            // Trigger the fade-out and canvas cleanup in ComputerInteraction
            ComputerInteraction computer = FindObjectOfType<ComputerInteraction>();
            if (computer != null)
                computer.CloseOverlay();
            else
                Debug.LogWarning("[DIALOGUE] ComputerInteraction not found — overlay may not close");
        }
    }
}