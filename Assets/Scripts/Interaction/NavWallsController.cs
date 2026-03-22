/*
controls the three visual wall states for NavRoomScene based on task completion

the nav room background is three looping MP4 videos in StreamingAssets/NavigationRoom/:
    NavWalls_Default.mp4  → plays until Task 1 is complete
    NavWalls_T1Solved.mp4 → plays after Task 1 until Task 2 is complete
    NavWalls_T2Solved.mp4 → plays permanently after both tasks are done

subscribes to TaskManager.OnTask1Completed and TaskManager.OnTaskCompleted
which fire automatically through the existing TaskInteraction → TaskManager flow
no extra wiring on the task side needed — just assign the VideoPlayer reference

follows the same GameManager init pattern as all other scene controllers
*/
using UnityEngine;
using UnityEngine.Video;
using SUNSET16.Core;

namespace SUNSET16.Interaction
{
    public class NavWallsController : MonoBehaviour
    {
        [Header("Video Player")]
        [SerializeField] private VideoPlayer videoPlayer;

        [Header("Video File Names (inside StreamingAssets/NavigationRoom/)")]
        [SerializeField] private string defaultVideoName   = "NavWalls_Default.mp4";
        [SerializeField] private string t1SolvedVideoName  = "NavWalls_T1Solved.mp4";
        [SerializeField] private string t2SolvedVideoName  = "NavWalls_T2Solved.mp4";

        private void Start()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsInitialized)
                Initialize();
            else if (GameManager.Instance != null)
                GameManager.Instance.OnInitializationComplete += Initialize;
        }

        private void Initialize()
        {
            if (TaskManager.Instance != null)
            {
                TaskManager.Instance.OnTask1Completed += OnTask1Done;
                TaskManager.Instance.OnTaskCompleted  += OnBothTasksDone;
            }
            else
            {
                Debug.LogWarning("[NAVWALLSCONTROLLER] TaskManager not found — wall states will not update");
            }

            PlayVideo(defaultVideoName);
            Debug.Log("[NAVWALLSCONTROLLER] Initialized — playing Default State");
        }

        // ─── Task event handlers ──────────────────────────────────────────────────

        private void OnTask1Done()
        {
            PlayVideo(t1SolvedVideoName);
            Debug.Log("[NAVWALLSCONTROLLER] Task 1 complete — switching to T1 Solved state");
        }

        private void OnBothTasksDone(int day)
        {
            PlayVideo(t2SolvedVideoName);
            Debug.Log($"[NAVWALLSCONTROLLER] Day {day} tasks complete — switching to T2 Solved state (permanent)");
        }

        // ─── Video switching ──────────────────────────────────────────────────────

        private void PlayVideo(string fileName)
        {
            if (videoPlayer == null)
            {
                Debug.LogWarning("[NAVWALLSCONTROLLER] VideoPlayer not assigned");
                return;
            }

            videoPlayer.Stop();
            videoPlayer.url = Application.streamingAssetsPath + "/NavigationRoom/" + fileName;
            videoPlayer.Play();
        }

        // ─── Cleanup ──────────────────────────────────────────────────────────────

        private void OnDestroy()
        {
            if (TaskManager.Instance != null)
            {
                TaskManager.Instance.OnTask1Completed -= OnTask1Done;
                TaskManager.Instance.OnTaskCompleted  -= OnBothTasksDone;
            }

            if (GameManager.Instance != null)
                GameManager.Instance.OnInitializationComplete -= Initialize;
        }
    }
}
