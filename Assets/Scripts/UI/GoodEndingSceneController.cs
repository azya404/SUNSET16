/*
Standalone good ending scene controller.
No CoreScene/managers required — this scene is fully self-contained.

VideoPlayer renders to Camera Far Plane (no RenderTexture or Canvas needed).
Video file must be in Assets/StreamingAssets/ — filename set in Inspector.

Close button (top-right X): fades to black then returns to MainMenuScene.
Video end: fades to black then returns to MainMenuScene automatically.
Space key: does nothing — exit is intentional via X button only.
*/
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;

namespace SUNSET16.UI
{
    public class GoodEndingSceneController : MonoBehaviour
    {
        [Header("Video")]
        [SerializeField] private VideoPlayer videoPlayer;
        [Tooltip("Filename only — must be in Assets/StreamingAssets/. No path, no subfolders.")]
        [SerializeField] private string videoFileName = "CutsceneGoodEnding.mp4";

        [Header("UI")]
        [SerializeField] private Button closeButton;
        [Tooltip("Full-screen black CanvasGroup — starts transparent, fades to black on exit.")]
        [SerializeField] private CanvasGroup screenFadePanel;
        [SerializeField] private float fadeOutDuration = 1.2f;

        private bool _transitioning = false;
        private bool _inputEnabled  = false;

        private const string MAIN_MENU_SCENE = "MainMenuScene";

        private void Start()
        {
            videoPlayer.url = Application.streamingAssetsPath + "/" + videoFileName;
            videoPlayer.loopPointReached += OnVideoFinished;
            videoPlayer.Play();

            closeButton.onClick.AddListener(OnCloseClicked);

            if (screenFadePanel != null)
                screenFadePanel.alpha = 0f;

            // one frame delay so the scene finishes loading before input is live
            StartCoroutine(EnableInputNextFrame());
        }

        private IEnumerator EnableInputNextFrame()
        {
            yield return null;
            _inputEnabled = true;
        }

        // Space intentionally does nothing — exit is via X button only
        // Update() not implemented to ensure no accidental keyboard skip

        private void OnVideoFinished(VideoPlayer vp)
        {
            if (!_transitioning)
                ReturnToMainMenu();
        }

        private void OnCloseClicked()
        {
            if (!_inputEnabled || _transitioning) return;
            ReturnToMainMenu();
        }

        private void ReturnToMainMenu()
        {
            _transitioning = true;
            videoPlayer.Stop();
            StartCoroutine(FadeOutThenLoad());
        }

        private IEnumerator FadeOutThenLoad()
        {
            if (screenFadePanel != null)
            {
                float timer = 0f;
                while (timer < fadeOutDuration)
                {
                    timer += Time.deltaTime;
                    screenFadePanel.alpha = Mathf.Lerp(0f, 1f, timer / fadeOutDuration);
                    yield return null;
                }
                screenFadePanel.alpha = 1f;
            }

            MainMenuController.ReturnedFromCredits = true;
            SceneManager.LoadScene(MAIN_MENU_SCENE);
        }
    }
}
