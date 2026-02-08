using UnityEngine;
using System;

namespace SUNSET16.Core
{
    public class SettingsManager : Singleton<SettingsManager>
    {
        private const float DEFAULT_MASTER_VOLUME = 0.8f;
        private const float DEFAULT_MUSIC_VOLUME = 0.7f;
        private const float DEFAULT_SFX_VOLUME = 0.7f;
        private const float DEFAULT_BRIGHTNESS = 0.5f;
    
        public float MasterVolume { get; private set; }
        public float MusicVolume { get; private set; }
        public float SFXVolume { get; private set; }
        public float Brightness { get; private set; }
        public bool IsInitialized { get; private set; }

        public event Action<float> OnMasterVolumeChanged;
        public event Action<float> OnMusicVolumeChanged;
        public event Action<float> OnSFXVolumeChanged;
        public event Action<float> OnBrightnessChanged;

        public void Initialize()
        {
            LoadSettings();
            IsInitialized = true;
            Debug.Log("[SETTINGSMANAGER] works :)");
        }

        private void LoadSettings()
        {
            MasterVolume = PlayerPrefs.GetFloat("SUNSET16_MasterVolume", DEFAULT_MASTER_VOLUME);
            MusicVolume = PlayerPrefs.GetFloat("SUNSET16_MusicVolume", DEFAULT_MUSIC_VOLUME);
            SFXVolume = PlayerPrefs.GetFloat("SUNSET16_SFXVolume", DEFAULT_SFX_VOLUME);
            Brightness = PlayerPrefs.GetFloat("SUNSET16_Brightness", DEFAULT_BRIGHTNESS);

            AudioListener.volume = MasterVolume;
        }

        /*API calls info to dig into before coding more
            https://docs.unity3d.com/ScriptReference/PlayerPrefs.Save.html
            https://docs.unity3d.com/ScriptReference/PlayerPrefs.GetFloat.html
            https://docs.unity3d.com/ScriptReference/PlayerPrefs.SetFloat.html
        */

        public void SetMasterVolume(float volume)
        {
            MasterVolume = Mathf.Clamp01(volume);
            AudioListener.volume = MasterVolume;
            OnMasterVolumeChanged?.Invoke(MasterVolume);
            SaveSettings();
        }

        public void SetMusicVolume(float volume)
        {
            MusicVolume = Mathf.Clamp01(volume);
            OnMusicVolumeChanged?.Invoke(MusicVolume);
            SaveSettings();
        }

        public void SetSFXVolume(float volume)
        {
            SFXVolume = Mathf.Clamp01(volume);
            OnSFXVolumeChanged?.Invoke(SFXVolume);
            SaveSettings();
        }

        public void SetBrightness(float brightness)
        {
            Brightness = Mathf.Clamp01(brightness);
            OnBrightnessChanged?.Invoke(Brightness);
            SaveSettings();
        }

        public void ResetToDefaults()
        {
            SetMasterVolume(DEFAULT_MASTER_VOLUME);
            SetMusicVolume(DEFAULT_MUSIC_VOLUME);
            SetSFXVolume(DEFAULT_SFX_VOLUME);
            SetBrightness(DEFAULT_BRIGHTNESS);

            Debug.Log("[SettingsManager] Reset to default settings");
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetFloat("SUNSET16_MasterVolume", MasterVolume);
            PlayerPrefs.SetFloat("SUNSET16_MusicVolume", MusicVolume);
            PlayerPrefs.SetFloat("SUNSET16_SFXVolume", SFXVolume);
            PlayerPrefs.SetFloat("SUNSET16_Brightness", Brightness);
            PlayerPrefs.Save();
        }

        protected override void Awake()
        {
            base.Awake();
        }
    }
}