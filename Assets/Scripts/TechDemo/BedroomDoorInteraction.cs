using UnityEngine;
using UnityEngine.SceneManagement;
using SUNSET16.Core;

namespace SUNSET16.TechDemo
{
    /// <summary>
    /// Simple bedroom door for tech demo.
    /// Press E to transition to Core scene.
    /// </summary>
    public class BedroomDoorInteraction : MonoBehaviour, IInteractable
    {
        [Header("Scene Transition")]
        [SerializeField] private string targetSceneName = "Core";

        [Header("Settings")]
        [SerializeField] private string interactionPrompt = "Press E to leave room";

        public void Interact()
        {
            Debug.Log($"[BEDROOM DOOR] Transitioning to {targetSceneName} scene...");

            // Lock player movement during transition (optional)
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.LockMovement(true);
            }

            // Load the target scene
            SceneManager.LoadScene(targetSceneName);
        }

        public string GetInteractionPrompt()
        {
            return interactionPrompt;
        }
    }
}
