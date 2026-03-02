/*
manages the daily mandatory tasks the player does each morning
tasks are the "job" part of the loop - wake up, take pill, go do your task

pill choice directly maps to task difficulty which is the key mechanic:
took pill = Easy task (labeled, hints, fewer steps) cos the pill makes
you a compliant worker who follows instructions perfectly
refused pill = Hard task (unlabeled, no hints, more complex) cos your
mind is clearer but the "system" doesnt help you

SpawnTask looks up the right TaskData ScriptableObject based on
current day + difficulty, spawns the prefab at the spawn point,
and locks the player in place until they finish

IMPORTANT: CompleteCurrentTask also calls DayManager.TaskCompleted()
which triggers the Morning -> Night phase transition. completing
your task IS what advances the day

TODO: 
*/
using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine.UI;
using UnityEditor.VersionControl;

namespace SUNSET16.Core
{
    public class DialogueManager : Singleton<DialogueManager>
    {
        /*[Header("Task Spawn Point")]
        [Tooltip("Transform where task prefabs are instantiated in the task room.")]
        [SerializeField] private Transform _taskSpawnPoint;*/

        public bool IsInitialized { get; private set; }
        public bool IsTaskCompletedToday { get; private set; }
        public IInteractable ActiveDialogue { get; private set; }
        //private Dictionary<int, bool> _taskCompletionByDay;
        //public event Action<TaskData> OnTaskSpawned;
        //public event Action<int> OnTaskCompleted;

        // UI references
        public GameObject DialogueParent;
        //public TextMeshProUGUI Message;
        public GameObject AlbertMessage;
        public GameObject PlayerMessage;
        public GameObject responseButtonPrefab;
        public Transform responseButtonContainer;

        public int AlbertX = 140;
        public int AlbertY = 160;
        public int PlayerX = 70;
        public int PlayerY = 100;
        public int offset = 120;
        public int messageNum = 0;
        private List<GameObject> messages = new List<GameObject>();

        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsInitialized)
            {
                Initialize();
            }
            else if (GameManager.Instance != null)
            {
                GameManager.Instance.OnInitializationComplete += Initialize;
            }
        }

        [ContextMenu("Initialize")]
        private void Initialize()
        {
            //DayManager.Instance.OnDayChanged += OnDayChanged; //reset task state each new day
            //SaveManager.Instance.OnSaveDeleted += OnSaveDeleted;

            IsInitialized = true;
            Debug.Log("[DIALOGUEMANAGER] Initialized");
        }

        //new day started, check if this day already has a completed task (from save)
        private void OnDayChanged(int newDay)
        {
            //
            Debug.Log($"[DIALOGUEMANAGER] Day changed to {newDay}");
        }

        private void OnSaveDeleted()
        {
            //
            Debug.Log("[DIALOGUEMANAGER] All task state reset (save deleted)");
        }

        //unsub from everything
        private void OnDestroy()
        {
            if (DayManager.Instance != null)
            {
                DayManager.Instance.OnDayChanged -= OnDayChanged;
            }

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.OnSaveDeleted -= OnSaveDeleted;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnInitializationComplete -= Initialize;
            }
        }

        public void OnUIOpen()
        {
            DialogueParent = GameObject.FindGameObjectWithTag("MessagingUI");
            if (DialogueParent != null)
            {
                //Transform textTransform = DialogueParent.transform.Find("Dialoguebox/MessageText");
                //Message = textTransform.GetComponent<TextMeshProUGUI>();
                responseButtonContainer = DialogueParent.transform.GetChild(0);
            }
            else
            {
                Debug.Log("Dialogue Parent not found");
            }
        }

        public void StartDialogue(DialogueNode node)
        {
            // Set message text
            //Message.text = node.dialogueText;
            Debug.Log("Starting Dialogue");
            GameObject chatBubble;
            int bubbleY;
            if (messageNum < 3)
            {
                bubbleY = AlbertY - (messageNum * offset);
            }
            else
            {
                bubbleY = AlbertY - (2 * offset);
                foreach (GameObject mes in messages)
                {
                    RectTransform rt = mes.GetComponent<RectTransform>();
                    rt.anchoredPosition += new Vector2(0, 60);
                    if (rt.anchoredPosition.y > AlbertY)
                    {
                        Destroy (mes.gameObject);
                    }
                }
            }
            Vector3 bubblePos = new Vector3(AlbertX, bubbleY, 0);
            Quaternion rot = Quaternion.identity;

            if (node.dialogueText != "")
            {
                chatBubble = Instantiate(AlbertMessage, DialogueParent.transform);
                messages.Add(chatBubble);
                chatBubble.transform.localPosition = bubblePos;
                chatBubble.GetComponentInChildren<TextMeshProUGUI>().text = node.dialogueText;
            }
            
            messageNum++;

            //Remove any existing response buttons
            foreach (Transform child in responseButtonContainer)
            {
                Destroy(child.gameObject);
            }

            // Create and set up response buttons based on current dialogue node
            foreach (DialogueResponse response in node.responses)
            {
                GameObject buttonObj = Instantiate(responseButtonPrefab, responseButtonContainer);
                buttonObj.GetComponentInChildren<TextMeshProUGUI>().text = response.responseText;

                // Set up button to trigger SelectResponse when clicked
                buttonObj.GetComponent<Button>().onClick.AddListener(() => SelectResponse(response));
            }
        }

        // Handles response selection and triggers next dialogue node
        public void SelectResponse(DialogueResponse response)
        {
            GameObject responseBubble;
            int responseY;
            if (messageNum < 3)
            {
                responseY = PlayerY - ((messageNum - 1) * offset);
            }
            else
            {
                responseY = PlayerY - (2 * offset);
            }
            Vector3 bubblePos = new Vector3(PlayerX, responseY, 0);
            Quaternion rot = Quaternion.identity;

            responseBubble = Instantiate(PlayerMessage, DialogueParent.transform);
            messages.Add(responseBubble);
            responseBubble.transform.localPosition = bubblePos;
            responseBubble.GetComponentInChildren<TextMeshProUGUI>().text = response.responseText;

            // Check if there's a follow-up node
            if (response.nextNode != null)
            {
                StartDialogue(response.nextNode); // Start next dialogue
            }
        }

        [ContextMenu("Test")]
        public void TestFunction()
        {
            Debug.Log("It's here :)");
            if (DialogueParent != null)
            {
                Debug.Log("Parent is here");
            }
            /*if (Message != null)
            {
                Debug.Log("Message is here");
            }*/
            if (responseButtonContainer != null)
            {
                Debug.Log("Container is here");
            }
        }
    }
}