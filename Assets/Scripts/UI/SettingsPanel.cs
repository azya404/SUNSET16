/*
settings UI panel - sliders for volume (master, music, sfx) and brightness
opened from the main menu rn, eventually from an in-game pause menu too

each slider calls SettingsManager when you drag it, and SettingsManager
fires events that AudioManager and VisualStateController pick up
so the chain is: slider drag -> SettingsManager -> events -> systems react

we use OnEnable/OnDisable to subscribe/unsubscribe the slider listeners
instead of Start/OnDestroy cos this panel gets toggled on and off a lot
if we used OnDestroy the listeners would stack up every time you open it
and youd get duplicate callbacks which is no good

when the panel opens it syncs all slider positions to whatever
SettingsManager currently has so they always show the right values

TODO: resolution/quality dropdown
TODO: fullscreen toggle
TODO: keybinding remapping
TODO: play an sfx sample when adjusting sfx volume so you can hear the change
*/
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

        [Header("Audio")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip   clickSFX;

        private void OnEnable()
        {
            if (SettingsManager.Instance == null) return;

            //sync sliders to whatever SettingsManager currently has
            //so if the player changed something and closed/reopened, its all still right
            masterVolumeSlider.value = SettingsManager.Instance.MasterVolume;
            musicVolumeSlider.value = SettingsManager.Instance.MusicVolume;
            sfxVolumeSlider.value = SettingsManager.Instance.SFXVolume;
            brightnessSlider.value = SettingsManager.Instance.Brightness;

            UpdateLabels();

            //subscribe to slider changes - these fire every time the slider moves
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
            //MUST unsub here or youll get ghost callbacks and eventual memory leaks
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
            SettingsManager.Instance.SetMasterVolume(value); //this fires an event that AudioManager picks up
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
            //multiply by 100 and round so it shows as a nice percentage
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
            sfxSource?.PlayOneShot(clickSFX);
            SettingsManager.Instance.ResetToDefaults();

            //resync the sliders to the new default values
            masterVolumeSlider.value = SettingsManager.Instance.MasterVolume;
            musicVolumeSlider.value = SettingsManager.Instance.MusicVolume;
            sfxVolumeSlider.value = SettingsManager.Instance.SFXVolume;
            brightnessSlider.value = SettingsManager.Instance.Brightness;

            UpdateLabels();
        }

        private void OnClose()
        {
            sfxSource?.PlayOneShot(clickSFX);
            gameObject.SetActive(false); //just hide the panel, OnDisable handles cleanup
        }
    }
}