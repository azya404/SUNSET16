using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SUNSET16.Core;

namespace SUNSET16.UI
{
    public class SettingsPanel : MonoBehaviour
    {
        [Header("Volume Sliders")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        [Header("Brightness")]
        [SerializeField] private Slider brightnessSlider;

        [Header("Labels")]
        [SerializeField] private TMP_Text masterVolumeLabel;
        [SerializeField] private TMP_Text musicVolumeLabel;
        [SerializeField] private TMP_Text sfxVolumeLabel;
        [SerializeField] private TMP_Text brightnessLabel;

        [Header("Buttons")]
        [SerializeField] private Button resetDefaultsButton;
        [SerializeField] private Button closeButton;

        private void OnEnable()
        {
            if (SettingsManager.Instance == null) return;

            masterVolumeSlider.value = SettingsManager.Instance.MasterVolume;
            musicVolumeSlider.value = SettingsManager.Instance.MusicVolume;
            sfxVolumeSlider.value = SettingsManager.Instance.SFXVolume;
            brightnessSlider.value = SettingsManager.Instance.Brightness;

            UpdateLabels();

            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);

            if (resetDefaultsButton != null)
                resetDefaultsButton.onClick.AddListener(OnResetDefaults);
            if (closeButton != null)
                closeButton.onClick.AddListener(OnClose);
        }

        private void OnDisable()
        {
            masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
            sfxVolumeSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
            brightnessSlider.onValueChanged.RemoveListener(OnBrightnessChanged);

            if (resetDefaultsButton != null)
                resetDefaultsButton.onClick.RemoveListener(OnResetDefaults);
            if (closeButton != null)
                closeButton.onClick.RemoveListener(OnClose);
        }

        private void OnMasterVolumeChanged(float value)
        {
            SettingsManager.Instance.SetMasterVolume(value);
            UpdateLabels();
        }

        private void OnMusicVolumeChanged(float value)
        {
            SettingsManager.Instance.SetMusicVolume(value);
            UpdateLabels();
        }

        private void OnSFXVolumeChanged(float value)
        {
            SettingsManager.Instance.SetSFXVolume(value);
            UpdateLabels();
        }

        private void OnBrightnessChanged(float value)
        {
            SettingsManager.Instance.SetBrightness(value);
            UpdateLabels();
        }

        private void UpdateLabels()
        {
            if (masterVolumeLabel != null)
                masterVolumeLabel.text = $"Master: {Mathf.RoundToInt(masterVolumeSlider.value * 100)}%";
            if (musicVolumeLabel != null)
                musicVolumeLabel.text = $"Music: {Mathf.RoundToInt(musicVolumeSlider.value * 100)}%";
            if (sfxVolumeLabel != null)
                sfxVolumeLabel.text = $"SFX: {Mathf.RoundToInt(sfxVolumeSlider.value * 100)}%";
            if (brightnessLabel != null)
                brightnessLabel.text = $"Brightness: {Mathf.RoundToInt(brightnessSlider.value * 100)}%";
        }

        private void OnResetDefaults()
        {
            SettingsManager.Instance.ResetToDefaults();

            masterVolumeSlider.value = SettingsManager.Instance.MasterVolume;
            musicVolumeSlider.value = SettingsManager.Instance.MusicVolume;
            sfxVolumeSlider.value = SettingsManager.Instance.SFXVolume;
            brightnessSlider.value = SettingsManager.Instance.Brightness;

            UpdateLabels();
        }

        private void OnClose()
        {
            gameObject.SetActive(false);
        }
    }
}