/*
the main menu - first thing the player sees when they open the game
has buttons for New Game, Continue, Settings, Credits

new game will check if a save already exists and if so shows a
confirmation popup so they dont accidentally overwrite their progress
if no save exists it just goes straight to the bedroom scene

continue loads CoreScene which has all the managers and they
auto-init and pull save data from PlayerPrefs

settings just toggles the settings panel on/off

credits loads NeutralCreditsScene — when returning from credits the menu
fades in from black via ReturnedFromCredits static flag set by NeutralCreditsSceneController

TODO: animated bg for the menu (space station exterior maybe?)
TODO: transition animation when going from menu to game
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

        // set by NeutralCreditsSceneController before loading back to this scene
        // so we know to fade in from black instead of appearing instantly
        public static bool ReturnedFromCredits = false;

        private Coroutine _musicLoopCoroutine;
        private bool      _sceneFadeActive = false;

        [Header("Screen Transition")]
        [Tooltip("Full-screen black CanvasGroup — starts transparent, fades to black on scene exit.")]
        [SerializeField] private CanvasGroup screenFadePanel;
        [SerializeField] private float       screenFadeDuration = 1.2f;

        [Header("Scene Names")]
        [SerializeField] private string newGameSceneName = "CoreScene";
        private const string CORE_SCENE_NAME = "CoreScene";

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
            {
                settingsPanel.SetActive(false);
            }

            if (musicSource != null)
            {
                musicSource.loop   = false; // MusicLoopWithFade manages looping manually
                musicSource.volume = 0f;
                musicSource.Play();
                _musicLoopCoroutine = StartCoroutine(MusicLoopWithFade());
            }

            // returning from credits — fade the menu in from black
            // otherwise just appear instantly (fresh game launch)
            if (ReturnedFromCredits)
            {
                ReturnedFromCredits = false;
                if (screenFadePanel != null)
                {
                    screenFadePanel.alpha = 1f;
                    StartCoroutine(FadeScreen(1f, 0f, screenFadeDuration));
                }
            }
            else if (screenFadePanel != null)
            {
                screenFadePanel.alpha = 0f;
            }

            Debug.Log("[MAINMENU] Main menu initialized");
        }

        // fade in → wait → fade out → restart → repeat until _sceneFadeActive
        private IEnumerator MusicLoopWithFade()
        {
            while (!_sceneFadeActive)
            {
                // fade in
                yield return StartCoroutine(FadeMusic(0f, 1f, musicFadeDuration));

                // wait until musicFadeDuration seconds before the track ends
                float waitTime = musicSource.clip.length - musicSource.time - musicFadeDuration;
                if (waitTime > 0f)
                    yield return new WaitForSeconds(waitTime);

                if (_sceneFadeActive) yield break;

                // fade out to end of track
                float timeLeft = Mathf.Max(musicSource.clip.length - musicSource.time, 0.1f);
                yield return StartCoroutine(FadeMusic(musicSource.volume, 0f, timeLeft));

                if (_sceneFadeActive) yield break;

                // restart from the top
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

            //if they already have a save, make sure they actually wanna overwrite it
            if (SaveManager.Instance.SaveExists && newGameConfirmPanel != null)
            {
                newGameConfirmPanel.SetActive(true);
            }
            else
            {
                StartNewGame(); //no save exists so just go
            }
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
            StartCoroutine(LoadSceneAfterSFX(newGameSceneName));
        }

        private IEnumerator LoadSceneAfterSFX(string sceneName)
        {
            // lock all buttons immediately — prevents button mashing during fade/SFX
            newGameButton.interactable  = false;
            continueButton.interactable = false;
            settingsButton.interactable = false;
            creditsButton.interactable  = false;

            float sfxDuration = (startButtonSFX != null && sfxSource != null) ? startButtonSFX.length : 0f;
            // fade duration matches SFX exactly — screenFadeDuration is fallback only if no clip assigned
            float totalDuration = sfxDuration > 0f ? sfxDuration : screenFadeDuration;

            _sceneFadeActive = true;
            if (_musicLoopCoroutine != null) StopCoroutine(_musicLoopCoroutine);
            if (musicSource != null && musicSource.isPlaying)
                StartCoroutine(FadeMusic(musicSource.volume, 0f, totalDuration));
            if (screenFadePanel != null)
                StartCoroutine(FadeScreen(0f, 1f, totalDuration));

            yield return new WaitForSeconds(totalDuration);
            Debug.Log($"[MAINMENU] Loading {sceneName}");
            SceneManager.LoadScene(sceneName);
        }

        private IEnumerator FadeScreen(float from, float to, float duration)
        {
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                if (screenFadePanel != null)
                    screenFadePanel.alpha = Mathf.Lerp(from, to, timer / duration);
                yield return null;
            }
            if (screenFadePanel != null) screenFadePanel.alpha = to;
        }

        private void OnContinueClicked()
        {
            // use startButtonSFX for continue too — same weight as starting a new game
            sfxSource?.PlayOneShot(startButtonSFX);
            Debug.Log("[MAINMENU] Continuing saved game");
            StartCoroutine(LoadSceneAfterSFX(CORE_SCENE_NAME));
        }

        private void OnSettingsClicked()
        {
            sfxSource?.PlayOneShot(menuClickSFX);
            //just flip the panel on/off, SettingsPanel.cs handles everything inside it
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(!settingsPanel.activeSelf);
            }
        }

        private void OnCreditsClicked()
        {
            sfxSource?.PlayOneShot(menuClickSFX);
            StartCoroutine(LoadSceneAfterSFX("NeutralCreditsScene"));
        }
    }
}