using UnityEngine;
using System;

namespace SUNSET16.Core
{
    public class DayManager : Singleton<DayManager>
    {
        public int CurrentDay { get; private set; }
        public DayPhase CurrentPhase { get; private set; }
        public bool IsInitialized { get; private set; }
        public event Action<int> OnDayChanged;
        public event Action<DayPhase> OnPhaseChanged;
        public event Action OnGameComplete;
        public void Initialize()
        {
            CurrentDay = 1;
            CurrentPhase = DayPhase.Morning;
            IsInitialized = true;
            Debug.Log("[DAYMANAGER] Initialized - Day 1, Morning");
        }

        public void AdvancePhase()
        {
            if (CurrentDay == 5 && CurrentPhase == DayPhase.Night)
            {
                Debug.LogWarning("[DAYMANAGER] Game complete - cannot advance further");
                OnGameComplete?.Invoke();
                return;
            }

            switch (CurrentPhase)
            {
                case DayPhase.Morning:
                    CurrentPhase = DayPhase.Night;
                    OnPhaseChanged?.Invoke(CurrentPhase);

                    Debug.Log($"[DAYMANAGER] Advanced to Day {CurrentDay} - Night phase");
                    break;

                case DayPhase.Night:
                    CurrentDay++;
                    CurrentPhase = DayPhase.Morning;
                    OnDayChanged?.Invoke(CurrentDay);
                    OnPhaseChanged?.Invoke(CurrentPhase);

                    Debug.Log($"[DAYMANAGER] Advanced to Day {CurrentDay} - Morning phase");
                    break;
            }
        }

        public void SetDay(int day)
        {
            CurrentDay = Mathf.Clamp(day, 1, 5);

            Debug.Log($"[DAYMANAGER] Day set to {CurrentDay}");
        }

        public void SetPhase(DayPhase phase)
        {
            CurrentPhase = phase;
            Debug.Log($"[DAYMANAGER] Phase set to {CurrentPhase}");
        }

        protected override void Awake()
        {
            base.Awake(); 
        }
    }
}
