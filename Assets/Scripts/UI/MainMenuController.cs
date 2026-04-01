/*
the main menu - first thing the player sees when they open the game
has buttons for New Game, Continue, Settings, Credits

new game will check if a save already exists and if so shows a
confirmation popup so they dont accidentally overwrite their progress
if no save exists it just goes straight to the bedroom scene

continue loads CoreScene which has all the managers and they
auto-init and pull save data from PlayerPrefs

settings just toggles the settings panel on/off and credits
doesnt do anything yet lol

TODO: credits scene
TODO: transition animation when going from menu to game

FADE SEQUENCE (on New Game / Continue / Credits click):
  Phase 1 - menu_character + menu_space_bg fade out simultaneously
            buttons are disabled immediately, still visible
  Phase 2 - menu_stars + ButtonGroup fade out simultaneously
            only menu_title remains
  Phase 3 - menu_title fades out, then scene loads
*/
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using SUNSET16.Core;

namespace SUNSET16.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button creditsButton;

        [Header("Panels")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject newGameConfirmPanel;

        [Header("Confirmation Dialog")]
        [SerializeField] private Button confirmNewGameButton;
        [SerializeField] private Button cancelNewGameButton;

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

        [Header("Fade Durations")]
        [SerializeField] private float phase1Duration = 0.6f;
        [SerializeField] private float phase2Duration = 0.5f;
        [SerializeField] private float phase3Duration = 0.8f;

        [Header("Scene Names")]
        [SerializeField] private string newGameSceneName  = "CoreScene";
        [SerializeField] private string creditsSceneName  = "NeutralCreditsScene";
        private const string CORE_SCENE_NAME = "CoreScene";

        // set by NeutralCreditsSceneController before returning to this scene
        public static bool ReturnedFromCredits = false;

        private Coroutine _musicLoopCoroutine;
        private bool      _sceneFadeActive = false;

        private void Start()
        {
            //gotta init these two ourselves cos we're on the menu scene, not CoreScene
            //GameManager doesnt exist here so nobody else is gonna do it
            SettingsManager.Instance.Initialize();
            SaveManager.Instance.Initialize();

            //wire up all the button click listeners
            newGameButton.onClick.AddListener(OnNewGameClicked);
            continueButton.onClick.AddListener(OnContinueClicked);
            settingsButton.onClick.AddListener(OnSettingsClicked);
            creditsButton.onClick.AddListener(OnCreditsClicked);

            //if theres a confirmation panel set up, wire those buttons too and hide it
            if (newGameConfirmPanel != null)
            {
                confirmNewGameButton.onClick.AddListener(OnConfirmNewGame);
                cancelNewGameButton.onClick.AddListener(OnCancelNewGame);
                newGameConfirmPanel.SetActive(false);
            }

            //gray out the continue button if theres no save to load
            continueButton.interactable = SaveManager.Instance.SaveExists;

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

            if (SaveManager.Instance.SaveExists && newGameConfirmPanel != null)
                newGameConfirmPanel.SetActive(true);
            else
                StartNewGame();
        }

        private void OnConfirmNewGame()
        {
            newGameConfirmPanel.SetActive(false);
            sfxSource?.PlayOneShot(menuClickSFX);
            StartNewGame();
        }

        private void OnCancelNewGame()
        {
            sfxSource?.PlayOneShot(menuClickSFX);
            newGameConfirmPanel.SetActive(false);
        }

        private void StartNewGame()
        {
            SaveManager.Instance.ClearSaveData();
            StartCoroutine(LayerFadeAndLoad(newGameSceneName));
        }

        private void OnContinueClicked()
        {
            sfxSource?.PlayOneShot(startButtonSFX);
            Debug.Log("[MAINMENU] Continuing saved game");
            StartCoroutine(LayerFadeAndLoad(CORE_SCENE_NAME));
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
            // lock all buttons immediately — no mashing during fade
            newGameButton.interactable  = false;
            continueButton.interactable = false;
            settingsButton.interactable = false;
            creditsButton.interactable  = false;

            _sceneFadeActive = true;
            if (_musicLoopCoroutine != null) StopCoroutine(_musicLoopCoroutine);

            float totalDuration = phase1Duration + phase2Duration + phase3Duration;
            StartCoroutine(FadeMusic(musicSource != null ? musicSource.volume : 1f, 0f, totalDuration));

            // phase 1 — character + space bg fade out together
            StartCoroutine(FadeCanvasGroup(characterLayer, 1f, 0f, phase1Duration));
            yield return StartCoroutine(FadeCanvasGroup(spaceBackgroundLayer, 1f, 0f, phase1Duration));

            // phase 2 — stars + buttons fade out together
            StartCoroutine(FadeCanvasGroup(starsLayer, 1f, 0f, phase2Duration));
            yield return StartCoroutine(FadeCanvasGroup(buttonsLayer, 1f, 0f, phase2Duration));

            // phase 3 — title fades out last
            yield return StartCoroutine(FadeCanvasGroup(titleLayer, 1f, 0f, phase3Duration));

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
