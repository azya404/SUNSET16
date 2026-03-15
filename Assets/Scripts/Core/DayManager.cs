/*
THE CALENDAR - tracks what day it is (1-5) and what phase (Morning/Night)
this is how the game knows what "time" it is and controls the flow of each day

the game runs on a 5-day cycle, each day has 2 phases:
Morning: wake up -> pill choice -> navigate to task room -> complete task
Night: if on-pill go to bed, if off-pill explore hidden rooms -> sleep -> next day

originally the brainstorming doc had 3 phases (Morning, Day, Night)
but we merged Morning and Day cos theyre basically the same gameplay-wise

the AdvancePhase() method is the HEART of the game loop - it controls day progression
and has a bunch of gate checks to make sure the player has done everything they need to
before advancing (pill choice made, task completed, puzzle completed if off-pill, etc)

EVENTS are how other systems know when the day/phase changes:
- OnDayChanged: fires when we move to a new day (LightingController resets, etc)
- OnPhaseChanged: fires when phase switches (AudioManager changes music, etc)
- OnGameComplete: fires when game reaches natural end at Day 5 Night
- OnGameEndedEarly: fires when ending triggered before Day 5 (3+ pills threshold)
- OnNightPhaseOnPill/OffPill: fires to tell systems what kind of night it is

HOW THIS EVOLVED:
- added forced task completion checks (originally you could advance freely)
- added puzzle completion checks for off-pill nights
- added HiddenRoomManager integration (must enter discovered room before advancing)
- TaskCompleted() method now directly handles the Morning->Night transition with pill-specific events
- SetDay/SetPhase/SetGameOver added for save/load system

TODO: the AdvancePhase Night->Morning transition has a LOT of checks now, might want to refactor
TODO: add transition animations/effects between phases (fade to black, etc)
*/
using UnityEngine;
using System;

namespace SUNSET16.Core
{
    //inherits from Singleton so we get global access via DayManager.Instance
    public class DayManager : Singleton<DayManager>
    {
        [Header("Game Settings")]
        [Tooltip("Which day the game starts on. Set to 1 for the full game.")]
        [SerializeField] private int _startingDay = 1;

        public int CurrentDay { get; private set; }       //what day it is (1-5), private set so only WE can change it
        public DayPhase CurrentPhase { get; private set; } //Morning or Night, drives the entire game flow
        public bool IsInitialized { get; private set; }    //true after Initialize() runs, other systems check this
        public bool IsGameOver { get; private set; }       //true when we hit an ending condition (no more advancing)

        //events that other systems subscribe to - this is our event-driven architecture in action
        public event Action<int> OnDayChanged;          //fires when day number changes (day 1 -> day 2 etc)
        public event Action<DayPhase> OnPhaseChanged;   //fires when phase changes (Morning -> Night or Night -> Morning)
        public event Action OnGameComplete;              //fires when player finishes day 5 (natural ending)
        public event Action<int> OnGameEndedEarly;       //fires when game ends before day 5 (pill threshold met)
        public event Action OnNightPhaseOnPill;          //fires specifically for on-pill nights (bedroom restriction)
        public event Action OnNightPhaseOffPill;         //fires specifically for off-pill nights (hidden room access)

        //called by GameManager during initialization sequence
        //sets everything to the start-of-game state
        public void Initialize()
        {
            CurrentDay = Mathf.Clamp(_startingDay, 1, 5);
            CurrentPhase = DayPhase.Morning;
            IsGameOver = false;
            IsInitialized = true;
            Debug.Log($"[DAYMANAGER] Initialized - Day {CurrentDay}, Morning");
        }

        /*
        THE MAIN GAME LOOP CONTROLLER
        this method is called when the game wants to move to the next phase
        it has a TON of gate checks to make sure the player has done everything required
        before letting them advance - this prevents sequence breaking

        the flow:
        Morning -> Night: requires pill choice AND task completion
        Night -> next day Morning: requires task completion, AND if off-pill:
            must enter hidden room AND complete puzzle before advancing
        */
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
                    //GATE CHECK 1: player must have made their pill choice before leaving morning
                    if (!PillStateManager.Instance.HasTakenPillToday())
                    {
                        Debug.LogWarning($"[DAYMANAGER] Cannot advance Day {CurrentDay} " +
                            "Morning -> Night: pill choice required first");
                        return;
                    }

                    //GATE CHECK 2: daily task must be completed before advancing to night
                    if (TaskManager.Instance == null || !TaskManager.Instance.IsTaskCompleted(CurrentDay))
                    {
                        Debug.LogWarning($"[DAYMANAGER] Cannot advance Day {CurrentDay} Morning -> Night: task must be completed first");
                        return;
                    }

                    //all checks passed - advance to Night phase
                    CurrentPhase = DayPhase.Night;
                    OnPhaseChanged?.Invoke(CurrentPhase); //tells AudioManager, LightingController, etc
                    Debug.Log($"[DAYMANAGER] Advanced to Day {CurrentDay} - Night phase");
                    break;

                case DayPhase.Night:
                    //GATE CHECK 3: task must also be done for Night->Morning transition
                    //(this is a safety check, should already be done from Morning->Night)
                    if (TaskManager.Instance == null || !TaskManager.Instance.IsTaskCompleted(CurrentDay))
                    {
                        Debug.LogWarning($"[DAYMANAGER] Cannot advance Day {CurrentDay} Night -> Day {CurrentDay + 1} Morning: task must be completed first");
                        return;
                    }

                    //GATE CHECKS 4-5: OFF-PILL NIGHT RESTRICTIONS
                    //if player refused pill today, they MUST enter the hidden room AND complete its puzzle
                    //this is the key gameplay difference: off-pill nights have mandatory exploration
                    PillChoice todayChoice = PillStateManager.Instance.GetPillChoice(CurrentDay);
                    if (todayChoice == PillChoice.NotTaken)
                    {
                        //check if HiddenRoomManager exists and is set up
                        if (HiddenRoomManager.Instance != null && HiddenRoomManager.Instance.IsInitialized)
                        {
                            //GATE CHECK 4: must have physically entered the hidden room this night
                            if (!HiddenRoomManager.Instance.HasEnteredRoomThisNight())
                            {
                                Debug.LogWarning($"[DAYMANAGER] Cannot advance Day {CurrentDay} Night -> Day {CurrentDay + 1} Morning: must enter the discovered hidden room first (off-pill restriction)");
                                return;
                            }

                            //GATE CHECK 5: must have completed this days puzzle in the hidden room
                            //only blocks if a puzzle asset actually exists for this day - if no asset is assigned
                            //in PuzzleManager, the gate is skipped (temporary until all puzzle assets are built)
                            //TODO: once puzzle_day_N assets exist for all days, HasPuzzleForDay will always return true
                            if (PuzzleManager.Instance != null && PuzzleManager.Instance.IsInitialized)
                            {
                                string expectedPuzzleId = $"puzzle_day_{CurrentDay}"; //puzzle IDs follow this naming convention
                                if (PuzzleManager.Instance.HasPuzzleForDay(CurrentDay) &&
                                    !PuzzleManager.Instance.IsPuzzleCompleted(expectedPuzzleId))
                                {
                                    Debug.LogWarning($"[DAYMANAGER] Cannot advance Day {CurrentDay} Night -> Day {CurrentDay + 1} Morning: must complete this day's hidden room puzzle first (off-pill restriction)");
                                    return;
                                }
                            }
                        }
                    }

                    //CHECK FOR ENDING: if pill threshold met, game might be over
                    if (PillStateManager.Instance.IsEndingReached)
                    {
                        Debug.Log($"[DAYMANAGER] Ending reached - game ending at Day {CurrentDay} Night");
                        IsGameOver = true;
                        if (CurrentDay < 5)
                        {
                            OnGameEndedEarly?.Invoke(CurrentDay); //early ending, didnt make it to day 5
                        }
                        else
                        {
                            OnGameComplete?.Invoke(); //reached day 5 AND ending triggered
                        }
                        return;
                    }

                    //SPECIAL CASE: Day 5 Night is the final night regardless of ending status
                    if (CurrentDay >= 5)
                    {
                        PillChoice day5Choice = PillStateManager.Instance.GetPillChoice(5);
                        if (day5Choice == PillChoice.NotTaken)
                        {
                            OnNightPhaseOffPill?.Invoke(); //off-pill on final night
                        }

                        Debug.LogWarning("[DAYMANAGER] Day 5 Night - game complete");
                        IsGameOver = true;
                        OnGameComplete?.Invoke();
                        return;
                    }

                    //NORMAL ADVANCEMENT: move to the next day
                    CurrentDay++;
                    CurrentPhase = DayPhase.Morning;
                    OnDayChanged?.Invoke(CurrentDay);      //tell everyone the day changed
                    OnPhaseChanged?.Invoke(CurrentPhase);  //tell everyone were back to Morning
                    Debug.Log($"[DAYMANAGER] Advanced to Day {CurrentDay} - Morning phase");
                    break;
            }
        }

        /*
        called when the daily task is completed (from TaskManager or ManagerTester)
        this is the "shortcut" that goes straight from Morning to Night
        without going through AdvancePhase() - cos the task completion IS the advancement trigger
        also fires the appropriate night event (on-pill vs off-pill) so systems know what to do
        */
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

            //advance to Night phase
            CurrentPhase = DayPhase.Night;
            OnPhaseChanged?.Invoke(CurrentPhase);
            Debug.Log($"[DAYMANAGER] Task completed - advanced to Day {CurrentDay} Night");

            //fire pill-specific night events so systems know what kind of night this is
            PillChoice todayChoice = PillStateManager.Instance.GetPillChoice(CurrentDay);
            if (todayChoice == PillChoice.Taken)
            {
                OnNightPhaseOnPill?.Invoke(); //on-pill night: player is restricted to bedroom
                Debug.Log("[DAYMANAGER] Night phase (on-pill): bedroom restriction active");
            }
            else if (todayChoice == PillChoice.NotTaken)
            {
                OnNightPhaseOffPill?.Invoke(); //off-pill night: hidden rooms are accessible!
                Debug.Log("[DAYMANAGER] Night phase (off-pill): hidden rooms accessible");
            }
        }

        //these setter methods exist purely for the save/load system
        //SaveManager calls these when restoring a saved game state
        //they bypass all the gate checks cos were loading a known-good state

        public void SetDay(int day)
        {
            CurrentDay = Mathf.Clamp(day, 1, 5); //clamp to valid range just in case
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
            base.Awake(); //Singleton handles instance management
        }
    }
}
