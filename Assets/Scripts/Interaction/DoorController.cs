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

            if (isLocked)
            {
                ShowLockedMessage("The door is locked.");
                return;
            }

            if (isHiddenRoomDoor)
            {
                if (!ValidateHiddenRoomAccess())
                {
                    return;
                }
            }

            if (!isBedroomDoor && IsBedroomRestrictionActive())
            {
                ShowLockedMessage("You feel too drowsy to explore... You should head back to your room.");
                return;
            }

            TransitionToRoom();
        }

        public string GetInteractionPrompt()
        {
            if (isLocked)
            {
                return "Locked";
            }

            if (isHiddenRoomDoor && HiddenRoomManager.Instance != null)
            {
                if (!HiddenRoomManager.Instance.IsRoomDiscovered(hiddenRoomID))
                {
                    return "Locked";
                }

                if (PillStateManager.Instance != null && PillStateManager.Instance.IsPillTakenToday())
                {
                    return "Too drowsy...";
                }
            }

            if (!isBedroomDoor && IsBedroomRestrictionActive())
            {
                return "Too tired to explore...";
            }

            return "Press E to open";
        }

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

            if (PillStateManager.Instance != null && PillStateManager.Instance.IsPillTakenToday())
            {
                ShowLockedMessage("You feel too drowsy to explore...");
                return false;
            }

            if (!HiddenRoomManager.Instance.CanAccessRoom(hiddenRoomID))
            {
                ShowLockedMessage("You cannot access this area right now.");
                return false;
            }

            return true;
        }

        private bool IsBedroomRestrictionActive()
        {
            if (DayManager.Instance == null || PillStateManager.Instance == null)
            {
                return false;
            }

            if (DayManager.Instance.CurrentPhase != DayPhase.Night)
            {
                return false;
            }

            int currentDay = DayManager.Instance.CurrentDay;
            PillChoice todayChoice = PillStateManager.Instance.GetPillChoice(currentDay);
            if (todayChoice != PillChoice.Taken)
            {
                return false;
            }

            return true;
        }

        void TransitionToRoom()
        {
            if (AudioManager.Instance != null)
            {
                // AudioManager.Instance.PlayDoorOpen();
            }

            if (RoomManager.Instance != null)
            {
                RoomManager.Instance.SetNextSpawnPosition(spawnPositionInTargetScene);
            }

            if (RoomManager.Instance != null)
            {
                RoomManager.Instance.LoadRoom(targetSceneName);
            }

            if (isHiddenRoomDoor && HiddenRoomManager.Instance != null)
            {
                HiddenRoomManager.Instance.EnterRoom(hiddenRoomID);
                SetDoorState(DoorState.Normal);
            }

            Debug.Log($"[DOORCONTROLLER] Transitioning to {targetSceneName}");
        }

        public void SetDoorState(DoorState state)
        {
            currentState = state;

            switch (state)
            {
                case DoorState.Locked:
                    if (doorLight != null)
                        doorLight.color = Color.red;
                    if (doorSprite != null)
                        doorSprite.color = new Color(1f, 0.5f, 0.5f);
                    isLocked = true;
                    break;

                case DoorState.Discovered:
                    if (doorLight != null)
                        doorLight.color = Color.yellow;
                    if (doorSprite != null)
                        doorSprite.color = new Color(1f, 1f, 0.7f);
                    isLocked = false;
                    break;

                case DoorState.Normal:
                    if (doorLight != null)
                        doorLight.color = Color.green;
                    if (doorSprite != null)
                        doorSprite.color = Color.white;
                    isLocked = false;
                    break;
            }

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
