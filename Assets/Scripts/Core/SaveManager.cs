using UnityEngine;
using System;
using System.Collections.Generic;

namespace SUNSET16.Core
{
    public class SaveManager : Singleton<SaveManager>
    {
        public bool SaveExists { get; private set; }
        public bool IsInitialized { get; private set; }
        public event Action OnGameSaved;
        public event Action OnGameLoaded;
        public event Action OnSaveDeleted;

        public void Initialize()
        {
            SaveExists = HasSaveData();
            IsInitialized = true;
            if (SaveExists)
            {
                Debug.Log("[SAVEMANAGER] Save data found - ready to load");
            }
            else
            {
                Debug.Log("[SAVEMANAGER] No save data found - new game");
            }
        }

        public bool HasSaveData()
        {
            return PlayerPrefs.GetInt("SUNSET16_SaveExists", 0) == 1;
        }

        public void SaveGame()
        {
            if (!GameManager.Instance.IsInitialized)
            {
                Debug.LogError("[SAVEMANAGER] Cannot save - managers not initialized");
                return;
            }

            try
            {
                int currentDay = DayManager.Instance.CurrentDay;
                DayPhase currentPhase = DayManager.Instance.CurrentPhase;
                PlayerPrefs.SetInt("SUNSET16_CurrentDay", currentDay);
                PlayerPrefs.SetInt("SUNSET16_CurrentPhase", (int)currentPhase);
                PlayerPrefs.SetInt("SUNSET16_IsGameOver", DayManager.Instance.IsGameOver ? 1 : 0);

                for (int day = 1; day <= 5; day++)
                {
                    PillChoice choice = PillStateManager.Instance.GetPillChoice(day);
                    PlayerPrefs.SetInt($"SUNSET16_PillDay{day}", (int)choice);
                }
                if (TaskManager.Instance != null && TaskManager.Instance.IsInitialized)
                {
                    for (int day = 1; day <= 5; day++)
                    {
                        PlayerPrefs.SetInt($"SUNSET16_TaskDay{day}Completed",
                            TaskManager.Instance.IsTaskCompleted(day) ? 1 : 0);
                    }
                }

                if (HiddenRoomManager.Instance != null && HiddenRoomManager.Instance.IsInitialized)
                {
                    Dictionary<string, DoorState> doorStates = HiddenRoomManager.Instance.GetAllDoorStates();
                    string doorData = "";
                    foreach (var kvp in doorStates)
                    {
                        if (doorData.Length > 0) doorData += ",";
                        doorData += $"{kvp.Key}:{(int)kvp.Value}";
                    }
                    PlayerPrefs.SetString("SUNSET16_DoorStates", doorData);

                    string roomTypeData = "";
                    foreach (string roomId in HiddenRoomManager.Instance.GetAllRoomIds())
                    {
                        if (roomTypeData.Length > 0) roomTypeData += ",";
                        roomTypeData += $"{roomId}:{(int)HiddenRoomManager.Instance.GetRoomType(roomId)}";
                    }
                    PlayerPrefs.SetString("SUNSET16_RoomTypes", roomTypeData);
                }

                if (PuzzleManager.Instance != null && PuzzleManager.Instance.IsInitialized)
                {
                    HashSet<string> completedPuzzles = PuzzleManager.Instance.GetCompletedPuzzles();
                    PlayerPrefs.SetString("SUNSET16_CompletedPuzzles", string.Join(",", completedPuzzles));

                    HashSet<string> unlockedLore = PuzzleManager.Instance.GetUnlockedLore();
                    PlayerPrefs.SetString("SUNSET16_UnlockedLore", string.Join(",", unlockedLore));
                }

                if (TabletUIController.Instance != null && TabletUIController.Instance.IsInitialized)
                {
                    PlayerPrefs.SetInt("SUNSET16_TabletUIVisible",
                        TabletUIController.Instance.IsVisible ? 1 : 0);
                }

                PlayerPrefs.SetInt("SUNSET16_SaveExists", 1);
                PlayerPrefs.Save();
                SaveExists = true;
                OnGameSaved?.Invoke();

                Debug.Log("[SAVEMANAGER] Game saved successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SAVEMANAGER] Save failed: {e.Message}");
            }
        }

        public void LoadGame()
        {
            if (!HasSaveData())
            {
                Debug.LogWarning("[SAVEMANAGER] No save data to load");
                return;
            }
            try
            {
                int savedDay = PlayerPrefs.GetInt("SUNSET16_CurrentDay", 1);
                int day = Mathf.Clamp(savedDay, 1, 5);
                int savedPhaseInt = PlayerPrefs.GetInt("SUNSET16_CurrentPhase", 0);
                int phaseInt = Mathf.Clamp(savedPhaseInt, 0, 1);
                DayPhase phase = (DayPhase)phaseInt;
                DayManager.Instance.SetDay(day);
                DayManager.Instance.SetPhase(phase);
                bool isGameOver = PlayerPrefs.GetInt("SUNSET16_IsGameOver", 0) == 1;
                DayManager.Instance.SetGameOver(isGameOver);

                for (int i = 1; i <= 5; i++)
                {
                    int choiceInt = PlayerPrefs.GetInt($"SUNSET16_PillDay{i}", -1);
                    if (choiceInt >= -1 && choiceInt <= 1)
                    {
                        PillChoice choice = (PillChoice)choiceInt;
                        PillStateManager.Instance.SetPillChoice(i, choice);
                    }
                }

                if (TaskManager.Instance != null && TaskManager.Instance.IsInitialized)
                {
                    for (int d = 1; d <= 5; d++)
                    {
                        bool completed = PlayerPrefs.GetInt($"SUNSET16_TaskDay{d}Completed", 0) == 1;
                        TaskManager.Instance.SetTaskCompleted(d, completed);
                    }
                }

                if (HiddenRoomManager.Instance != null && HiddenRoomManager.Instance.IsInitialized)
                {
                    string doorData = PlayerPrefs.GetString("SUNSET16_DoorStates", "");
                    if (!string.IsNullOrEmpty(doorData))
                    {
                        string[] pairs = doorData.Split(',');
                        foreach (string pair in pairs)
                        {
                            string[] parts = pair.Split(':');
                            if (parts.Length == 2 && int.TryParse(parts[1], out int stateInt))
                            {
                                HiddenRoomManager.Instance.SetDoorState(parts[0], (DoorState)stateInt);
                            }
                        }
                    }

                    string roomTypeData = PlayerPrefs.GetString("SUNSET16_RoomTypes", "");
                    if (!string.IsNullOrEmpty(roomTypeData))
                    {
                        string[] pairs = roomTypeData.Split(',');
                        foreach (string pair in pairs)
                        {
                            string[] parts = pair.Split(':');
                            if (parts.Length == 2 && int.TryParse(parts[1], out int typeInt))
                            {
                                HiddenRoomManager.Instance.SetRoomType(parts[0], (RoomType)typeInt);
                            }
                        }
                    }
                }

                if (PuzzleManager.Instance != null && PuzzleManager.Instance.IsInitialized)
                {
                    string puzzleData = PlayerPrefs.GetString("SUNSET16_CompletedPuzzles", "");
                    if (!string.IsNullOrEmpty(puzzleData))
                    {
                        HashSet<string> puzzleIds = new HashSet<string>(puzzleData.Split(','));
                        PuzzleManager.Instance.SetCompletedPuzzles(puzzleIds);
                    }

                    string loreData = PlayerPrefs.GetString("SUNSET16_UnlockedLore", "");
                    if (!string.IsNullOrEmpty(loreData))
                    {
                        HashSet<string> loreIds = new HashSet<string>(loreData.Split(','));
                        PuzzleManager.Instance.SetUnlockedLore(loreIds);
                    }
                }

                if (TabletUIController.Instance != null && TabletUIController.Instance.IsInitialized)
                {
                    bool tabletVisible = PlayerPrefs.GetInt("SUNSET16_TabletUIVisible", 0) == 1;
                    if (tabletVisible)
                        TabletUIController.Instance.OpenTablet();
                    else
                        TabletUIController.Instance.CloseTablet();
                }

                OnGameLoaded?.Invoke();
                Debug.Log($"[SAVEMANAGER] Game loaded - Day {day}, {phase}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SAVEMANAGER] Load failed: {e.Message}");
            }
        }

        public void DeleteSave()
        {
            PlayerPrefs.DeleteKey("SUNSET16_SaveExists");
            PlayerPrefs.DeleteKey("SUNSET16_CurrentDay");
            PlayerPrefs.DeleteKey("SUNSET16_CurrentPhase");
            PlayerPrefs.DeleteKey("SUNSET16_IsGameOver");

            for (int i = 1; i <= 5; i++)
            {
                PlayerPrefs.DeleteKey($"SUNSET16_PillDay{i}");
            }

            for (int i = 1; i <= 5; i++)
            {
                PlayerPrefs.DeleteKey($"SUNSET16_TaskDay{i}Completed");
            }

            PlayerPrefs.DeleteKey("SUNSET16_DoorStates");

            PlayerPrefs.DeleteKey("SUNSET16_CompletedPuzzles");
            PlayerPrefs.DeleteKey("SUNSET16_UnlockedLore");

            PlayerPrefs.DeleteKey("SUNSET16_TabletUIVisible");

            PlayerPrefs.Save();
            SaveExists = false;
            DayManager.Instance.Initialize();
            PillStateManager.Instance.Initialize();

            OnSaveDeleted?.Invoke();

            Debug.Log("[SAVEMANAGER] Save data deleted - managers reset to new game state");
        }

        public void ClearSaveData()
        {
            PlayerPrefs.DeleteKey("SUNSET16_SaveExists");
            PlayerPrefs.DeleteKey("SUNSET16_CurrentDay");
            PlayerPrefs.DeleteKey("SUNSET16_CurrentPhase");
            PlayerPrefs.DeleteKey("SUNSET16_IsGameOver");

            for (int i = 1; i <= 5; i++)
            {
                PlayerPrefs.DeleteKey($"SUNSET16_PillDay{i}");
                PlayerPrefs.DeleteKey($"SUNSET16_TaskDay{i}Completed");
            }

            PlayerPrefs.DeleteKey("SUNSET16_DoorStates");
            PlayerPrefs.DeleteKey("SUNSET16_RoomTypes");
            PlayerPrefs.DeleteKey("SUNSET16_CompletedPuzzles");
            PlayerPrefs.DeleteKey("SUNSET16_UnlockedLore");

            PlayerPrefs.DeleteKey("SUNSET16_TabletUIVisible");

            PlayerPrefs.Save();
            SaveExists = false;

            Debug.Log("[SAVEMANAGER] Save data cleared from main menu");
        }

        protected override void Awake()
        {
            base.Awake();
        }
    }
}