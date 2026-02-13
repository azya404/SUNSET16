using UnityEngine;
using System;

namespace SUNSET16.Core
{
    public class DayManager : Singleton<DayManager>
    {
        public int CurrentDay { get; private set; }
        public DayPhase CurrentPhase { get; private set; }
        public bool IsInitialized { get; private set; }
        public bool IsGameOver { get; private set; }
        public event Action<int> OnDayChanged;
        public event Action<DayPhase> OnPhaseChanged;
        public event Action OnGameComplete;
        public event Action<int> OnGameEndedEarly;
        public event Action OnNightPhaseOnPill;
        public event Action OnNightPhaseOffPill;

        public void Initialize()
        {
            CurrentDay = 1;
            CurrentPhase = DayPhase.Morning;
            IsGameOver = false;
            IsInitialized = true;
            Debug.Log("[DAYMANAGER] Initialized - Day 1, Morning");
        }

        public void AdvancePhase()
        {
            if (IsGameOver)
            {
                Debug.LogWarning("[DAYMANAGER] Game is over - cannot advance further");
                return;
            }

            switch (CurrentPhase)
            {
                case DayPhase.Morning:
                    if (!PillStateManager.Instance.HasTakenPillToday())
                    {
                        Debug.LogWarning($"[DAYMANAGER] Cannot advance Day {CurrentDay} " +
                            "Morning -> Night: pill choice required first");
                        return;
                    }

                    CurrentPhase = DayPhase.Night;
                    OnPhaseChanged?.Invoke(CurrentPhase);
                    Debug.Log($"[DAYMANAGER] Advanced to Day {CurrentDay} - Night phase");
                    if (PillStateManager.Instance.IsEndingReached)
                    {
                        Debug.Log($"[DAYMANAGER] Ending reached at Day {CurrentDay} Night!");
                        IsGameOver = true;
                        if (CurrentDay < 5)
                        {
                            OnGameEndedEarly?.Invoke(CurrentDay);
                        }
                        else
                        {
                            OnGameComplete?.Invoke();
                        }
                        return;
                    }
                    break;

                case DayPhase.Night:
                    if (CurrentDay >= 5)
                    {
                        // On final day, ensure discovery happens before game complete
                        PillChoice day5Choice = PillStateManager.Instance.GetPillChoice(5);
                        if (day5Choice == PillChoice.NotTaken)
                        {
                            OnNightPhaseOffPill?.Invoke();  // Allow final discoveries
                        }

                        Debug.LogWarning("[DAYMANAGER] Day 5 Night - game complete");
                        IsGameOver = true;
                        OnGameComplete?.Invoke();
                        return;
                    }

                    CurrentDay++;
                    CurrentPhase = DayPhase.Morning;
                    OnDayChanged?.Invoke(CurrentDay);
                    OnPhaseChanged?.Invoke(CurrentPhase);
                    Debug.Log($"[DAYMANAGER] Advanced to Day {CurrentDay} - Morning phase");
                    break;
            }
        }

        public void TaskCompleted()
        {
            if (IsGameOver)
            {
                Debug.LogWarning("[DAYMANAGER] TaskCompleted called but game is already over");
                return;
            }

            if (CurrentPhase != DayPhase.Morning)
            {
                Debug.LogWarning("[DAYMANAGER] TaskCompleted called but not in Morning phase");
                return;
            }

            if (!PillStateManager.Instance.HasTakenPillToday())
            {
                Debug.LogWarning("[DAYMANAGER] TaskCompleted called but no pill choice made today");
                return;
            }

            CurrentPhase = DayPhase.Night;
            OnPhaseChanged?.Invoke(CurrentPhase);
            Debug.Log($"[DAYMANAGER] Task completed - advanced to Day {CurrentDay} Night");

            // Fire pill-related callbacks FIRST (allows discovery to happen)
            PillChoice todayChoice = PillStateManager.Instance.GetPillChoice(CurrentDay);
            if (todayChoice == PillChoice.Taken)
            {
                OnNightPhaseOnPill?.Invoke();
                Debug.Log("[DAYMANAGER] Night phase (on-pill): bedroom restriction active");
            }
            else if (todayChoice == PillChoice.NotTaken)
            {
                OnNightPhaseOffPill?.Invoke();
                Debug.Log("[DAYMANAGER] Night phase (off-pill): hidden rooms accessible");
            }

            // NOW check if game is ending (after discovery completes)
            if (PillStateManager.Instance.IsEndingReached)
            {
                Debug.Log($"[DAYMANAGER] Ending reached at Day {CurrentDay} Night!");
                IsGameOver = true;
                if (CurrentDay < 5)
                {
                    OnGameEndedEarly?.Invoke(CurrentDay);
                }
                else
                {
                    OnGameComplete?.Invoke();
                }
                return;
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

        public void SetGameOver(bool value)
        {
            IsGameOver = value;
            Debug.Log($"[DAYMANAGER] IsGameOver set to {value}");
        }

        protected override void Awake()
        {
            base.Awake();
        }
    }
}
