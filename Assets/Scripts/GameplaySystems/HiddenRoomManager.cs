/*
manages hidden room discovery and access
hidden rooms are the reward for refusing the pill - they have puzzles
and USB drive lore that reveals whats really going on

station has 3 hidden rooms, all start locked. when you refuse the pill
and night comes, ONE room gets auto-discovered (in order: room_1, room_2, room_3)
so refusing on 3 different nights = all 3 rooms found

theres a 1-per-night limit on discovery to pace the content out
and you can only actually enter if its night + off-pill + daily task done
that triple check prevents any shortcuts

on-pill nights fire OnBedroomRestrictionActive instead which tells
DoorController to lock everything except the bedroom door

TODO: visual effects when a room is discovered (camera pan, door glow)
TODO: room IDs are hardcoded - should match scene naming convention
TODO: re-entering a completed room should skip the puzzle
*/
using UnityEngine;
using System;
using System.Collections.Generic;

namespace SUNSET16.Core
{
    public class HiddenRoomManager : Singleton<HiddenRoomManager>
    {
        [Header("Hidden Room IDs")]
        [Tooltip("List of room identifiers in discovery order.")]
        [SerializeField] private string[] _roomIds = { "room_1", "room_2", "room_3" };

        public bool IsInitialized { get; private set; }
        private Dictionary<string, DoorState> _doorStates;
        private Dictionary<string, RoomType> _roomTypes;
        private int _roomsDiscoveredThisNight = 0;
        private int _roomsEnteredThisNight = 0;
        public event Action<string> OnRoomDiscovered;
        public event Action<string> OnRoomEntered;
        public event Action OnBedroomRestrictionActive;

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

        private void Initialize()
        {
            //set up dictionaries - all rooms start locked
            _doorStates = new Dictionary<string, DoorState>();
            _roomTypes = new Dictionary<string, RoomType>();
            foreach (string roomId in _roomIds)
            {
                _doorStates[roomId] = DoorState.Locked;
                _roomTypes[roomId] = RoomType.Hidden;
            }

            //listen for night phase events to know when to discover rooms
            DayManager.Instance.OnNightPhaseOffPill += OnNightPhaseOffPill;
            DayManager.Instance.OnNightPhaseOnPill += OnNightPhaseOnPill;
            SaveManager.Instance.OnSaveDeleted += OnSaveDeleted;

            IsInitialized = true;
            Debug.Log("[HIDDENROOMMANAGER] Initialized");
        }

        //off-pill night = time to discover the next room
        private void OnNightPhaseOffPill()
        {
            _roomsDiscoveredThisNight = 0; //reset per-night counters
            _roomsEnteredThisNight = 0;

            //find the next room thats still locked and discover it
            string nextRoom = GetNextLockedRoom();
            if (nextRoom != null)
            {
                DiscoverRoom(nextRoom);
            }
            else
            {
                Debug.Log("[HIDDENROOMMANAGER] All rooms already discovered or entered");
            }
        }

        //on-pill night = no exploring, bedroom only
        private void OnNightPhaseOnPill()
        {
            _roomsDiscoveredThisNight = 0;
            _roomsEnteredThisNight = 0;

            OnBedroomRestrictionActive?.Invoke(); //tells DoorController to lock non-bedroom doors
            Debug.Log("[HIDDENROOMMANAGER] On-pill night: bedroom restriction active");
        }

        //save got wiped, lock everything back up
        private void OnSaveDeleted()
        {
            foreach (string roomId in _roomIds)
            {
                _doorStates[roomId] = DoorState.Locked;
            }

            Debug.Log("[HIDDENROOMMANAGER] All door states reset (save deleted)");
        }

        //marks a room as discovered (Locked -> Discovered)
        //has a bunch of validation - must be night, off-pill, task done, max 1 per night
        public void DiscoverRoom(string roomId)
        {
            if (!_doorStates.ContainsKey(roomId))
            {
                Debug.LogWarning($"[HIDDENROOMMANAGER] Unknown room ID: {roomId}");
                return;
            }

            //make sure its actually a hidden room and not a normal one
            if (_roomTypes.TryGetValue(roomId, out RoomType type) && type != RoomType.Hidden)
            {
                Debug.LogWarning($"[HIDDENROOMMANAGER] Room '{roomId}' is not a hidden room (type: {type})");
                return;
            }

            if (!CanAccessHiddenRooms())
            {
                Debug.LogWarning($"[HIDDENROOMMANAGER] Cannot discover room '{roomId}': hidden room access not allowed (must be night phase, off pill, and task completed)");
                return;
            }

            //only 1 discovery per night to pace the content
            if (_roomsDiscoveredThisNight >= 1)
            {
                Debug.LogWarning($"[HIDDENROOMMANAGER] Cannot discover room '{roomId}': already discovered 1 room this night (limit: 1 per night)");
                return;
            }

            if (_doorStates[roomId] != DoorState.Locked)
            {
                Debug.LogWarning($"[HIDDENROOMMANAGER] Room '{roomId}' is already {_doorStates[roomId]}");
                return;
            }

            _doorStates[roomId] = DoorState.Discovered; //door light turns on
            _roomsDiscoveredThisNight++;
            OnRoomDiscovered?.Invoke(roomId);
            Debug.Log($"[HIDDENROOMMANAGER] Room '{roomId}' discovered! (Total this night: {_roomsDiscoveredThisNight})");
        }

        //player walks into a discovered room (Discovered -> Entered)
        public void EnterRoom(string roomId)
        {
            if (!_doorStates.ContainsKey(roomId))
            {
                Debug.LogWarning($"[HIDDENROOMMANAGER] Unknown room ID: {roomId}");
                return;
            }

            if (!CanAccessHiddenRooms())
            {
                Debug.LogWarning($"[HIDDENROOMMANAGER] Cannot enter room '{roomId}': hidden room access not allowed (must be night phase, off pill, and task completed)");
                return;
            }

            //can only enter if its been discovered first
            if (_doorStates[roomId] != DoorState.Discovered && _doorStates[roomId] != DoorState.Entered)
            {
                Debug.LogWarning($"[HIDDENROOMMANAGER] Room '{roomId}' must be Discovered before entering (current: {_doorStates[roomId]})");
                return;
            }

            _roomsEnteredThisNight++;

            //only fire the event on first entry, not re-entries
            if (_doorStates[roomId] == DoorState.Discovered)
            {
                _doorStates[roomId] = DoorState.Entered;
                OnRoomEntered?.Invoke(roomId); //PuzzleManager listens to this
                Debug.Log($"[HIDDENROOMMANAGER] Room '{roomId}' entered for the first time!");
            }
            else
            {
                Debug.Log($"[HIDDENROOMMANAGER] Re-entering room '{roomId}' (already completed)");
            }
        }

        //returns the door state, defaults to Locked if not found
        public DoorState GetDoorState(string roomId)
        {
            if (_doorStates != null && _doorStates.ContainsKey(roomId))
            {
                return _doorStates[roomId];
            }
            return DoorState.Locked; //safe default
        }

        public void SetDoorState(string roomId, DoorState state)
        {
            if (_doorStates == null) return;

            if (!_doorStates.ContainsKey(roomId))
            {
                Debug.LogWarning($"[HIDDENROOMMANAGER] Unknown room ID: {roomId}");
                return;
            }

            _doorStates[roomId] = state;
        }

        public string[] GetAllRoomIds()
        {
            return _roomIds;
        }

        //returns a COPY so the caller cant mess with our internal state
        public Dictionary<string, DoorState> GetAllDoorStates()
        {
            return new Dictionary<string, DoorState>(_doorStates);
        }

        public bool HasEnteredRoomThisNight()
        {
            return _roomsEnteredThisNight > 0;
        }

        //DoorController calls this to check if a specific room can be entered rn
        public bool CanAccessRoom(string roomId)
        {
            if (!CanAccessHiddenRooms())
            {
                return false; //fails the global check
            }

            //room needs to be at least Discovered to enter
            DoorState state = GetDoorState(roomId);
            return state == DoorState.Discovered || state == DoorState.Entered;
        }

        public bool IsRoomDiscovered(string roomId)
        {
            DoorState state = GetDoorState(roomId);
            return state == DoorState.Discovered || state == DoorState.Entered;
        }

        //walks the room list in order and returns the first one thats still locked
        //this is how we ensure rooms are discovered in sequence
        private string GetNextLockedRoom()
        {
            foreach (string roomId in _roomIds)
            {
                if (_doorStates.ContainsKey(roomId) && _doorStates[roomId] == DoorState.Locked &&
                    _roomTypes.TryGetValue(roomId, out RoomType type) && type == RoomType.Hidden)
                {
                    return roomId;
                }
            }
            return null; //all rooms already discovered
        }

        //the triple check: night + off-pill + task done
        //all three must pass or you cant access hidden rooms
        private bool CanAccessHiddenRooms()
        {
            if (DayManager.Instance == null || PillStateManager.Instance == null || TaskManager.Instance == null)
            {
                Debug.LogWarning("[HIDDENROOMMANAGER] Cannot validate access: required managers not initialized");
                return false;
            }

            if (DayManager.Instance.CurrentPhase != DayPhase.Night)
            {
                Debug.Log("[HIDDENROOMMANAGER] Hidden rooms only accessible during Night phase");
                return false; //daytime = no exploring
            }

            PillChoice todaysPillChoice = PillStateManager.Instance.GetPillChoice(DayManager.Instance.CurrentDay);
            if (todaysPillChoice != PillChoice.NotTaken)
            {
                Debug.Log("[HIDDENROOMMANAGER] Hidden rooms only accessible when OFF pill");
                return false; //took the pill = no access
            }

            if (!TaskManager.Instance.IsTaskCompleted(DayManager.Instance.CurrentDay))
            {
                Debug.Log("[HIDDENROOMMANAGER] Hidden rooms only accessible after completing daily task");
                return false; //do your chores first
            }

            return true; //all good
        }

        public RoomType GetRoomType(string roomId)
        {
            return _roomTypes.TryGetValue(roomId, out RoomType type) ? type : RoomType.Task;
        }

        public void SetRoomType(string roomId, RoomType type)
        {
            _roomTypes[roomId] = type;
        }

        //unsub from everything
        private void OnDestroy()
        {
            if (DayManager.Instance != null)
            {
                DayManager.Instance.OnNightPhaseOffPill -= OnNightPhaseOffPill;
                DayManager.Instance.OnNightPhaseOnPill -= OnNightPhaseOnPill;
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
    }
}