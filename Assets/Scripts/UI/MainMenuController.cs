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

            Debug.Log("[MAINMENU] Main menu initialized");
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
            StartNewGame();
        }

        private void OnCancelNewGame()
        {
            newGameConfirmPanel.SetActive(false);
        }

        private void StartNewGame()
        {
            SaveManager.Instance.ClearSaveData(); //wipe the old save first
            Debug.Log($"[MAINMENU] Starting new game - Loading {newGameSceneName}");
            SceneManager.LoadScene(newGameSceneName); //straight to bedroom for tech demo
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