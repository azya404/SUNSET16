/*
ship map - press M during normal gameplay to see where you've been

tracks three room states:
- normal rooms: always visible on the map, no discovery needed (hallways, main areas, etc)
- discovered hidden rooms: the player found the entrance off-pill, shown in yellow
- entered hidden rooms: been inside, shown in green

hidden rooms NEVER appear on the map until OnRoomDiscovered fires from HiddenRoomManager
they don't show up as "???" or "locked" or anything like that - they just don't exist
on the map until you find them, no spoilers

can't open during dialogue, while the task overlay is up, or during a DOLOS announcement
M also closes the map, Escape is handled upstream in PauseMenuController

the visual right now is just a text list in a VerticalLayoutGroup - placeholder until
proper ship map art lands on the assets branch
subscribes to HiddenRoomManager and RoomManager events to keep state in sync with saves

lives in CoreScene as a Singleton (DontDestroyOnLoad)

TODO: actual ship map visual with rooms at real positions
TODO: animated dots or icons to show room states instead of colour-coded text
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using SUNSET16.Core;

namespace SUNSET16.UI
{
    public class MapUIController : Singleton<MapUIController>
    {
        [Header("Map Panel")]
        [SerializeField] private GameObject mapPanel;

        [Header("Ship Layout Art (placeholder until art is delivered)")]
        [SerializeField] private Image      shipLayoutImage;    // Assign ship layout art when ready

        [Header("Room List (placeholder display)")]
        [SerializeField] private Transform  roomListContainer;  // VerticalLayoutGroup parent
        [SerializeField] private GameObject roomEntryPrefab;    // Prefab: Image dot + TMP_Text label

        [Header("Room State Colours")]
        [SerializeField] private Color colorDiscovered = new Color(0.9f, 0.85f, 0.3f);   // Yellow
        [SerializeField] private Color colorEntered    = new Color(0.3f, 0.85f, 0.4f);   // Green
        [SerializeField] private Color colorNormal     = new Color(0.7f, 0.7f, 0.7f);    // Grey (standard rooms)

        [Header("Close Prompt")]
        [SerializeField] private TMP_Text closePromptText;     // "M / ESC to close"

        // ─── Runtime State ────────────────────────────────────────────────────────

        public bool IsMapOpen { get; private set; }

        // tracks ALL rooms this session: hidden rooms only added once discovered
        private readonly Dictionary<string, DoorState> _roomStates = new Dictionary<string, DoorState>();

        // normal rooms are always visible, no discovery required
        private readonly HashSet<string> _knownNormalRooms = new HashSet<string>();

        // ─── Lifecycle ────────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            if (mapPanel != null) mapPanel.SetActive(false);

            if (closePromptText != null)
                closePromptText.text = "M / ESC to close";
        }

        private void Start()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsInitialized)
                Subscribe();
            else if (GameManager.Instance != null)
                GameManager.Instance.OnInitializationComplete += Subscribe;
        }

        private void Subscribe()
        {
            HiddenRoomManager.Instance.OnRoomDiscovered += HandleRoomDiscovered;
            HiddenRoomManager.Instance.OnRoomEntered    += HandleRoomEntered;
            RoomManager.Instance.OnRoomLoaded           += HandleRoomLoaded;
            SaveManager.Instance.OnSaveDeleted          += HandleSaveDeleted;
            SaveManager.Instance.OnGameLoaded           += HandleGameLoaded;

            Debug.Log("[MAP] Subscribed");
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnInitializationComplete -= Subscribe;

            if (HiddenRoomManager.Instance != null)
            {
                HiddenRoomManager.Instance.OnRoomDiscovered -= HandleRoomDiscovered;
                HiddenRoomManager.Instance.OnRoomEntered    -= HandleRoomEntered;
            }

            if (RoomManager.Instance != null)
                RoomManager.Instance.OnRoomLoaded -= HandleRoomLoaded;

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.OnSaveDeleted -= HandleSaveDeleted;
                SaveManager.Instance.OnGameLoaded  -= HandleGameLoaded;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.M))
                ToggleMap();
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        public void ToggleMap()
        {
            if (IsMapOpen) CloseMap();
            else           OpenMap();
        }

        public void OpenMap()
        {
            // guard: don't open during dialogue, task overlay, or DOLOS
            if (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueActive)
            {
                Debug.Log("[MAP] Cannot open — Albert dialogue is active");
                return;
            }

            if (TaskUIManager.Instance != null && TaskUIManager.Instance.IsOverlayActive)
            {
                Debug.Log("[MAP] Cannot open — task overlay is active");
                return;
            }

            if (DOLOSManager.Instance != null && DOLOSManager.Instance.IsAnnouncementActive)
            {
                Debug.Log("[MAP] Cannot open — DOLOS announcement is active");
                return;
            }

            IsMapOpen = true;
            RefreshMapDisplay();
            if (mapPanel != null) mapPanel.SetActive(true);
            Debug.Log("[MAP] Opened");
        }

        public void CloseMap()
        {
            IsMapOpen = false;
            if (mapPanel != null) mapPanel.SetActive(false);
            Debug.Log("[MAP] Closed");
        }

        // rebuilds the room list from scratch with current discovery state
        public void RefreshMapDisplay()
        {
            if (roomListContainer == null) return;

            foreach (Transform child in roomListContainer)
                Destroy(child.gameObject);

            // normal rooms always show up
            foreach (string roomName in _knownNormalRooms)
            {
                DoorState state = _roomStates.ContainsKey(roomName) ? _roomStates[roomName] : DoorState.Normal;
                CreateRoomEntry(FormatRoomName(roomName), colorNormal, state);
            }

            // hidden rooms only appear once the player has discovered them
            foreach (var pair in _roomStates)
            {
                if (_knownNormalRooms.Contains(pair.Key)) continue; // Already shown above

                Color    entryColor = pair.Value == DoorState.Entered ? colorEntered : colorDiscovered;
                DoorState state     = pair.Value;
                CreateRoomEntry(FormatRoomName(pair.Key), entryColor, state);
            }
        }

        // ─── Event Handlers ───────────────────────────────────────────────────────

        private void HandleRoomDiscovered(string roomId)
        {
            // first time this hidden room appears on the map
            if (!_roomStates.ContainsKey(roomId))
                _roomStates[roomId] = DoorState.Discovered;

            if (IsMapOpen) RefreshMapDisplay();
        }

        private void HandleRoomEntered(string roomId)
        {
            _roomStates[roomId] = DoorState.Entered;
            if (IsMapOpen) RefreshMapDisplay();
        }

        private void HandleRoomLoaded(string roomName)
        {
            // normal rooms get added to the known set as soon as you visit them
            if (!string.IsNullOrEmpty(roomName))
            {
                _knownNormalRooms.Add(roomName);
                if (!_roomStates.ContainsKey(roomName))
                    _roomStates[roomName] = DoorState.Normal;
            }
        }

        private void HandleSaveDeleted()
        {
            _roomStates.Clear();
            _knownNormalRooms.Clear();
            if (IsMapOpen) RefreshMapDisplay();
        }

        private void HandleGameLoaded()
        {
            // room states come back through HiddenRoomManager's save data
            // just refresh the display so the map matches the loaded save
            if (IsMapOpen) RefreshMapDisplay();
        }

        // ─── Internal Helpers ─────────────────────────────────────────────────────

        private void CreateRoomEntry(string label, Color dotColor, DoorState state)
        {
            if (roomEntryPrefab == null || roomListContainer == null) return;

            GameObject go   = Instantiate(roomEntryPrefab, roomListContainer);
            Image[]    imgs = go.GetComponentsInChildren<Image>();
            TMP_Text   txt  = go.GetComponentInChildren<TMP_Text>();

            // first Image component used as status dot
            if (imgs.Length > 0)
                imgs[0].color = dotColor;

            if (txt != null)
                txt.text = $"{label}  [{state}]";
        }

        private string FormatRoomName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return raw;
            return System.Globalization.CultureInfo.CurrentCulture
                         .TextInfo.ToTitleCase(raw.Replace("_", " "));
        }
    }
}
