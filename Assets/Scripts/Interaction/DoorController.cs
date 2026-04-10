/*
the door script - handles room transitions and access control
implements IInteractable so InteractionSystem can trigger it with E
implements IProximityResponder so InteractionSystem can notify it when player enters/exits zone

three flavors of door:
- normal: always works, no restrictions (hallways, bedroom, etc)
- hidden room: only works off-pill at night after task is done
  has visual states too (red = locked, orange = accessible, green = player in zone)
- bedroom: special flag so on-pill nights the player can ONLY go here
  all other doors show "too drowsy to explore" which is kinda funny

when you go through a door it tells RoomManager where to spawn the player
in the next room and then triggers the additive scene load
if its a hidden room door it also tells HiddenRoomManager you entered

light colour feedback (option 2):
- red    = hard locked regardless of proximity
- orange = accessible, player not in zone (always visible from a distance)
- green  = player is in zone and door is accessible (ready to press E)
animation plays on successful interact, THEN transitions to room

TODO: PlayDoorOpen sfx is commented out - need the audio asset
TODO: HUD locked message display (needs HUDManager which doesnt exist yet)
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using SUNSET16.UI;

namespace SUNSET16.Core
{
    public class DoorController : MonoBehaviour, IInteractable, IProximityResponder
    {
        [Header("Room Configuration")]
        [SerializeField] private string targetSceneName;
        [SerializeField] private Vector3 spawnPositionInTargetScene;
        [SerializeField] private RoomType roomType = RoomType.Normal;

        [Header("Hidden Room Settings")]
        [SerializeField] private bool isHiddenRoomDoor = false;
        [SerializeField] private string hiddenRoomID;

        [Header("Bedroom Door Settings")]
        [Tooltip("Bypasses the on-pill night restriction. Set on ANY door that should remain passable at night — not just literal bedroom doors. E.g. BoilerRoom→Hallway and Hallway→Bedroom both need this set so the player can always find their way back.")]
        [SerializeField] private bool isBedroomDoor = false;

        [Header("Task Room")]
        [Tooltip("Block exit until all tasks are complete (use on BoilerRoom hallway door).")]
        [SerializeField] private bool requiresAllTasksComplete = false;
        
        [Header("Puzzle Room")]
        [SerializeField] private bool requiresPuzzleComplete = false;

        [Header("Bedroom Door Gating")]
        [Tooltip("Enable morning/night dialogue gates on the bedroom exit door.")]
        [SerializeField] private bool isMorningGated = false;

        [Header("Exit Door Settings")]
        [Tooltip("Mark this as the ship's exit door. Good ending: unlocked, loads GoodEndingScene. Bad/undetermined ending: permanently locked.")]
        [SerializeField] private bool isExitDoor = false;
        [Tooltip("Bark lines shown when player approaches in good ending state. E.g. 'I should at least try to leave the ship...right?'")]
        [SerializeField] private List<string> goodEndingExitBark = new List<string>();

        [Header("Visual")]
        [SerializeField] private Light2D doorLight;
        [SerializeField] private SpriteRenderer doorSprite;
        [SerializeField] private GameObject lightSpriteRed;    // LightR.png GO — shown when locked
        [SerializeField] private GameObject lightSpriteYellow; // YELLOW.png GO — shown when accessible
        [SerializeField] private GameObject lightSpriteGreen;  // LightG.png GO — shown when player in zone

        [Header("Interaction Prompts")]
        [SerializeField] private List<string> mirrorIncomplete = new List<string>();
        [SerializeField] private List<string> computerIncomplete = new List<string>();
        [SerializeField] private List<string> shouldSleep = new List<string>();
        [SerializeField] private List<string> hallwayLockedDay = new List<string>();
        [SerializeField] private List<string> hallwayLockedNightPill = new List<string>();
        [SerializeField] private List<string> hallwayLockedNightNoPill = new List<string>();
        [SerializeField] private List<string> taskIncomplete = new List<string>();

        [Header("Animation")]
        [SerializeField] private Sprite[] animationFrames; // drag DoorAnim_0 to DoorAnim_7 in order
        [SerializeField] private float frameDelay = 0.07f;
        private bool isAnimating = false;

        [Header("Door State")]
        private DoorState currentState = DoorState.Normal;
        private bool isLocked = false;

        private static readonly Color ColourLocked     = Color.red;
        private static readonly Color ColourAccessible = new Color(1f, 0.5f, 0f); // orange
        private static readonly Color ColourInZone     = Color.green;
        private const string GOOD_ENDING_SCENE = "GoodEndingScene";
        private List<string> taskRooms = new List<string> {"boiler room 1A", "the navigation room", "the infirmary", "server room 1A", "boiler room 1B"};
        private int guideToRoom;
        

        private InteractionSystem interactionSystem;

        void Start() 
        {
            interactionSystem = GetComponent<InteractionSystem>();

            // Exit door: state set entirely by ending — skip all normal targetSceneName logic
            if (isExitDoor)
            {
                string ending = PillStateManager.Instance != null
                    ? PillStateManager.Instance.DetermineEnding()
                    : "Undetermined";
                // Good ending = escape is possible; anything else = door stays sealed
                SetDoorState(ending == "Good" ? DoorState.Normal : DoorState.Locked);
                return;
            }

            //if its a hidden room door, ask HiddenRoomManager what state its in
            //otherwise just set it to normal (unlocked, orange)
        if (targetSceneName  == "HallwayScene")
        {
            SetDoorState(DoorState.Normal);
        }
        else if (targetSceneName == "BedroomScene")
        {
            if (DayManager.Instance.CurrentPhase == DayPhase.Night && PillStateManager.Instance.GetPillsRefusedCount() == 1 && PuzzleManager.Instance.DonePuzzleCount() == 0)
                {
                    SetDoorState(DoorState.Locked);
                }
            else if (DayManager.Instance.CurrentPhase == DayPhase.Night && PillStateManager.Instance.GetPillsRefusedCount() == 2 && PuzzleManager.Instance.DonePuzzleCount() == 1)
                {
                    SetDoorState(DoorState.Locked);
                }
            else if (DayManager.Instance.CurrentPhase == DayPhase.Night && PillStateManager.Instance.GetPillsRefusedCount() == 3 && PuzzleManager.Instance.DonePuzzleCount() == 2)
                {
                    SetDoorState(DoorState.Locked);
                }
            else if (DayManager.Instance.CurrentPhase == DayPhase.Night && PillStateManager.Instance.GetPillsRefusedCount() == 4 && PuzzleManager.Instance.DonePuzzleCount() == 3)
                {
                    SetDoorState(DoorState.Locked);
                }
            else if (DayManager.Instance.CurrentPhase == DayPhase.Morning)
                {
                    SetDoorState(DoorState.Locked);
                }
            else
                {
                    SetDoorState(DoorState.Normal);     
                }
            // SetDoorState(DoorState.Locked);
            // if (targetSceneName == "BedroomScene" &&
            // DayManager.Instance.CurrentPhase == DayPhase.Night &&
            // requiresAllTasksComplete == true)
            // {
            //     SetDoorState(DoorState.Normal);
            // }
        }
        else if (targetSceneName == "BoilerRoomScene" &&
                DayManager.Instance.CurrentPhase == DayPhase.Morning &&
                DayManager.Instance.CurrentDay == 1)
        {
            SetDoorState(DoorState.Normal);
        }
        else if (targetSceneName == "NavRoomScene" &&
                DayManager.Instance.CurrentPhase == DayPhase.Morning &&
                DayManager.Instance.CurrentDay == 2)
        {
            SetDoorState(DoorState.Normal);
        }
        else if (targetSceneName == "LabScene")
            {
                if (DayManager.Instance.CurrentPhase == DayPhase.Night && DayManager.Instance.CurrentDay == 2 && PuzzleManager.Instance.DonePuzzleCount() == 0)
                {
                SetDoorState(DoorState.Normal);
                }
                else
                {
                SetDoorState(DoorState.Locked);
                }
            }
        else if (targetSceneName == "InfirmaryScene" &&
                DayManager.Instance.CurrentPhase == DayPhase.Morning &&
                DayManager.Instance.CurrentDay == 3)
        {
            SetDoorState(DoorState.Normal);
        }
        else if (targetSceneName == "ServerRoomScene") //Sect
                if (DayManager.Instance.CurrentPhase == DayPhase.Night && DayManager.Instance.CurrentDay >= 3 && PillStateManager.Instance.GetPillsRefusedCount() == 2 && PuzzleManager.Instance.DonePuzzleCount() == 1)
                {
                    SetDoorState(DoorState.Normal); //Before Done Puzzle
                }
                else
                {
                    SetDoorState(DoorState.Locked); //After Done Puzzle
                    //SetDoorState(DoorState.Normal);
                }
        else if (targetSceneName == "Server2RoomScene" &&
                DayManager.Instance.CurrentPhase == DayPhase.Morning &&
                DayManager.Instance.CurrentDay == 4)
        {
            SetDoorState(DoorState.Normal);
        }
        else if (targetSceneName == "CrematoriumScene")
            {
                if (DayManager.Instance.CurrentPhase == DayPhase.Night && DayManager.Instance.CurrentDay >= 4 && PillStateManager.Instance.GetPillsRefusedCount() == 3 && PuzzleManager.Instance.DonePuzzleCount() == 2) 
                {
                    SetDoorState(DoorState.Normal); //Before Done Puzzle
                }
                else
                {
                    SetDoorState(DoorState.Locked); //After Done Puzzle
                    //SetDoorState(DoorState.Normal);
                }
            } 
        else if (targetSceneName == "LittleBoilerRoomScene" &&
                DayManager.Instance.CurrentPhase == DayPhase.Morning &&
                DayManager.Instance.CurrentDay == 5)
        {
            SetDoorState(DoorState.Normal);
        }
        else
        {
            SetDoorState(DoorState.Locked);
        }
            
            
            // if (isHiddenRoomDoor && HiddenRoomManager.Instance != null)
            // {
            //     DoorState state = HiddenRoomManager.Instance.GetDoorState(hiddenRoomID);
            //     SetDoorState(state);
            // }
            // else
            // {
            //     SetDoorState(DoorState.Normal);
            // }
        }

        // ─── IProximityResponder ──────────────────────────────────────────────────

        public void OnPlayerEnterZone()
        {
            if (!isLocked && doorLight != null)
                doorLight.color = ColourInZone;
            if (!isLocked)
                ActivateLightSprite(lightSpriteGreen);
        }

        public void OnPlayerExitZone()
        {
            if (!isLocked && doorLight != null)
                doorLight.color = ColourAccessible;
            if (!isLocked)
                ActivateLightSprite(lightSpriteYellow);
        }

        // ─── IInteractable ────────────────────────────────────────────────────────

        public void Interact()
        {
            Debug.Log($"[DOORCONTROLLER] Attempting to interact with door to {targetSceneName}");

            if (isAnimating) return; // dont interrupt an animation already playing

            // andy can move, but not change rooms when DOLOS is announcing things
            if (DOLOSManager.Instance != null && DOLOSManager.Instance.IsAnnouncementActive)
            {
                Debug.Log("[DOORCONTROLLER] DOLOS announcement active — door transition blocked");
                return;
            }
            // Exit door in good ending state — only passable after night computer session is done
            if (isExitDoor && PillStateManager.Instance != null &&
                PillStateManager.Instance.DetermineEnding() == "Good")
            {
                // at night, must complete Albert's final dialogue before escaping
                if (DayManager.Instance != null
                    && DayManager.Instance.CurrentPhase == DayPhase.Night
                    && DialogueUIManager.Instance != null
                    && !DialogueUIManager.Instance.HasCompletedTodayNightSequence)
                {
                    ShowLockedMessage("I should finish up before leaving.");
                    return;
                }
                StartCoroutine(PlayOpenAnimation());
                return;
            }

            //chain of checks - if any fail we bail early with a message
            if (isLocked)
            {
                ShowLockedMessage("The door is sealed.");
                return;
            }

            //hidden room doors have extra validation (night phase, off-pill, room discovered, etc)
            if (isHiddenRoomDoor)
            {
                if (!ValidateHiddenRoomAccess())
                    return;
            }

            //on-pill nights you can ONLY use the bedroom door, everything else is blocked
            if (!isBedroomDoor && IsNightRestrictionActive())
            {
                ShowSleepyLockedMessage();
                return;
            }

            // task room gate: block exit until all tasks are complete
            if (requiresAllTasksComplete && TaskManager.Instance != null && !TaskManager.Instance.AreAllTasksCompleted)
            {
                ShowLockedMessage("Complete your tasks first.");
                return;
            }

            if (requiresPuzzleComplete && PuzzleManager.Instance != null && !PuzzleManager.Instance.completedPuzzle)
                return;

            // bedroom exit gates: pill check, morning dialogue, night dialogue
            if (isMorningGated && DayManager.Instance != null)
            {
                // gate 1 (morning): pill choice not made
                if (DayManager.Instance.CurrentPhase == DayPhase.Morning &&
                    PillStateManager.Instance != null &&
                    !PillStateManager.Instance.HasTakenPillToday())
                {
                    ShowLockedMessage("I should start my morning routine...");
                    return;
                }
                // gate 2 (morning): morning computer session not done
                if (DayManager.Instance.CurrentPhase == DayPhase.Morning &&
                    DialogueUIManager.Instance != null &&
                    !DialogueUIManager.Instance.HasCompletedTodaySequence)
                {
                    ShowLockedMessage("I should check in at the computer first.");
                    return;
                }
                // gate 3 (night): night computer session not done
                // VERTICAL SLICE NOTE: for the Day 3 VS demo this gate acts as the "sleep" gate.
                // Once night dialogue is done and this passes, pressing E on the bedroom door is
                // effectively the end of the demo — the door transitions to HallwayScene, which
                // is a placeholder. No further day cycling is shown.
                //
                // FUTURE IMPLEMENTATION — when adding multi-day progression, this gate and the
                // door it sits on need to be rethought:
                //   - "Sleep" should fade to black → DayManager.AdvanceDay() → load BedroomScene
                //     fresh as Morning (not transition to HallwayScene mid-night)
                //   - Consider replacing the isMorningGated door transition with a dedicated
                //     SleepInteraction.cs object in BedroomScene (like MirrorInteraction) that
                //     calls DayManager.AdvanceDay() and triggers a morning cutscene / DayManager reset
                //   - HasCompletedTodayNightSequence resets on OnPhaseChanged — correct for cycling,
                //     but verify DialogueUIManager.Start() re-subscribes after scene reload if needed
                //   - Off-pill path: HiddenRoom puzzle completion triggers IsNightRestrictionActive
                //     for non-bedroom doors — confirm this still chains correctly into multi-day
                if (DayManager.Instance.CurrentPhase == DayPhase.Night)
                {
                    ShowLockedMessage("I should finish up before leaving.");
                    return;
                }
            }

            // all checks passed - play animation then transition
            StartCoroutine(PlayOpenAnimation());
        }

        public string GetInteractionPrompt()
        {
            // Exit door prompt: good ending = escape bark, otherwise = sealed
            if (isExitDoor)
            {
                if (PillStateManager.Instance != null && PillStateManager.Instance.DetermineEnding() == "Good")
                    return goodEndingExitBark.Count > 0
                        ? goodEndingExitBark[Random.Range(0, goodEndingExitBark.Count)]
                        : "I should at least try to leave the ship...right?";
                return isLocked ? "The door is sealed." : "Press E to open";
            }

            if (isMorningGated && DayManager.Instance != null)
            {
                if (DayManager.Instance.CurrentPhase == DayPhase.Morning && PillStateManager.Instance != null && !PillStateManager.Instance.HasTakenPillToday())
                    return mirrorIncomplete[Random.Range(0, mirrorIncomplete.Count)];

                if (DialogueUIManager.Instance != null && ((DayManager.Instance.CurrentPhase == DayPhase.Morning && !DialogueUIManager.Instance.HasCompletedTodaySequence) || 
                    (DayManager.Instance.CurrentPhase == DayPhase.Night && !DialogueUIManager.Instance.HasCompletedTodayNightSequence)))
                    return computerIncomplete[Random.Range(0, computerIncomplete.Count)];

                if (DayManager.Instance.CurrentPhase == DayPhase.Night && DialogueUIManager.Instance != null && DialogueUIManager.Instance.HasCompletedTodayNightSequence)
                    return shouldSleep[Random.Range(0, shouldSleep.Count)];
            }

            if ((requiresAllTasksComplete && TaskManager.Instance != null && !TaskManager.Instance.AreAllTasksCompleted) 
                || (requiresPuzzleComplete && PuzzleManager.Instance != null && !PuzzleManager.Instance.completedPuzzle))
                return taskIncomplete[Random.Range(0, taskIncomplete.Count)];
            
            //prompt changes based on whether you can actually use the door
            if (isLocked)
            {
                if (DayManager.Instance != null && DayManager.Instance.CurrentPhase == DayPhase.Morning)
                {
                    guideToRoom = Random.Range(0, 2);
                    if (guideToRoom == 1)
                        return "\"I need to go do my tasks in " + taskRooms[DayManager.Instance.CurrentDay - 1] + "\"";
                    return hallwayLockedDay[Random.Range(0, hallwayLockedDay.Count)];
                }
                else if ((PillStateManager.Instance != null && (PillStateManager.Instance.GetPillChoice(DayManager.Instance.CurrentDay) == PillChoice.Taken)) || PuzzleManager.Instance.completedPuzzle)
                    return hallwayLockedNightPill[Random.Range(0, hallwayLockedNightPill.Count)];
                else
                    return hallwayLockedNightNoPill[Random.Range(0, hallwayLockedNightNoPill.Count)];
            }

            /*if (isHiddenRoomDoor && HiddenRoomManager.Instance != null)
            {
                if (!HiddenRoomManager.Instance.IsRoomDiscovered(hiddenRoomID))
                    return hallwayLockedDay[Random.Range(0, hallwayLockedDay.Count)]; //havent found this room yet

                if (PillStateManager.Instance != null && PillStateManager.Instance.GetPillChoice(DayManager.Instance.CurrentDay) == PillChoice.Taken)
                    return "Too drowsy..."; //took the pill so no exploring tonight
            }*/

            if (!isBedroomDoor && IsNightRestrictionActive())
                return "Too tired...";

            return "Press E to open";
        }

        public bool GetLocked()
        {
            if (RoomManager.Instance != null && RoomManager.Instance.GetCurrentRoomName().Contains("Bedroom"))
                return isMorningGated;
            else if (requiresAllTasksComplete && TaskManager.Instance != null)
                return TaskManager.Instance.AreAllTasksCompleted == false;
            else if (requiresPuzzleComplete && PuzzleManager.Instance != null)
                return PuzzleManager.Instance.completedPuzzle == false;
            else
                return currentState == DoorState.Locked;
        }

        // ─── Animation ────────────────────────────────────────────────────────────

        private IEnumerator PlayOpenAnimation()
        {
            isAnimating = true;

            if (animationFrames != null && animationFrames.Length > 0 && doorSprite != null)
            {
                foreach (Sprite frame in animationFrames)
                {
                    doorSprite.sprite = frame;
                    yield return new WaitForSeconds(frameDelay);
                }
            }

            isAnimating = false;
            TransitionToRoom();
        }

        // ─── Door Restriction Logic ───────────────────────────────────────────────

        /// <summary>
        /// Returns true when a non-bedroom door should block passage.
        ///
        /// On-pill night:   always restricted — the ship controls her immediately.
        /// Off-pill night:  FREE movement until the day's hidden-room puzzle is completed.
        ///                  After puzzle completion, restriction activates — her window of
        ///                  agency closes naturally rather than being imposed by the ship.
        /// </summary>
        private bool IsNightRestrictionActive()
        {
            if (DayManager.Instance == null || PillStateManager.Instance == null)
                return false; //cant restrict if managers dont exist

            if (DayManager.Instance.CurrentPhase != DayPhase.Night)
                return false; //only applies at night

            int currentDay         = DayManager.Instance.CurrentDay;
            PillChoice todayChoice = PillStateManager.Instance.GetPillChoice(currentDay);

            if (todayChoice == PillChoice.Taken)
                return true; //didnt take the pill so theyre free to roam

            if (todayChoice == PillChoice.NotTaken)
            {
                // Off-pill: restricted only AFTER the hidden-room puzzle for today is solved
                string expectedPuzzleId = $"puzzle_day_{currentDay}";
                return PuzzleManager.Instance != null
                    && PuzzleManager.Instance.IsPuzzleCompleted(expectedPuzzleId);
            }

            return false;
        }

        //runs through all the checks for hidden room access
        //returns false if ANY check fails (with a message for the player)
        private bool ValidateHiddenRoomAccess()
        {
            if (HiddenRoomManager.Instance == null)
            {
                Debug.LogError("[DOORCONTROLLER] HiddenRoomManager not found!");
                return false;
            }

            if (!HiddenRoomManager.Instance.IsRoomDiscovered(hiddenRoomID))
            {
                ShowLockedMessage("You don't know where this leads...");
                return false;
            }

            if (PillStateManager.Instance != null &&
                PillStateManager.Instance.GetPillChoice(DayManager.Instance.CurrentDay) == PillChoice.Taken)
            {
                ShowSleepyLockedMessage();
                return false;
            }
            
            //CanAccessRoom does the full check: night phase + off-pill + task done
            if (!HiddenRoomManager.Instance.CanAccessRoom(hiddenRoomID))
            {
                ShowLockedMessage("You cannot access this area right now.");
                return false;
            }

            return true;
        }

        // ─── Feedback Helpers ─────────────────────────────────────────────────────

        private void ShowLockedMessage(string message)
        {
            Debug.Log($"[DOORCONTROLLER] {message}");
            if (HUDController.Instance != null)
                HUDController.Instance.ShowMessage(message, 2f);
        }

        private void ShowSleepyLockedMessage()
        {
            Debug.Log("[DOORCONTROLLER] Night restriction — showing sleepy message");
            if (HUDController.Instance != null)
                HUDController.Instance.ShowSleepyMessage();
        }

        // ─── Room Transition ──────────────────────────────────────────────────────

        [ContextMenu("Go into room")]
        void TransitionToRoom()
        {
            // Good ending escape: bypass RoomManager, load standalone GoodEndingScene directly
            if (isExitDoor && PillStateManager.Instance != null &&
                PillStateManager.Instance.DetermineEnding() == "Good")
            {
                Debug.Log("[DOORCONTROLLER] Good ending exit — loading GoodEndingScene");
                SceneManager.LoadScene(GOOD_ENDING_SCENE);
                return;
            }

            if (AudioManager.Instance != null)
            {
                // AudioManager.Instance.PlayDoorOpen();
            }

            if (RoomManager.Instance.GetCurrentRoomName().Contains("Bedroom"))
                DialogueUIManager.Instance.ResetDialogue();

            if (RoomManager.Instance != null)
                RoomManager.Instance.SetNextSpawnPosition(spawnPositionInTargetScene);

            if (RoomManager.Instance != null)
                RoomManager.Instance.LoadRoom(targetSceneName);

            if (isHiddenRoomDoor && HiddenRoomManager.Instance != null)
            {
                HiddenRoomManager.Instance.EnterRoom(hiddenRoomID);
                SetDoorState(DoorState.Normal);
            }

            Debug.Log($"[DOORCONTROLLER] Transitioning to {targetSceneName}");
        }

        // ─── State Management ─────────────────────────────────────────────────────

        private void ActivateLightSprite(GameObject active)
        {
            if (lightSpriteRed    != null) lightSpriteRed.SetActive(lightSpriteRed       == active);
            if (lightSpriteYellow != null) lightSpriteYellow.SetActive(lightSpriteYellow == active);
            if (lightSpriteGreen  != null) lightSpriteGreen.SetActive(lightSpriteGreen   == active);
        }

        public void SetDoorState(DoorState state)
        {
            currentState = state;

            switch (state)
            {
                case DoorState.Locked:
                    if (doorLight  != null) doorLight.color  = ColourLocked;
                    if (doorSprite != null) doorSprite.color = new Color(1f, 0.5f, 0.5f);
                    ActivateLightSprite(lightSpriteRed);
                    isLocked = true;
                    break;

                case DoorState.Discovered:
                    // discovered = accessible but not yet entered, show orange like Normal
                    if (doorLight  != null) doorLight.color  = ColourAccessible;
                    if (doorSprite != null) doorSprite.color = Color.white;
                    ActivateLightSprite(lightSpriteYellow);
                    isLocked = false;
                    break;

                case DoorState.Normal:
                    if (doorLight  != null) doorLight.color  = ColourAccessible;
                    if (doorSprite != null) doorSprite.color = Color.white;
                    ActivateLightSprite(lightSpriteYellow);
                    isLocked = false;
                    break;

                // DoorState.Entered: no separate visual — door reverts to Normal on re-use
            }

            if (interactionSystem != null)
                interactionSystem.RefreshPrompt();

            Debug.Log($"[DOORCONTROLLER] {gameObject.name} set to {state}");
        }

        public DoorState GetDoorState()       => currentState;
        public bool      IsLocked()           => isLocked;
        public string    GetTargetSceneName() => targetSceneName;
        public RoomType  GetRoomType()        => roomType;
        public bool      IsBedroomDoor()      => isBedroomDoor;
        public bool      IsHiddenRoomDoor()   => isHiddenRoomDoor;

        public void SetLocked(bool locked)
        {
            isLocked = locked;
            if (locked && currentState != DoorState.Locked)
                SetDoorState(DoorState.Locked);
        }
    }
}
