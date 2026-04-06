/*
controls the visual state of Server2RoomScene based on task progress
this is a task room (not a hidden puzzle room) so it hooks into TaskManager,
not PuzzleManager like the original ServerRoomController does

two tasks on the same console, one console screen, two hazard props:

state 1 - task 1 not done:
  - screen shows notDoneSprite
  - HazardLeft GO active (full opacity caution sign, left of console)
  - HazardRight GO inactive

state 2 - task 1 done, task 2 not done:
  - screen flickers between notDoneSprite and successSprite then settles on notDoneSprite
    (signals partial resolution - something is still wrong)
  - HazardLeft deactivated
  - HazardRight activated (half opacity caution sign, right of console)

state 3 - both tasks done:
  - screen switches to successSprite
  - HazardRight deactivated (no more hazards visible)

HazardSprite.png is a 3-frame sprite sheet - slice it in Sprite Editor (Multiple mode):
  frame 0 = full opacity  -> assign to HazardLeft SpriteRenderer
  frame 1 = half opacity  -> assign to HazardRight SpriteRenderer
  frame 2 = transparent block -> unused (GO is just hidden)
*/

using System.Collections;
using UnityEngine;
using SUNSET16.Core;

namespace SUNSET16.Core
{
    public class Server2RoomController : MonoBehaviour
    {
        [Header("Computer Screen")]
        [SerializeField] private SpriteRenderer computerScreen;
        [SerializeField] private Sprite notDoneSprite;
        [SerializeField] private Sprite successSprite;

        [Header("Hazard Props")]
        [Tooltip("Full opacity hazard sign — left of console. Active when task 1 is not done.")]
        [SerializeField] private GameObject hazardLeft;
        [Tooltip("Half opacity hazard sign — right of console. Active when task 1 done but task 2 not done.")]
        [SerializeField] private GameObject hazardRight;

        [Header("Flicker Settings")]
        [Tooltip("How many times the screen flickers between notdone and success when task 1 completes.")]
        [SerializeField] private int flickerCount = 6;
        [Tooltip("Seconds between each flicker frame.")]
        [SerializeField] private float flickerInterval = 0.1f;

        private void Start()
        {
            // set initial state
            if (computerScreen != null && notDoneSprite != null)
                computerScreen.sprite = notDoneSprite;

            if (hazardLeft  != null) hazardLeft.SetActive(true);
            if (hazardRight != null) hazardRight.SetActive(false);

            if (TaskManager.Instance != null)
            {
                TaskManager.Instance.OnTask1Completed += OnTask1Completed;
                TaskManager.Instance.OnTaskCompleted  += OnBothTasksCompleted;
            }
            else
            {
                Debug.LogWarning("[SERVER2ROOMCONTROLLER] TaskManager not found - visual states won't update");
            }
        }

        // task 1 done: swap hazards, flicker screen, settle on notdone
        private void OnTask1Completed()
        {
            if (hazardLeft  != null) hazardLeft.SetActive(false);
            if (hazardRight != null) hazardRight.SetActive(true);

            StartCoroutine(FlickerScreen());

            Debug.Log("[SERVER2ROOMCONTROLLER] Task 1 complete - hazard shifted, screen flickering");
        }

        // both tasks done: hide remaining hazard, lock screen to success
        private void OnBothTasksCompleted(int day)
        {
            StopAllCoroutines(); // in case flicker is still running somehow

            if (hazardRight != null) hazardRight.SetActive(false);

            if (computerScreen != null && successSprite != null)
                computerScreen.sprite = successSprite;

            Debug.Log("[SERVER2ROOMCONTROLLER] Both tasks complete - success state");
        }

        // flickers screen between notdone and success, resolves back to notdone
        private IEnumerator FlickerScreen()
        {
            for (int i = 0; i < flickerCount; i++)
            {
                if (computerScreen != null && successSprite != null)
                    computerScreen.sprite = successSprite;

                yield return new WaitForSeconds(flickerInterval);

                if (computerScreen != null && notDoneSprite != null)
                    computerScreen.sprite = notDoneSprite;

                yield return new WaitForSeconds(flickerInterval);
            }

            // settle on notdone - task 2 still needs doing
            if (computerScreen != null && notDoneSprite != null)
                computerScreen.sprite = notDoneSprite;

            Debug.Log("[SERVER2ROOMCONTROLLER] Flicker resolved to notdone - task 2 pending");
        }

        private void OnDestroy()
        {
            if (TaskManager.Instance != null)
            {
                TaskManager.Instance.OnTask1Completed -= OnTask1Completed;
                TaskManager.Instance.OnTaskCompleted  -= OnBothTasksCompleted;
            }
        }
    }
}
