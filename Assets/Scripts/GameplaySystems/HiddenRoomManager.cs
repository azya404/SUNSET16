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

            if (_doorStates[roomId] != DoorState.Locked)
            {
                Debug.LogWarning($"[HIDDENROOMMANAGER] Room '{roomId}' is already {_doorStates[roomId]}");
                return;
            }

            _doorStates[roomId] = DoorState.Discovered;
            OnRoomDiscovered?.Invoke(roomId);
            Debug.Log($"[HIDDENROOMMANAGER] Room '{roomId}' discovered!");
        }

        public void EnterRoom(string roomId)
        {
            if (!_doorStates.ContainsKey(roomId))
            {
                Debug.LogWarning($"[HIDDENROOMMANAGER] Unknown room ID: {roomId}");
                return;
            }

            if (_doorStates[roomId] != DoorState.Discovered)
            {
                Debug.LogWarning($"[HIDDENROOMMANAGER] Room '{roomId}' must be Discovered before entering (current: {_doorStates[roomId]})");
                return;
            }

            _doorStates[roomId] = DoorState.Entered;
            OnRoomEntered?.Invoke(roomId);
            Debug.Log($"[HIDDENROOMMANAGER] Room '{roomId}' entered!");
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