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
            _doorStates = new Dictionary<string, DoorState>();
            _roomTypes = new Dictionary<string, RoomType>();
            foreach (string roomId in _roomIds)
            {
                _doorStates[roomId] = DoorState.Locked;
                _roomTypes[roomId] = RoomType.Hidden;
            }

            DayManager.Instance.OnNightPhaseOffPill += OnNightPhaseOffPill;
            DayManager.Instance.OnNightPhaseOnPill += OnNightPhaseOnPill;
            SaveManager.Instance.OnSaveDeleted += OnSaveDeleted;

            IsInitialized = true;
            Debug.Log("[HIDDENROOMMANAGER] Initialized");
        }

        private void OnNightPhaseOffPill()
        {
            _roomsDiscoveredThisNight = 0;
            _roomsEnteredThisNight = 0;

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

        private void OnNightPhaseOnPill()
        {
            _roomsDiscoveredThisNight = 0;
            _roomsEnteredThisNight = 0;

            OnBedroomRestrictionActive?.Invoke();
            Debug.Log("[HIDDENROOMMANAGER] On-pill night: bedroom restriction active");
        }

        private void OnSaveDeleted()
        {
            foreach (string roomId in _roomIds)
            {
                _doorStates[roomId] = DoorState.Locked;
            }

            Debug.Log("[HIDDENROOMMANAGER] All door states reset (save deleted)");
        }

        public void DiscoverRoom(string roomId)
        {
            if (!_doorStates.ContainsKey(roomId))
            {
                Debug.LogWarning($"[HIDDENROOMMANAGER] Unknown room ID: {roomId}");
                return;
            }

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

            _doorStates[roomId] = DoorState.Discovered;
            _roomsDiscoveredThisNight++;
            OnRoomDiscovered?.Invoke(roomId);
            Debug.Log($"[HIDDENROOMMANAGER] Room '{roomId}' discovered! (Total this night: {_roomsDiscoveredThisNight})");
        }

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

            if (_doorStates[roomId] != DoorState.Discovered && _doorStates[roomId] != DoorState.Entered)
            {
                Debug.LogWarning($"[HIDDENROOMMANAGER] Room '{roomId}' must be Discovered before entering (current: {_doorStates[roomId]})");
                return;
            }

            _roomsEnteredThisNight++;

            if (_doorStates[roomId] == DoorState.Discovered)
            {
                _doorStates[roomId] = DoorState.Entered;
                OnRoomEntered?.Invoke(roomId);
                Debug.Log($"[HIDDENROOMMANAGER] Room '{roomId}' entered for the first time!");
            }
            else
            {
                Debug.Log($"[HIDDENROOMMANAGER] Re-entering room '{roomId}' (already completed)");
            }
        }

        public DoorState GetDoorState(string roomId)
        {
            if (_doorStates != null && _doorStates.ContainsKey(roomId))
            {
                return _doorStates[roomId];
            }
            return DoorState.Locked;
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

        public Dictionary<string, DoorState> GetAllDoorStates()
        {
            return new Dictionary<string, DoorState>(_doorStates);
        }

        public bool HasEnteredRoomThisNight()
        {
            return _roomsEnteredThisNight > 0;
        }

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
            return null;
        }

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
                return false;
            }

            PillChoice todaysPillChoice = PillStateManager.Instance.GetPillChoice(DayManager.Instance.CurrentDay);
            if (todaysPillChoice != PillChoice.NotTaken)
            {
                Debug.Log("[HIDDENROOMMANAGER] Hidden rooms only accessible when OFF pill");
                return false;
            }

            if (!TaskManager.Instance.IsTaskCompleted(DayManager.Instance.CurrentDay))
            {
                Debug.Log("[HIDDENROOMMANAGER] Hidden rooms only accessible after completing daily task");
                return false;
            }

            return true;
        }

        public RoomType GetRoomType(string roomId)
        {
            return _roomTypes.TryGetValue(roomId, out RoomType type) ? type : RoomType.Task;
        }

        public void SetRoomType(string roomId, RoomType type)
        {
            _roomTypes[roomId] = type;
        }

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