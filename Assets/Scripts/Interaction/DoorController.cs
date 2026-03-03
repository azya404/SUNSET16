/*
the door script - handles room transitions and access control
implements IInteractable so InteractionSystem can trigger it with E

three flavors of door:
- normal: always works, no restrictions (hallways, bedroom, etc)
- hidden room: only works off-pill at night after task is done
  has visual states too (red = locked, yellow = discovered, green = entered)
- bedroom: special flag so on-pill nights the player can ONLY go here
  all other doors show "too drowsy to explore" which is kinda funny

when you go through a door it tells RoomManager where to spawn the player
in the next room and then triggers the additive scene load
if its a hidden room door it also tells HiddenRoomManager you entered

the light + sprite color feedback is a nice touch
red/pink for locked, yellow/warm for discovered, green/white for open
players can tell at a glance whats accessible

TODO: actual door open/close animation
TODO: PlayDoorOpen sfx is commented out - need the audio asset
TODO: HUD locked message display (needs HUDManager which doesnt exist yet)
*/
using UnityEngine;
using UnityEngine.Rendering.Universal;
using SUNSET16.UI;

namespace SUNSET16.Core
{
    public class DoorController : MonoBehaviour, IInteractable
    {
        [Header("Room Configuration")]
        [SerializeField] private string targetSceneName;
        [SerializeField] private Vector3 spawnPositionInTargetScene;
        [SerializeField] private RoomType roomType = RoomType.Normal;

        [Header("Hidden Room Settings")]
        [SerializeField] private bool isHiddenRoomDoor = false;
        [SerializeField] private string hiddenRoomID;

        [Header("Bedroom Door Settings")]
        [SerializeField] private bool isBedroomDoor = false;

        [Header("Visual")]
        [SerializeField] private Light2D doorLight;
        [SerializeField] private SpriteRenderer doorSprite;

        [Header("Door State")]
        private DoorState currentState = DoorState.Normal;
        private bool isLocked = false;

        private InteractionSystem interactionSystem;

        void Start()
        {
            interactionSystem = GetComponent<InteractionSystem>();
            //if its a hidden room door, ask HiddenRoomManager what state its in
            //otherwise just set it to normal (unlocked, green)
            if (isHiddenRoomDoor && HiddenRoomManager.Instance != null)
            {
                DoorState state = HiddenRoomManager.Instance.GetDoorState(hiddenRoomID);
                SetDoorState(state);
            }
            else
            {
                SetDoorState(DoorState.Normal);
            }
        }

        public void Interact()
        {
            Debug.Log($"[DOORCONTROLLER] Attempting to interact with door to {targetSceneName}");

            // andy can move, but not change rooms when DOLOS is announcing things
            if (DOLOSManager.Instance != null && DOLOSManager.Instance.IsAnnouncementActive)
            {
                Debug.Log("[DOORCONTROLLER] DOLOS announcement active — door transition blocked");
                return;
            }
            //chain of checks - if any fail we bail early with a message
            if (isLocked)
            {
                ShowLockedMessage("The door is locked.");
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

            TransitionToRoom(); //all checks passed, LSFFGGGG
        }

        public string GetInteractionPrompt()
        {
            //prompt changes based on whether you can actually use the door
            if (isLocked)
                return "Locked";

            if (isHiddenRoomDoor && HiddenRoomManager.Instance != null)
            {
                if (!HiddenRoomManager.Instance.IsRoomDiscovered(hiddenRoomID))
                    return "Locked"; //havent found this room yet

                if (PillStateManager.Instance != null && PillStateManager.Instance.HasTakenPillToday())
                    return "Too drowsy..."; //took the pill so no exploring tonight
            }

            if (!isBedroomDoor && IsNightRestrictionActive())
                return "Too tired...";

            return "Press E to open";
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

            if (PillStateManager.Instance != null && PillStateManager.Instance.HasTakenPillToday())
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

        void TransitionToRoom()
        {
            if (AudioManager.Instance != null)
            {
                // AudioManager.Instance.PlayDoorOpen();
            }

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

        public void SetDoorState(DoorState state)
        {
            currentState = state;

            switch (state)
            {
                case DoorState.Locked:
                    if (doorLight  != null) doorLight.color  = Color.red;
                    if (doorSprite != null) doorSprite.color = new Color(1f, 0.5f, 0.5f);
                    isLocked = true;
                    break;

                case DoorState.Discovered:
                    if (doorLight  != null) doorLight.color  = Color.yellow;
                    if (doorSprite != null) doorSprite.color = new Color(1f, 1f, 0.7f);
                    isLocked = false;
                    break;

                case DoorState.Normal:
                    if (doorLight  != null) doorLight.color  = Color.green;
                    if (doorSprite != null) doorSprite.color = Color.white;
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