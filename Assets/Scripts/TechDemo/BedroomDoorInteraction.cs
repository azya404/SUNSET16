/*
the simplest door in the project - just loads a scene when you press E
we need this cos the tech demo bedroom is standalone and doesnt use the
full CoreScene + additive loading setup that the real DoorController uses

its basically a throwaway - once all the room scenes exist this gets
replaced by DoorController which has all the hidden room checks,
bedroom restriction, visual feedback etc

still implements IInteractable tho so InteractionSystem works exactly the same
*/
using UnityEngine;
using UnityEngine.SceneManagement;
using SUNSET16.Core;

namespace SUNSET16.TechDemo
{
    public class BedroomDoorInteraction : MonoBehaviour, IInteractable
    {
        [Header("Scene Transition")]
        [SerializeField] private string targetSceneName = "Core";

        [Header("Settings")]
        [SerializeField] private string interactionPrompt = "Press E to leave room";

        public void Interact()
        {
            Debug.Log($"[BEDROOM DOOR] Transitioning to {targetSceneName} scene...");

            //freeze the player so they dont walk around during the scene load
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.LockMovement(true);
            }

            //straight up scene load - no fancy additive stuff
            SceneManager.LoadScene(targetSceneName);
        }

        public string GetInteractionPrompt()
        {
            return interactionPrompt;
        }
    }
}
