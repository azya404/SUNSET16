/*
THE CHOICE TRACKER - manages the pill mechanic which is THE core narrative mechanic of SUNSET16
every morning the player chooses: take the pill (compliance) or refuse it (defiance)
this single choice ripples through the ENTIRE game - visuals, audio, difficulty, accessible areas, ending

how the pill system works:
- days 1-2 are FORCED choices (scripted tutorial: day 1 = must take, day 2 = must refuse)
  this teaches the player both states before giving them real choice on days 3-5
- days 3-5 are FREE choices (player decides based on what theyve seen)
- tracks choices as a Dictionary<int, PillChoice> mapping day number to choice

ENDING DETERMINATION:
- 3+ pills taken = BAD ending (player became a mindless compliant worker)
- 3+ pills refused = GOOD ending (player discovered truth and can escape)
- the ENDING_THRESHOLD constant (3) is what controls this
  with 5 days, you need at least 3 of the same choice to trigger an ending
  so the earliest possible ending trigger is after day 3 (if you took/refused all 3)

HOW THIS EVOLVED:
- originally used simple bools and separate counters (totalPillsTaken, totalPillsAvoided)
- we switched to Dictionary<int, PillChoice> so we can track per-day choices
  which is needed for save/load and for the forced choice system
- added IsForcedChoice/GetForcedChoice for the scripted days 1-2
- added IsEndingReached property (computed, checks both thresholds)
- DetermineEnding() returns string ("Good"/"Bad"/"Undetermined") instead of enum

TODO: the ending determination is pretty basic rn - might want more nuanced endings
TODO: add visual feedback when pill choice is made (event fires to VisualStateController)
TODO: the forced choice system should be more obvious to the player (UI feedback)
*/
using UnityEngine;
using System;
using System.Collections.Generic;

namespace SUNSET16.Core
{
    public class PillStateManager : Singleton<PillStateManager>
    {
        //stores the pill choice for each day (key = day number, value = what they chose)
        //using Dictionary instead of array cos its cleaner for lookup and we can use day number directly
        private Dictionary<int, PillChoice> _pillChoices;

        private const int ENDING_THRESHOLD = 3; //how many of the same choice triggers an ending (3 out of 5 days)

        public bool IsInitialized { get; private set; }

        //computed property - checks if EITHER threshold has been met
        //this means the game should end (either good or bad path)
        //PillStateManager doesnt END the game itself, it just reports the state
        //GameManager/DayManager check this and handle the actual ending
        public bool IsEndingReached => GetPillsTakenCount() >= ENDING_THRESHOLD
                                   || GetPillsRefusedCount() >= ENDING_THRESHOLD;

        //event fires whenever a pill choice is made, with day number and what they chose
        //AudioManager, VisualStateController, etc subscribe to this to update presentation
        public event Action<int, PillChoice> OnPillTaken;
        //event fires when IsEndingReached becomes true, with the ending type string
        public event Action<string> OnEndingReached;

        //called by GameManager during initialization
        //sets up a fresh dictionary with all days set to None (no choice made)
        public void Initialize()
        {
            _pillChoices = new Dictionary<int, PillChoice>();
            for (int i = 1; i <= 5; i++)
            {
                _pillChoices[i] = PillChoice.None; //no choice made yet for any day
            }

            IsInitialized = true;
            Debug.Log("[PILLSTATEMANAGER] Initialized");
        }

        //TUTORIAL SYSTEM: days 1 and 2 have scripted pill choices
        //this forces the player to experience both states before making real choices
        //day 1 = forced to take (shows them the on-pill experience)
        //day 2 = forced to refuse (shows them the off-pill experience)
        //days 3+ = free choice
        public bool IsForcedChoice(int day)
        {
            return day <= 2;
        }

        //returns what the forced choice is for scripted days
        public PillChoice GetForcedChoice(int day)
        {
            switch (day)
            {
                case 1: return PillChoice.Taken;      //day 1: forced take (tutorial)
                case 2: return PillChoice.NotTaken;   //day 2: forced refuse (tutorial)
                default: return PillChoice.None;      //days 3+: no forced choice
            }
        }

        //THE MAIN PILL CHOICE METHOD
        //called when the player makes their daily pill choice
        //has several validation checks to prevent invalid states
        public void TakePill(PillChoice choice)
        {
            int currentDay = DayManager.Instance.CurrentDay;

            //cant choose twice in one day
            if (HasTakenPillToday())
            {
                Debug.LogWarning($"[PILLSTATEMANAGER] Pill already chosen on Day {currentDay}");
                return;
            }

            //PillChoice.None isnt a valid player choice, its the default "hasnt chosen yet" state
            if (choice == PillChoice.None)
            {
                Debug.LogWarning("[PILLSTATEMANAGER] Cannot take 'None' pill");
                return;
            }

            //once an ending is reached, no more choices (game should be ending)
            if (IsEndingReached)
            {
                Debug.LogWarning("[PILLSTATEMANAGER] Game has already reached an ending - no more choices");
                return;
            }

            //enforce scripted choices on tutorial days (1 and 2)
            if (IsForcedChoice(currentDay))
            {
                PillChoice forced = GetForcedChoice(currentDay);
                if (choice != forced)
                {
                    Debug.LogWarning($"[PILLSTATEMANAGER] Day {currentDay} is scripted - must choose {forced}");
                    return;
                }
            }

            //all validation passed - record the choice
            _pillChoices[currentDay] = choice;
            OnPillTaken?.Invoke(currentDay, choice); //notify everyone (AudioManager, VisualStateController, etc)
            Debug.Log($"[PILLSTATEMANAGER] Day {currentDay}: {(IsForcedChoice(currentDay) ? "Forced" : "Player chose")} {choice}");

            //check if this choice triggered an ending
            CheckForEnding();
        }

        //returns true if the player has already made a pill choice today
        //used by DayManager gate checks and UI to know if pill interaction is still needed
        public bool HasTakenPillToday()
        {
            int currentDay = DayManager.Instance.CurrentDay;
            return _pillChoices.ContainsKey(currentDay) && _pillChoices[currentDay] != PillChoice.None;
        }

        //get the pill choice for a specific day (used by DayManager, SaveManager, etc)
        public PillChoice GetPillChoice(int day)
        {
            if (day < 1 || day > 5)
            {
                Debug.LogWarning($"[PILLSTATEMANAGER] Invalid day {day}");
                return PillChoice.None;
            }
            return _pillChoices.ContainsKey(day) ? _pillChoices[day] : PillChoice.None;
        }

        //SAVE/LOAD ONLY - silently set a pill choice without validation or events
        //SaveManager uses this to restore saved state without triggering gameplay logic
        public void SetPillChoice(int day, PillChoice choice)
        {
            if (day < 1 || day > 5)
            {
                Debug.LogWarning($"[PILLSTATEMANAGER] Invalid day {day}");
                return;
            }

            _pillChoices[day] = choice;
        }

        //count how many days the player took the pill (for ending determination)
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

        //count how many days the player refused the pill (for ending determination)
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

        //determines which ending the player is heading toward based on their cumulative choices
        //returns "Bad" if 3+ pills taken (compliance path)
        //returns "Good" if 3+ pills refused (defiance/escape path)
        //returns "Undetermined" if neither threshold met yet
        public string DetermineEnding()
        {
            int takenCount = GetPillsTakenCount();
            int refusedCount = GetPillsRefusedCount();

            if (takenCount >= ENDING_THRESHOLD)
            {
                return "Bad"; //player took too many pills, became a compliant drone
            }

            if (refusedCount >= ENDING_THRESHOLD)
            {
                return "Good"; //player refused enough pills, discovered the truth, can escape
            }

            return "Undetermined"; //still playing, neither path locked in
        }

        //checks if an ending has been reached and fires the event if so
        //called after every pill choice
        public bool CheckForEnding()
        {
            if (!IsEndingReached) return false;

            string ending = DetermineEnding();
            Debug.Log($"[PILLSTATEMANAGER] === ENDING REACHED: {ending} ===");
            OnEndingReached?.Invoke(ending); //GameManager listens for this
            return true;
        }

        protected override void Awake()
        {
            base.Awake(); //Singleton handles instance management
        }
    }
}
