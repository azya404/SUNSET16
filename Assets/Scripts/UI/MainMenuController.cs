/*
the main menu - first thing the player sees when they open the game
has buttons for New Game, Settings, Credits

New Game always starts a fresh save — no confirmation dialog,
no continue button. SaveManager.ClearSaveData() runs every time.

FADE SEQUENCE (on New Game / Credits click):
  Phase 1 - buttons locked, nothing visual changes, music plays
            holds for startButtonSFX.length + 0.25s
  Phase 2 - buttons fade out over 0.5s, everything else still visible
  Phase 3 - character, space bg, stars all fade out over 0.5s
            title remains on screen, music still plays
  Phase 4 - title fades out over 4s, music fades simultaneously
            scene loads when complete
*/
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using SUNSET16.Core;

namespace SUNSET16.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button creditsButton;

        [Header("Panels")]
        [SerializeField] private GameObject settingsPanel;

        [Header("Audio")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip   startButtonSFX;
        [SerializeField] private AudioClip   menuClickSFX;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private float       musicFadeDuration = 1.5f;

        [Header("Layer CanvasGroups")]
        [SerializeField] private CanvasGroup spaceBackgroundLayer;
        [SerializeField] private CanvasGroup starsLayer;
        [SerializeField] private CanvasGroup titleLayer;
        [SerializeField] private CanvasGroup characterLayer;
        [SerializeField] private CanvasGroup buttonsLayer;

        [Header("Scene Names")]
        [SerializeField] private string newGameSceneName = "CoreScene";
        [SerializeField] private string creditsSceneName = "NeutralCreditsScene";

        // set by NeutralCreditsSceneController before returning to this scene
        public static bool ReturnedFromCredits = false;

        private Coroutine _musicLoopCoroutine;
        private bool      _sceneFadeActive = false;
        private float     _sfxDuration;

        // fade sequence durations — fixed, read once from clip in Start()
        private const float SfxHoldPadding    = 0.25f;
        private const float ButtonFadeDuration = 0.5f;
        private const float LayerFadeDuration  = 0.5f;
        private const float TitleFadeDuration  = 4f;

        private void Start()
        {
            //gotta init these two ourselves cos we're on the menu scene, not CoreScene
            //GameManager doesnt exist here so nobody else is gonna do it
            SettingsManager.Instance.Initialize();
            SaveManager.Instance.Initialize();

            // read SFX length once — used as phase 1 hold duration
            _sfxDuration = startButtonSFX != null ? startButtonSFX.length : 0f;

            newGameButton.onClick.AddListener(OnNewGameClicked);
            settingsButton.onClick.AddListener(OnSettingsClicked);
            creditsButton.onClick.AddListener(OnCreditsClicked);

            if (settingsPanel != null)
                settingsPanel.SetActive(false);

            if (musicSource != null)
            {
                musicSource.loop   = false;
                musicSource.volume = 0f;
                musicSource.Play();
                _musicLoopCoroutine = StartCoroutine(MusicLoopWithFade());
            }

            Debug.Log("[MAINMENU] Main menu initialized");
        }

        // fade in → wait → fade out → restart → repeat until _sceneFadeActive
        private IEnumerator MusicLoopWithFade()
        {
            while (!_sceneFadeActive)
            {
                yield return StartCoroutine(FadeMusic(0f, 1f, musicFadeDuration));

                float waitTime = musicSource.clip.length - musicSource.time - musicFadeDuration;
                if (waitTime > 0f)
                    yield return new WaitForSeconds(waitTime);

                if (_sceneFadeActive) yield break;

                float timeLeft = Mathf.Max(musicSource.clip.length - musicSource.time, 0.1f);
                yield return StartCoroutine(FadeMusic(musicSource.volume, 0f, timeLeft));

                if (_sceneFadeActive) yield break;

                musicSource.Stop();
                musicSource.volume = 0f;
                musicSource.Play();
            }
        }

        private IEnumerator FadeMusic(float from, float to, float duration)
        {
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                if (musicSource != null)
                    musicSource.volume = Mathf.Lerp(from, to, timer / duration);
                yield return null;
            }
            if (musicSource != null) musicSource.volume = to;
        }

        private void OnNewGameClicked()
        {
            sfxSource?.PlayOneShot(startButtonSFX);
            SaveManager.Instance.ClearSaveData();
            StartCoroutine(LayerFadeAndLoad(newGameSceneName));
        }

        private void OnSettingsClicked()
        {
            sfxSource?.PlayOneShot(menuClickSFX);
            if (settingsPanel != null)
                settingsPanel.SetActive(!settingsPanel.activeSelf);
        }

        private void OnCreditsClicked()
        {
            sfxSource?.PlayOneShot(menuClickSFX);
            StartCoroutine(LayerFadeAndLoad(creditsSceneName));
        }

        private IEnumerator LayerFadeAndLoad(string sceneName)
        {
            // lock all buttons immediately — no visual change yet
            newGameButton.interactable  = false;
            settingsButton.interactable = false;
            creditsButton.interactable  = false;

            _sceneFadeActive = true;
            if (_musicLoopCoroutine != null) StopCoroutine(_musicLoopCoroutine);

            // phase 1 — hold for SFX length + padding, everything visible, music plays
            yield return new WaitForSeconds(_sfxDuration + SfxHoldPadding);

            // phase 2 — buttons fade out, everything else still visible, music plays
            yield return StartCoroutine(FadeCanvasGroup(buttonsLayer, 1f, 0f, ButtonFadeDuration));

            // phase 3 — character, space bg, stars fade out simultaneously, title remains, music plays
            StartCoroutine(FadeCanvasGroup(characterLayer, 1f, 0f, LayerFadeDuration));
            StartCoroutine(FadeCanvasGroup(spaceBackgroundLayer, 1f, 0f, LayerFadeDuration));
            yield return StartCoroutine(FadeCanvasGroup(starsLayer, 1f, 0f, LayerFadeDuration));

            // phase 4 — title fades out, music fades simultaneously, then scene loads
            StartCoroutine(FadeMusic(musicSource != null ? musicSource.volume : 1f, 0f, TitleFadeDuration));
            yield return StartCoroutine(FadeCanvasGroup(titleLayer, 1f, 0f, TitleFadeDuration));

            Debug.Log($"[MAINMENU] Loading {sceneName}");
            SceneManager.LoadScene(sceneName);
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
        {
            if (cg == null) yield break;
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                cg.alpha = Mathf.Lerp(from, to, timer / duration);
                yield return null;
            }
            cg.alpha = to;
        }
    }
}
