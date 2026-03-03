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

        private Coroutine _musicLoopCoroutine;
        private bool      _sceneFadeActive = false;

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
            float delay = (startButtonSFX != null && sfxSource != null) ? startButtonSFX.length : 0f;

            // stop the loop coroutine and fade out over the SFX duration
            _sceneFadeActive = true;
            if (_musicLoopCoroutine != null) StopCoroutine(_musicLoopCoroutine);
            if (musicSource != null && musicSource.isPlaying)
                StartCoroutine(FadeMusic(musicSource.volume, 0f, delay));

            yield return new WaitForSeconds(delay);
            Debug.Log($"[MAINMENU] Starting new game - Loading {sceneName}");
            SceneManager.LoadScene(sceneName);
        }

        private void OnContinueClicked()
        {
            sfxSource?.PlayOneShot(menuClickSFX);
            //load CoreScene and let the managers handle everything from there
            Debug.Log("[MAINMENU] Continuing saved game");
            SceneManager.LoadScene(CORE_SCENE_NAME);
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
            Debug.Log("[MAINMENU] Credits scene not yet implemented");
        }
    }
}