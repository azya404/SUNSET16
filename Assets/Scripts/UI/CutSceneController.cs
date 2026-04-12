/*
Full-screen video cutscene controller.
- Place on a GO in a cutscene scene alongside a VideoPlayer component
- VideoPlayer render mode: Camera Far Plane (no RenderTexture or Canvas needed)
- Video file must be in Assets/StreamingAssets/ (NOT imported via Project window)
- When the video ends or is skipped, calls RoomManager.LoadRoom(nextSceneName)
  so CoreScene stays loaded and the cutscene scene is unloaded additively like any room

audio:
- VideoPlayer Audio Output Mode must be set to Audio Source, with an AudioSource on the
  same GO assigned as Track 0. This script grabs that source via GetTargetAudioSource(0).
- videoVolume is the design ceiling (1.0 = full encoded volume). Set this lower in the
  Inspector to tune the cutscene level — the player's master volume scales on top.
  e.g. videoVolume 0.7 + player at 80% = final volume 0.56
- subscribes to SettingsManager.OnMasterVolumeChanged so live slider adjustments apply
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

        [Header("Audio")]
        [Tooltip("Design volume ceiling for the cutscene video. 1.0 = full encoded volume. " +
                 "Lower this in the Inspector to set the tuned maximum — player master volume scales on top. " +
                 "e.g. 0.7 means player at 100% hears 70% of the encoded audio.")]
        [SerializeField] [Range(0f, 1f)] private float videoVolume = 1f;

        private AudioSource _videoAudioSource;
        private bool _transitioning = false;
        private bool _inputEnabled  = false;

        private void Start()
        {
            // grab the AudioSource the VideoPlayer routes through (Track 0)
            // must match the Audio Source assigned in the VideoPlayer Inspector
            _videoAudioSource = videoPlayer.GetTargetAudioSource(0);
            ApplyVideoVolume();

            if (SettingsManager.Instance != null)
                SettingsManager.Instance.OnMasterVolumeChanged += OnMasterVolumeChanged;
            else
                Debug.LogWarning("[CUTSCENE] SettingsManager not ready — video volume will not respond to settings changes");

            // String concat NOT Path.Combine — WebGL needs URL forward slashes, not Windows backslashes
            videoPlayer.url = Application.streamingAssetsPath + "/" + videoFileName;
            videoPlayer.loopPointReached += OnVideoFinished;
            videoPlayer.Play();
            StartCoroutine(EnableInputNextFrame());
        }

        private void ApplyVideoVolume()
        {
            if (_videoAudioSource == null) return;
            float master = SettingsManager.Instance != null ? SettingsManager.Instance.MasterVolume : 1f;
            _videoAudioSource.volume = videoVolume * master;
        }

        private void OnMasterVolumeChanged(float masterVolume)
        {
            if (_videoAudioSource != null)
                _videoAudioSource.volume = videoVolume * masterVolume;
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

        private void OnDestroy()
        {
            if (SettingsManager.Instance != null)
                SettingsManager.Instance.OnMasterVolumeChanged -= OnMasterVolumeChanged;
        }
    }
}
