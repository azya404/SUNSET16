/*
Full-screen video cutscene controller.
- Place on a GO in a cutscene scene alongside a VideoPlayer component
- VideoPlayer render mode: Camera Far Plane (no RenderTexture or Canvas needed)
- Video file must be in Assets/StreamingAssets/ (NOT imported via Project window)
- When the video ends or is skipped, calls RoomManager.LoadRoom(nextSceneName)
  so CoreScene stays loaded and the cutscene scene is unloaded additively like any room
*/
using UnityEngine;
using UnityEngine.Video;
using System.Collections;
using SUNSET16.Core;

namespace SUNSET16.UI
{
    public class CutsceneController : MonoBehaviour
    {
        [Header("Scene Routing")]
        [Tooltip("Room scene to load via RoomManager when the cutscene ends or is skipped.")]
        [SerializeField] private string nextSceneName = "BedroomScene";

        [Header("Video")]
        [SerializeField] private VideoPlayer videoPlayer;
        [Tooltip("Filename only — file must be in Assets/StreamingAssets/. No path, no subfolders.")]
        [SerializeField] private string videoFileName = "cutscene.mp4";

        private bool _transitioning = false;
        private bool _inputEnabled  = false;

        private void Start()
        {
            // String concat NOT Path.Combine — WebGL needs URL forward slashes, not Windows backslashes
            videoPlayer.url = Application.streamingAssetsPath + "/" + videoFileName;
            videoPlayer.loopPointReached += OnVideoFinished;
            videoPlayer.Play();
            StartCoroutine(EnableInputNextFrame());
        }

        // wait one frame so RoomManager finishes its LoadRoomCoroutine and clears _isTransitioning
        // before we accept any skip input — prevents immediate-skip softlock
        private IEnumerator EnableInputNextFrame()
        {
            yield return null;
            _inputEnabled = true;
        }

        private void Update()
        {
            if (!_inputEnabled || _transitioning) return;
            if (Input.GetKeyDown(KeyCode.Space)
                || Input.GetKeyDown(KeyCode.Return)
                || Input.GetKeyDown(KeyCode.Escape))
            {
                Skip();
            }
        }

        private void OnVideoFinished(VideoPlayer vp)
        {
            if (!_transitioning)
                StartCoroutine(LoadNext());
        }

        private void Skip()
        {
            _transitioning = true;
            videoPlayer.Stop();
            StartCoroutine(LoadNext());
        }

        private IEnumerator LoadNext()
        {
            _transitioning = true;
            yield return null; // one frame gap avoids double-trigger
            // skipFadeOut: true — no fade-to-black on cutscene exit, bedroom handles its own fade in
            RoomManager.Instance.LoadRoom(nextSceneName, skipFadeOut: true);
        }
    }
}
