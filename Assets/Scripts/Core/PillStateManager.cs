using UnityEngine;
using System;
using System.Collections.Generic;

namespace SUNSET16.Core
{
    public class PillStateManager : Singleton<PillStateManager>
    {
        private Dictionary<int, PillChoice> _pillChoices;

        private const int ENDING_THRESHOLD = 3;

        public bool IsInitialized { get; private set; }

        public bool IsEndingReached => GetPillsTakenCount() >= ENDING_THRESHOLD
                                   || GetPillsRefusedCount() >= ENDING_THRESHOLD;

        public event Action<int, PillChoice> OnPillTaken;
        public event Action<string> OnEndingReached;

        public void Initialize()
        {
            _pillChoices = new Dictionary<int, PillChoice>();
            for (int i = 1; i <= 5; i++)
            {
                _pillChoices[i] = PillChoice.None;
            }

            IsInitialized = true;
            Debug.Log("[PILLSTATEMANAGER] Initialized");
        }

        public bool IsForcedChoice(int day)
        {
            return day <= 2;
        }

        public PillChoice GetForcedChoice(int day)
        {
            switch (day)
            {
                case 1: return PillChoice.Taken;  
                case 2: return PillChoice.NotTaken;   
                default: return PillChoice.None;      
            }
        }

        public void TakePill(PillChoice choice)
        {
            int currentDay = DayManager.Instance.CurrentDay;

            if (HasTakenPillToday())
            {
                Debug.LogWarning($"[PILLSTATEMANAGER] Pill already chosen on Day {currentDay}");
                return;
            }

            if (choice == PillChoice.None)
            {
                Debug.LogWarning("[PILLSTATEMANAGER] Cannot take 'None' pill");
                return;
            }

            if (IsEndingReached)
            {
                Debug.LogWarning("[PILLSTATEMANAGER] Game has already reached an ending - no more choices");
                return;
            }

            if (IsForcedChoice(currentDay))
            {
                PillChoice forced = GetForcedChoice(currentDay);
                if (choice != forced)
                {
                    Debug.LogWarning($"[PILLSTATEMANAGER] Day {currentDay} is scripted - must choose {forced}");
                    return;
                }
            }

            _pillChoices[currentDay] = choice;
            OnPillTaken?.Invoke(currentDay, choice);
            Debug.Log($"[PILLSTATEMANAGER] Day {currentDay}: {(IsForcedChoice(currentDay) ? "Forced" : "Player chose")} {choice}");

            CheckForEnding();
        }

        public bool HasTakenPillToday()
        {
            int currentDay = DayManager.Instance.CurrentDay;
            return _pillChoices.ContainsKey(currentDay) && _pillChoices[currentDay] != PillChoice.None;
        }

        public PillChoice GetPillChoice(int day)
        {
            if (day < 1 || day > 5)
            {
                Debug.LogWarning($"[PILLSTATEMANAGER] Invalid day {day}");
                return PillChoice.None;
            }
            return _pillChoices.ContainsKey(day) ? _pillChoices[day] : PillChoice.None;
        }

        public void SetPillChoice(int day, PillChoice choice)
        {
            if (day < 1 || day > 5)
            {
                Debug.LogWarning($"[PILLSTATEMANAGER] Invalid day {day}");
                return;
            }

            _pillChoices[day] = choice;
        }

        public int GetPillsTakenCount()
        {
            int count = 0;

            foreach (var choice in _pillChoices.Values)
            {
                if (choice == PillChoice.Taken)
                {
                    count++;
                }
            }

            return count;
        }

        public int GetPillsRefusedCount()
        {
            int count = 0;

            foreach (var choice in _pillChoices.Values)
            {
                if (choice == PillChoice.NotTaken)
                {
                    count++;
                }
            }

            return count;
        }

        public string DetermineEnding()
        {
            int takenCount = GetPillsTakenCount();
            int refusedCount = GetPillsRefusedCount();

            if (takenCount >= ENDING_THRESHOLD)
            {
                return "Bad"; 
            }

            if (refusedCount >= ENDING_THRESHOLD)
            {
                return "Good"; 
            }

            return "Undetermined";
        }

        public bool CheckForEnding()
        {
            if (!IsEndingReached) return false;

            string ending = DetermineEnding();
            Debug.Log($"[PILLSTATEMANAGER] === ENDING REACHED: {ending} ===");
            OnEndingReached?.Invoke(ending);
            return true;
        }

        protected override void Awake()
        {
            base.Awake();
        }
    }
}