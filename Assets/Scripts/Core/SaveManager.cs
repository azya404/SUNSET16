using UnityEngine;
using System;

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
                for (int day = 1; day <= 5; day++)
                {
                    PillChoice choice = PillStateManager.Instance.GetPillChoice(day);
                    PlayerPrefs.SetInt($"SUNSET16_PillDay{day}", (int)choice);
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
                for (int i = 1; i <= 5; i++)
                {
                    int choiceInt = PlayerPrefs.GetInt($"SUNSET16_PillDay{i}", -1);
                    if (choiceInt >= -1 && choiceInt <= 1)
                    {
                        PillChoice choice = (PillChoice)choiceInt;
                        PillStateManager.Instance.SetPillChoice(i, choice);
                    }
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

            for (int i = 1; i <= 5; i++)
            {
                PlayerPrefs.DeleteKey($"SUNSET16_PillDay{i}");
            }

            PlayerPrefs.Save();
            SaveExists = false;
            OnSaveDeleted?.Invoke();

            Debug.Log("[SAVEMANAGER] Save data deleted");
        }

        protected override void Awake()
        {
            base.Awake();
        }
    }
}