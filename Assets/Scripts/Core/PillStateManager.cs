using UnityEngine;
using System;
using System.Collections.Generic;

namespace SUNSET16.Core
{
    public class PillStateManager : Singleton<PillStateManager>
    {
        private Dictionary<int, PillChoice> _pillChoices;

        public bool IsInitialized { get; private set; }
        public event Action<int, PillChoice> OnPillTaken;
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

        public void TakePill(PillChoice choice)
        {
            int currentDay = DayManager.Instance.CurrentDay;
            if (HasTakenPillToday())
            {
                Debug.LogWarning($"[PILLSTATEMANAGER] Pill already taken on Day {currentDay}");
                return;
            }
            if (choice == PillChoice.None)
            {
                Debug.LogWarning("[PILLSTATEMANAGER] Cannot take 'None' pill");
                return;
            }
            _pillChoices[currentDay] = choice;
            OnPillTaken?.Invoke(currentDay, choice);
            Debug.Log($"[PILLSTATEMANAGER] Day {currentDay}: {choice} pill taken");
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
            if (takenCount == 3)
            {
                return "AlwaysTaken_Conformity";
            }

            if (refusedCount == 3)
            {
                return "AlwaysRefused_Rebellion";
            }

            if (takenCount > refusedCount)
            {
                return "MostlyTaken_Compliance";
            }

            if (refusedCount > takenCount)
            {
                return "MostlyRefused_Resistance";
            }

            return "Balanced_Undecided";
        }

        protected override void Awake()
        {
            base.Awake();
        }
    }
}