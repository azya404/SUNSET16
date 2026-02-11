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
        [SerializeField] private Button quitButton;

        [Header("Panels")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject newGameConfirmPanel;

        [Header("Confirmation Dialog")]
        [SerializeField] private Button confirmNewGameButton;
        [SerializeField] private Button cancelNewGameButton;

        private const string CORE_SCENE_NAME = "CoreScene";

        private void Start()
        {
            SettingsManager.Instance.Initialize();
            SaveManager.Instance.Initialize();

            newGameButton.onClick.AddListener(OnNewGameClicked);
            continueButton.onClick.AddListener(OnContinueClicked);
            settingsButton.onClick.AddListener(OnSettingsClicked);
            quitButton.onClick.AddListener(OnQuitClicked);

            if (newGameConfirmPanel != null)
            {
                confirmNewGameButton.onClick.AddListener(OnConfirmNewGame);
                cancelNewGameButton.onClick.AddListener(OnCancelNewGame);
                newGameConfirmPanel.SetActive(false);
            }

            continueButton.interactable = SaveManager.Instance.SaveExists;

            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                quitButton.gameObject.SetActive(false);
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }

            Debug.Log("[MAINMENU] Main menu initialized");
        }

        private void OnNewGameClicked()
        {
            if (SaveManager.Instance.SaveExists && newGameConfirmPanel != null)
            {
                newGameConfirmPanel.SetActive(true);
            }
            else
            {
                StartNewGame();
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
            SaveManager.Instance.ClearSaveData();
            Debug.Log("[MAINMENU] Starting new game");
            SceneManager.LoadScene(CORE_SCENE_NAME);
        }

        private void OnContinueClicked()
        {
            Debug.Log("[MAINMENU] Continuing saved game");
            SceneManager.LoadScene(CORE_SCENE_NAME);
        }

        private void OnSettingsClicked()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(!settingsPanel.activeSelf);
            }
        }

        private void OnQuitClicked()
        {
            Debug.Log("[MAINMENU] Quitting application");
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
    }
}