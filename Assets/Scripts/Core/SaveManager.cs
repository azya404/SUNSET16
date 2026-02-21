/*
THE PERSISTENCE ENGINE - handles saving and loading ALL game progress using PlayerPrefs
this is how the game remembers where you left off between sessions

WHY PLAYERPREFS AND NOT FILE I/O:
- we target WebGL (browser) where you CANNOT write to the filesystem
- PlayerPrefs maps to browser localStorage on WebGL, which persists across sessions
- on desktop, PlayerPrefs stores to the registry (Windows) or plist (Mac)
- its limited to simple key-value pairs (string, int, float) but thats enough for our game

ONE SAVE SLOT DESIGN:
- we only have ONE save slot (no save file management, no multiple profiles)
- this keeps things simple for a 5-day game thats meant to be replayed
- all keys are prefixed with "SUNSET16_" to avoid collisions with other PlayerPrefs

WHAT GETS SAVED:
- current day and phase (from DayManager)
- game over state (from DayManager)
- all 5 days of pill choices (from PillStateManager)
- task completion status per day (from TaskManager)
- door states for hidden rooms (from HiddenRoomManager) - serialized as comma-separated strings
- room types (from HiddenRoomManager) - same serialization
- completed puzzles and unlocked lore (from PuzzleManager) - comma-separated HashSets
- tablet visibility state (from TabletUIController)

WHAT DOES NOT GET SAVED (handled by SettingsManager separately):
- volume settings, brightness - these are user preferences, not game progress

WEBGL CRITICAL:
- PlayerPrefs.Save() is called after EVERY SaveGame/DeleteSave/ClearSaveData
- without explicit Save(), WebGL will lose all data when the tab closes
- this is different from desktop where Unity auto-saves periodically

HOW THIS EVOLVED:
- originally only had day/phase/pill saves
- added task completion, door states, puzzles, lore, tablet state as those systems were built
- added ClearSaveData() separately from DeleteSave() for main menu use
- the string serialization for door states and room types was a creative workaround
  cos PlayerPrefs doesnt support arrays or complex types

TODO: the string serialization is fragile - if a room ID contains "," or ":" itll break
TODO: might want to add a save version number for future migration
TODO: error handling could be more granular (right now we just catch everything)
*/
using UnityEngine;
using System;
using System.Collections.Generic;

namespace SUNSET16.Core
{
    public class SaveManager : Singleton<SaveManager>
    {
        public bool SaveExists { get; private set; }  //true if we found save data in PlayerPrefs
        public bool IsInitialized { get; private set; }
        public event Action OnGameSaved;   //fires after a successful save (for UI feedback)
        public event Action OnGameLoaded;  //fires after a successful load (for systems to refresh)
        public event Action OnSaveDeleted; //fires after save is deleted (for UI reset)

        //called by GameManager during initialization
        //just checks if save data exists and marks itself ready
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

        //checks the "master key" that indicates whether a save exists
        //we set this to 1 when saving and delete it when clearing save data
        public bool HasSaveData()
        {
            return PlayerPrefs.GetInt("SUNSET16_SaveExists", 0) == 1;
        }

        /*
        SAVE GAME - captures the entire game state into PlayerPrefs
        this is called:
        1. when the application quits (GameManager.OnApplicationQuit)
        2. when the player manually saves (if we add that feature)
        3. could be called on phase transitions for safety

        the try-catch wraps everything cos if ANY save fails,
        we dont want to corrupt partial data - either all saves work or none do
        (though tbh the catch just logs the error and moves on lol)
        */
        public void SaveGame()
        {
            //safety check - cant save if managers arent ready
            if (!GameManager.Instance.IsInitialized)
            {
                Debug.LogError("[SAVEMANAGER] Cannot save - managers not initialized");
                return;
            }

            try
            {
                //=== SAVE CORE STATE (DayManager) ===
                int currentDay = DayManager.Instance.CurrentDay;
                DayPhase currentPhase = DayManager.Instance.CurrentPhase;
                PlayerPrefs.SetInt("SUNSET16_CurrentDay", currentDay);
                PlayerPrefs.SetInt("SUNSET16_CurrentPhase", (int)currentPhase); //cast enum to int for storage
                PlayerPrefs.SetInt("SUNSET16_IsGameOver", DayManager.Instance.IsGameOver ? 1 : 0); //bool as int (0/1)

                //=== SAVE PILL CHOICES (PillStateManager) ===
                //save each days choice individually (5 separate keys)
                for (int day = 1; day <= 5; day++)
                {
                    PillChoice choice = PillStateManager.Instance.GetPillChoice(day);
                    PlayerPrefs.SetInt($"SUNSET16_PillDay{day}", (int)choice); //None=-1, NotTaken=0, Taken=1
                }

                //=== SAVE TASK COMPLETION (TaskManager) ===
                //only if TaskManager exists and is ready (its optional for testing)
                if (TaskManager.Instance != null && TaskManager.Instance.IsInitialized)
                {
                    for (int day = 1; day <= 5; day++)
                    {
                        PlayerPrefs.SetInt($"SUNSET16_TaskDay{day}Completed",
                            TaskManager.Instance.IsTaskCompleted(day) ? 1 : 0); //bool as int
                    }
                }

                //=== SAVE DOOR STATES (HiddenRoomManager) ===
                //this is where it gets creative - we serialize door states as a comma-separated string
                //format: "room1:0,room2:1,room3:2" where the number is the DoorState enum value
                if (HiddenRoomManager.Instance != null && HiddenRoomManager.Instance.IsInitialized)
                {
                    Dictionary<string, DoorState> doorStates = HiddenRoomManager.Instance.GetAllDoorStates();
                    string doorData = "";
                    foreach (var kvp in doorStates)
                    {
                        if (doorData.Length > 0) doorData += ","; //comma separator between entries
                        doorData += $"{kvp.Key}:{(int)kvp.Value}"; //format: roomId:stateInt
                    }
                    PlayerPrefs.SetString("SUNSET16_DoorStates", doorData);

                    //same serialization for room types
                    string roomTypeData = "";
                    foreach (string roomId in HiddenRoomManager.Instance.GetAllRoomIds())
                    {
                        if (roomTypeData.Length > 0) roomTypeData += ",";
                        roomTypeData += $"{roomId}:{(int)HiddenRoomManager.Instance.GetRoomType(roomId)}";
                    }
                    PlayerPrefs.SetString("SUNSET16_RoomTypes", roomTypeData);
                }

                //=== SAVE PUZZLE/LORE PROGRESS (PuzzleManager) ===
                //HashSets get serialized to comma-separated strings using string.Join
                if (PuzzleManager.Instance != null && PuzzleManager.Instance.IsInitialized)
                {
                    HashSet<string> completedPuzzles = PuzzleManager.Instance.GetCompletedPuzzles();
                    PlayerPrefs.SetString("SUNSET16_CompletedPuzzles", string.Join(",", completedPuzzles));

                    HashSet<string> unlockedLore = PuzzleManager.Instance.GetUnlockedLore();
                    PlayerPrefs.SetString("SUNSET16_UnlockedLore", string.Join(",", unlockedLore));
                }

                //=== SAVE UI STATE (TabletUIController) ===
                if (TabletUIController.Instance != null && TabletUIController.Instance.IsInitialized)
                {
                    PlayerPrefs.SetInt("SUNSET16_TabletUIVisible",
                        TabletUIController.Instance.IsVisible ? 1 : 0);
                }

                //mark save as existing and flush to disk/localStorage
                PlayerPrefs.SetInt("SUNSET16_SaveExists", 1); //the master key
                PlayerPrefs.Save(); //CRITICAL for WebGL
                SaveExists = true;
                OnGameSaved?.Invoke(); //notify any listeners (future: save confirmation UI)

                Debug.Log("[SAVEMANAGER] Game saved successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SAVEMANAGER] Save failed: {e.Message}");
            }
        }

        /*
        LOAD GAME - restores game state from PlayerPrefs into all managers
        called by GameManager during initialization IF a save exists
        
        the load order doesnt matter much cos we just set values -
        the managers are already initialized with defaults, we just overwrite them
        
        uses Mathf.Clamp for safety in case saved values are somehow corrupted
        */
        public void LoadGame()
        {
            if (!HasSaveData())
            {
                Debug.LogWarning("[SAVEMANAGER] No save data to load");
                return;
            }
            try
            {
                //=== LOAD CORE STATE (DayManager) ===
                int savedDay = PlayerPrefs.GetInt("SUNSET16_CurrentDay", 1);
                int day = Mathf.Clamp(savedDay, 1, 5); //safety clamp in case of corruption
                int savedPhaseInt = PlayerPrefs.GetInt("SUNSET16_CurrentPhase", 0);
                int phaseInt = Mathf.Clamp(savedPhaseInt, 0, 1); //0=Morning, 1=Night
                DayPhase phase = (DayPhase)phaseInt; //cast back from int to enum
                DayManager.Instance.SetDay(day);     //uses the setter methods (bypasses gate checks)
                DayManager.Instance.SetPhase(phase);
                bool isGameOver = PlayerPrefs.GetInt("SUNSET16_IsGameOver", 0) == 1;
                DayManager.Instance.SetGameOver(isGameOver);

                //=== LOAD PILL CHOICES (PillStateManager) ===
                for (int i = 1; i <= 5; i++)
                {
                    int choiceInt = PlayerPrefs.GetInt($"SUNSET16_PillDay{i}", -1); //default -1 = None
                    if (choiceInt >= -1 && choiceInt <= 1) //validate: must be -1, 0, or 1
                    {
                        PillChoice choice = (PillChoice)choiceInt;
                        PillStateManager.Instance.SetPillChoice(i, choice); //uses the silent setter (no events)
                    }
                }

                //=== LOAD TASK COMPLETION (TaskManager) ===
                if (TaskManager.Instance != null && TaskManager.Instance.IsInitialized)
                {
                    for (int d = 1; d <= 5; d++)
                    {
                        bool completed = PlayerPrefs.GetInt($"SUNSET16_TaskDay{d}Completed", 0) == 1;
                        TaskManager.Instance.SetTaskCompleted(d, completed);
                    }
                }

                //=== LOAD DOOR STATES (HiddenRoomManager) ===
                //reverse the comma-separated string serialization
                if (HiddenRoomManager.Instance != null && HiddenRoomManager.Instance.IsInitialized)
                {
                    string doorData = PlayerPrefs.GetString("SUNSET16_DoorStates", "");
                    if (!string.IsNullOrEmpty(doorData))
                    {
                        string[] pairs = doorData.Split(','); //split into "roomId:stateInt" pairs
                        foreach (string pair in pairs)
                        {
                            string[] parts = pair.Split(':'); //split into roomId and stateInt
                            if (parts.Length == 2 && int.TryParse(parts[1], out int stateInt))
                            {
                                HiddenRoomManager.Instance.SetDoorState(parts[0], (DoorState)stateInt);
                            }
                        }
                    }

                    //same deserialization for room types
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

                //=== LOAD PUZZLE/LORE PROGRESS (PuzzleManager) ===
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

                //=== LOAD UI STATE (TabletUIController) ===
                if (TabletUIController.Instance != null && TabletUIController.Instance.IsInitialized)
                {
                    bool tabletVisible = PlayerPrefs.GetInt("SUNSET16_TabletUIVisible", 0) == 1;
                    if (tabletVisible)
                        TabletUIController.Instance.OpenTablet();
                    else
                        TabletUIController.Instance.CloseTablet();
                }

                OnGameLoaded?.Invoke(); //notify everyone that state has been restored
                Debug.Log($"[SAVEMANAGER] Game loaded - Day {day}, {phase}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SAVEMANAGER] Load failed: {e.Message}");
            }
        }

        /*
        DELETE SAVE - clears all saved game data AND resets managers to fresh state
        used when the player wants to start completely over (in-game "delete save" option)
        
        difference from ClearSaveData:
        - DeleteSave: clears PlayerPrefs AND reinitializes DayManager + PillStateManager
        - ClearSaveData: ONLY clears PlayerPrefs (for main menu use where managers might not be ready)
        */
        public void DeleteSave()
        {
            //delete all the individual keys we saved
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

            PlayerPrefs.Save(); //flush the deletions to localStorage
            SaveExists = false;

            //reinitialize core managers to fresh state (day 1, morning, no pills)
            DayManager.Instance.Initialize();
            PillStateManager.Instance.Initialize();

            OnSaveDeleted?.Invoke();

            Debug.Log("[SAVEMANAGER] Save data deleted - managers reset to new game state");
        }

        /*
        CLEAR SAVE DATA - clears ONLY PlayerPrefs, does NOT reinitialize managers
        this is specifically for the main menu where we want to clear save data
        but the managers might be in a different state or the game hasnt started yet
        
        NOTE: this also cleans up the "SUNSET16_RoomTypes" key which DeleteSave misses
        (probably a bug in DeleteSave tbh - TODO: make them consistent)
        */
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
            PlayerPrefs.DeleteKey("SUNSET16_RoomTypes"); //this one is missing from DeleteSave!
            PlayerPrefs.DeleteKey("SUNSET16_CompletedPuzzles");
            PlayerPrefs.DeleteKey("SUNSET16_UnlockedLore");

            PlayerPrefs.DeleteKey("SUNSET16_TabletUIVisible");

            PlayerPrefs.Save(); //flush to localStorage
            SaveExists = false;

            Debug.Log("[SAVEMANAGER] Save data cleared from main menu");
        }

        protected override void Awake()
        {
            base.Awake(); //Singleton handles instance management
        }
    }
}
