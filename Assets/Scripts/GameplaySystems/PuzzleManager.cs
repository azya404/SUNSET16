/*
manages the puzzles inside hidden rooms and their lore rewards
puzzles are optional content you get for exploring off-pill nights

when PuzzleManager hears OnRoomEntered from HiddenRoomManager it
looks up the PuzzleData ScriptableObject for that room, spawns the
puzzle prefab at the spawn point, and locks the player in place
the prefab has to implement IPuzzle or it gets destroyed immediately

when the puzzle is solved (CompletePuzzle called) the player gets
unlocked and if theres a lore reward (LoreEntryData) it gets added
to the unlocked lore collection

everything is data-driven - designers make PuzzleData assets in the
Inspector and the puzzle logic lives in the prefab, not here

completed puzzles and unlocked lore are tracked with HashSets
and saved/loaded by SaveManager as comma-separated strings

TODO: FindPuzzleForRoom uses .Contains which could match wrong rooms (room_1 matches room_10)
TODO: puzzle hints/clue system for the tablet
TODO: puzzle timer for speed-run mode
*/
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

        public int CompletePuzzleCount = 0;

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

            //listen for room entries so we know when to spawn puzzles
            if (HiddenRoomManager.Instance != null)
            {
                HiddenRoomManager.Instance.OnRoomEntered += OnRoomEntered;
            }

            SaveManager.Instance.OnSaveDeleted += OnSaveDeleted;
            IsInitialized = true;
            Debug.Log("[PUZZLEMANAGER] Initialized");
        }

        //HiddenRoomManager says a room was entered, check if theres a puzzle for it
        private void OnRoomEntered(string roomId)
        {
            PuzzleData puzzleData = FindPuzzleForRoom(roomId);
            if (puzzleData == null)
            {
                Debug.Log($"[PUZZLEMANAGER] No puzzle assigned to room '{roomId}'");
                return;
            }

            //dont respawn if already solved
            if (_completedPuzzles.Contains(puzzleData.puzzleId))
            {
                Debug.Log($"[PUZZLEMANAGER] Puzzle '{puzzleData.puzzleId}' already completed");
                return;
            }

            SpawnPuzzle(puzzleData);
        }
        public void DonePuzzle()
        {
            CompletePuzzleCount = CompletePuzzleCount + 1;
        }

        public int DonePuzzleCount()
        {
            return CompletePuzzleCount;
        }

        private void OnSaveDeleted()
        {
            _completedPuzzles.Clear();
            _unlockedLore.Clear();
            DestroyActivePuzzle();
            Debug.Log("[PUZZLEMANAGER] All puzzle/lore state reset (save deleted)");
        }

        //spawns the puzzle prefab and locks the player in place
        private void SpawnPuzzle(PuzzleData puzzleData)
        {
            DestroyActivePuzzle(); //clean up any leftover puzzle

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
                    //prefab exists but doesnt implement IPuzzle, thats a setup error
                    Debug.LogWarning("[PUZZLEMANAGER] Puzzle prefab does not implement IPuzzle interface");
                    Destroy(puzzleObj);
                }
            }
            else
            {
                Debug.Log($"[PUZZLEMANAGER] Puzzle '{puzzleData.puzzleId}' ready - no prefab assigned (tech demo)");
            }

            //freeze the player so they cant walk away from the puzzle
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

        //called when the puzzle is solved - marks it done, gives lore reward, unlocks player
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

            //check if this puzzle has a lore reward attached
            PuzzleData puzzleData = FindPuzzleById(puzzleId);
            if (puzzleData != null && puzzleData.loreReward != null)
            {
                UnlockLore(puzzleData.loreReward); //add it to the collection
            }

            DestroyActivePuzzle();

            //unfreeze the player
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.LockMovement(false);
                Debug.Log("[PUZZLEMANAGER] Player input unlocked (puzzle complete)");
            }

            OnPuzzleCompleted?.Invoke(puzzleId);
            Debug.Log($"[PUZZLEMANAGER] Puzzle '{puzzleId}' completed!");
        }

        //adds lore to the unlocked set if we havent already got it
        private void UnlockLore(LoreEntryData loreEntry)
        {
            if (_unlockedLore.Contains(loreEntry.loreId))
            {
                return; //already have this one
            }

            _unlockedLore.Add(loreEntry.loreId);
            OnLoreUnlocked?.Invoke(loreEntry); //TabletUIController will pick this up eventually
            Debug.Log($"[PUZZLEMANAGER] Lore unlocked: '{loreEntry.title}'");
        }

        public bool IsPuzzleCompleted(string puzzleId)
        {
            return _completedPuzzles != null && _completedPuzzles.Contains(puzzleId);
        }

        //returns true if a PuzzleData asset exists for the given day
        //DayManager uses this so the night->morning gate only blocks when a puzzle actually exists
        //TODO: once all puzzle assets are created this will always return true for off-pill days
        public bool HasPuzzleForDay(int day)
        {
            return FindPuzzleById($"puzzle_day_{day}") != null;
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

        //SaveManager calls these to restore state from save data
        public void SetCompletedPuzzles(HashSet<string> puzzleIds)
        {
            _completedPuzzles = new HashSet<string>(puzzleIds);
        }

        public void SetUnlockedLore(HashSet<string> loreIds)
        {
            _unlockedLore = new HashSet<string>(loreIds);
        }

        //uses .Contains which is a bit loose - "room_1" could match "room_10"
        //TODO: should probably be exact match or use a dictionary
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

        //cast to MonoBehaviour so we can Destroy the gameObject
        //IPuzzle is an interface so we cant destroy it directly
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

        //unsub from everything
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