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
                {
                    return;
                }
            }

            //on-pill nights you can ONLY use the bedroom door, everything else is blocked
            if (!isBedroomDoor && IsBedroomRestrictionActive())
            {
                ShowLockedMessage("You feel too drowsy to explore... You should head back to your room.");
                return;
            }

            TransitionToRoom(); //all checks passed, lets go
        }

        public string GetInteractionPrompt()
        {
            //prompt changes based on whether you can actually use the door
            if (isLocked)
            {
                return "Locked";
            }

            if (isHiddenRoomDoor && HiddenRoomManager.Instance != null)
            {
                if (!HiddenRoomManager.Instance.IsRoomDiscovered(hiddenRoomID))
                {
                    return "Locked"; //havent found this room yet
                }

            if (PillStateManager.Instance != null && PillStateManager.Instance.HasTakenPillToday())
                {
                    return "Too drowsy..."; //took the pill so no exploring tonight
                }
            }

            if (!isBedroomDoor && IsBedroomRestrictionActive())
            {
                return "Too tired to explore...";
            }

            return "Press E to open";
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
                ShowLockedMessage("You feel too drowsy to explore...");
                return false;
            }

            //CanAccessRoom does the full check: night phase + off-pill + task done
            if (!HiddenRoomManager.Instance.CanAccessRoom(hiddenRoomID))
            {
                ShowLockedMessage("You cannot access this area right now.");
                return false;
            }

            return true; //all good, let em in
        }

        //checks if the player took the pill today and its night
        //if so only the bedroom door should work (on-pill restriction)
        private bool IsBedroomRestrictionActive()
        {
            if (DayManager.Instance == null || PillStateManager.Instance == null)
            {
                return false; //cant restrict if managers dont exist
            }

            if (DayManager.Instance.CurrentPhase != DayPhase.Night)
            {
                return false; //only applies at night
            }

            int currentDay = DayManager.Instance.CurrentDay;
            PillChoice todayChoice = PillStateManager.Instance.GetPillChoice(currentDay);
            if (todayChoice != PillChoice.Taken)
            {
                return false; //didnt take the pill so theyre free to roam
            }

            return true; //took the pill + night = bedroom only
        }

        void TransitionToRoom()
        {
            //TODO: uncomment when we have the audio asset
            if (AudioManager.Instance != null)
            {
                // AudioManager.Instance.PlayDoorOpen();
            }

            //tell RoomManager where to put the player in the next scene
            if (RoomManager.Instance != null)
            {
                RoomManager.Instance.SetNextSpawnPosition(spawnPositionInTargetScene);
            }

            //actually load the room
            if (RoomManager.Instance != null)
            {
                RoomManager.Instance.LoadRoom(targetSceneName);
            }

            //if its a hidden room door, mark it as entered
            if (isHiddenRoomDoor && HiddenRoomManager.Instance != null)
            {
                HiddenRoomManager.Instance.EnterRoom(hiddenRoomID);
                SetDoorState(DoorState.Normal);
            }

            Debug.Log($"[DOORCONTROLLER] Transitioning to {targetSceneName}");
        }

        //changes the visual appearance of the door based on its state
        //red = locked, yellow = found it but havent gone in, green = good to go
        public void SetDoorState(DoorState state)
        {
            currentState = state;

            switch (state)
            {
                case DoorState.Locked:
                    if (doorLight != null)
                        doorLight.color = Color.red;
                    if (doorSprite != null)
                        doorSprite.color = new Color(1f, 0.5f, 0.5f); //pinkish tint
                    isLocked = true;
                    break;

                case DoorState.Discovered:
                    if (doorLight != null)
                        doorLight.color = Color.yellow;
                    if (doorSprite != null)
                        doorSprite.color = new Color(1f, 1f, 0.7f); //warm yellow tint
                    isLocked = false;
                    break;

                case DoorState.Normal:
                    if (doorLight != null)
                        doorLight.color = Color.green;
                    if (doorSprite != null)
                        doorSprite.color = Color.white; //no tint
                    isLocked = false;
                    break;
            }

            //update the interaction prompt to match the new state
            if (interactionSystem != null)
            {
                interactionSystem.RefreshPrompt();
            }

            Debug.Log($"[DOORCONTROLLER] {gameObject.name} set to {state}");
        }

        public DoorState GetDoorState()
        {
            return currentState;
        }

        void ShowLockedMessage(string message)
        {
            Debug.Log($"[DOORCONTROLLER] {message}");
            //TODO: need to build HUDManager first, then we can show this on screen
            // if (HUDManager.Instance != null)
            // {
            //     HUDManager.Instance.ShowMessage(message, 2f);
            // }
        }

        public void SetLocked(bool locked)
        {
            isLocked = locked;

            if (locked && currentState != DoorState.Locked)
            {
                SetDoorState(DoorState.Locked);
            }
        }

        public bool IsLocked()
        {
            return isLocked;
        }

        public string GetTargetSceneName()
        {
            return targetSceneName;
        }

        public RoomType GetRoomType()
        {
            return roomType;
        }

        public bool IsBedroomDoor()
        {
            return isBedroomDoor;
        }

        public bool IsHiddenRoomDoor()
        {
            return isHiddenRoomDoor;
        }
    }
}
