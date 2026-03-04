using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;
using System.IO;

namespace SUNSET16.UI
{
    public class CutsceneController : MonoBehaviour
    {
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private string nextSceneName = "CoreScene";
        [SerializeField] private string videoFileName = "cutscene.mp4";

        private bool _skipped = false;

        private void Start()
        {
            videoPlayer.url = Path.Combine(Application.streamingAssetsPath, videoFileName);
            videoPlayer.loopPointReached += OnVideoFinished;
            videoPlayer.Play();
        }

        private void Update()
        {
            // allow player to skip with Space, Enter, or Escape
            if (!_skipped && (Input.GetKeyDown(KeyCode.Space)
                || Input.GetKeyDown(KeyCode.Return)
                || Input.GetKeyDown(KeyCode.Escape)))
            {
                Skip();
            }
        }

        private void OnVideoFinished(VideoPlayer vp)
        {
            if (!_skipped)
                LoadNextScene();
        }

        private void Skip()
        {
            _skipped = true;
            videoPlayer.Stop();
            LoadNextScene();
        }

        private void LoadNextScene()
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
