using UnityEngine;
using System;
using System.Collections.Generic;

namespace SUNSET16.Core
{
    public class PuzzleManager : Singleton<PuzzleManager>
    {
        [Header("Puzzle Configuration")]
        [Tooltip("PuzzleData assets for each hidden room puzzle.")]
        [SerializeField] private PuzzleData[] _puzzleDataAssets;

        [Header("Puzzle Spawn Point")]
        [Tooltip("Transform where puzzle prefabs are instantiated in the hidden room.")]
        [SerializeField] private Transform _puzzleSpawnPoint;
        public bool IsInitialized { get; private set; }
        public IPuzzle ActivePuzzle { get; private set; }
        private HashSet<string> _completedPuzzles;
        private HashSet<string> _unlockedLore;
        public event Action<PuzzleData> OnPuzzleSpawned;
        public event Action<string> OnPuzzleCompleted;
        public event Action<LoreEntryData> OnLoreUnlocked;

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
            _completedPuzzles = new HashSet<string>();
            _unlockedLore = new HashSet<string>();

            if (HiddenRoomManager.Instance != null)
            {
                HiddenRoomManager.Instance.OnRoomEntered += OnRoomEntered;
            }

            SaveManager.Instance.OnSaveDeleted += OnSaveDeleted;
            IsInitialized = true;
            Debug.Log("[PUZZLEMANAGER] Initialized");
        }

        private void OnRoomEntered(string roomId)
        {
            PuzzleData puzzleData = FindPuzzleForRoom(roomId);
            if (puzzleData == null)
            {
                Debug.Log($"[PUZZLEMANAGER] No puzzle assigned to room '{roomId}'");
                return;
            }

            if (_completedPuzzles.Contains(puzzleData.puzzleId))
            {
                Debug.Log($"[PUZZLEMANAGER] Puzzle '{puzzleData.puzzleId}' already completed");
                return;
            }

            SpawnPuzzle(puzzleData);
        }

        private void OnSaveDeleted()
        {
            _completedPuzzles.Clear();
            _unlockedLore.Clear();
            DestroyActivePuzzle();
            Debug.Log("[PUZZLEMANAGER] All puzzle/lore state reset (save deleted)");
        }

        private void SpawnPuzzle(PuzzleData puzzleData)
        {
            DestroyActivePuzzle();

            if (puzzleData.puzzlePrefab != null && _puzzleSpawnPoint != null)
            {
                GameObject puzzleObj = Instantiate(puzzleData.puzzlePrefab, _puzzleSpawnPoint.position, Quaternion.identity);
                ActivePuzzle = puzzleObj.GetComponent<IPuzzle>();

                if (ActivePuzzle != null)
                {
                    ActivePuzzle.InitializePuzzle(puzzleData);
                    Debug.Log($"[PUZZLEMANAGER] Spawned puzzle '{puzzleData.puzzleId}'");
                }
                else
                {
                    Debug.LogWarning("[PUZZLEMANAGER] Puzzle prefab does not implement IPuzzle interface");
                    Destroy(puzzleObj);
                }
            }
            else
            {
                Debug.Log($"[PUZZLEMANAGER] Puzzle '{puzzleData.puzzleId}' ready - no prefab assigned (tech demo)");
            }

            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.LockMovement(true);
                Debug.Log("[PUZZLEMANAGER] Player input locked (puzzle active)");
            }
            else
            {
                Debug.LogWarning("[PUZZLEMANAGER] PlayerController not found - cannot lock input");
            }

            OnPuzzleSpawned?.Invoke(puzzleData);
        }

        public void CompletePuzzle(string puzzleId)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("[PUZZLEMANAGER] Cannot complete puzzle - not initialized");
                return;
            }

            if (_completedPuzzles.Contains(puzzleId))
            {
                Debug.LogWarning($"[PUZZLEMANAGER] Puzzle '{puzzleId}' already completed");
                return;
            }

            _completedPuzzles.Add(puzzleId);

            PuzzleData puzzleData = FindPuzzleById(puzzleId);
            if (puzzleData != null && puzzleData.loreReward != null)
            {
                UnlockLore(puzzleData.loreReward);
            }

            DestroyActivePuzzle();

            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.LockMovement(false);
                Debug.Log("[PUZZLEMANAGER] Player input unlocked (puzzle complete)");
            }

            OnPuzzleCompleted?.Invoke(puzzleId);
            Debug.Log($"[PUZZLEMANAGER] Puzzle '{puzzleId}' completed!");
        }

        private void UnlockLore(LoreEntryData loreEntry)
        {
            if (_unlockedLore.Contains(loreEntry.loreId))
            {
                return;
            }

            _unlockedLore.Add(loreEntry.loreId);
            OnLoreUnlocked?.Invoke(loreEntry);
            Debug.Log($"[PUZZLEMANAGER] Lore unlocked: '{loreEntry.title}'");
        }

        public bool IsPuzzleCompleted(string puzzleId)
        {
            return _completedPuzzles != null && _completedPuzzles.Contains(puzzleId);
        }

        public bool IsLoreUnlocked(string loreId)
        {
            return _unlockedLore != null && _unlockedLore.Contains(loreId);
        }

        public HashSet<string> GetCompletedPuzzles()
        {
            return new HashSet<string>(_completedPuzzles);
        }

        public HashSet<string> GetUnlockedLore()
        {
            return new HashSet<string>(_unlockedLore);
        }

        public void SetCompletedPuzzles(HashSet<string> puzzleIds)
        {
            _completedPuzzles = new HashSet<string>(puzzleIds);
        }

        public void SetUnlockedLore(HashSet<string> loreIds)
        {
            _unlockedLore = new HashSet<string>(loreIds);
        }

        private PuzzleData FindPuzzleForRoom(string roomId)
        {
            if (_puzzleDataAssets == null) return null;

            foreach (PuzzleData data in _puzzleDataAssets)
            {
                if (data != null && data.puzzleId.Contains(roomId))
                {
                    return data;
                }
            }
            return null;
        }

        private PuzzleData FindPuzzleById(string puzzleId)
        {
            if (_puzzleDataAssets == null) return null;

            foreach (PuzzleData data in _puzzleDataAssets)
            {
                if (data != null && data.puzzleId == puzzleId)
                {
                    return data;
                }
            }
            return null;
        }

        private void DestroyActivePuzzle()
        {
            if (ActivePuzzle != null)
            {
                MonoBehaviour puzzleMono = ActivePuzzle as MonoBehaviour;
                if (puzzleMono != null)
                {
                    Destroy(puzzleMono.gameObject);
                }
                ActivePuzzle = null;
            }
        }

        private void OnDestroy()
        {
            if (HiddenRoomManager.Instance != null)
            {
                HiddenRoomManager.Instance.OnRoomEntered -= OnRoomEntered;
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