using UnityEngine;
using System;

namespace SUNSET16.Core
{
    public class SettingsManager : Singleton<SettingsManager>
    {
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
            IsInitialized = true;
        }

        /*API calls info to dig into before coding more
            https://docs.unity3d.com/ScriptReference/PlayerPrefs.Save.html
            https://docs.unity3d.com/ScriptReference/PlayerPrefs.GetFloat.html
            https://docs.unity3d.com/ScriptReference/PlayerPrefs.SetFloat.html
        */
        public void SetMasterVolume(float volume) { }
        public void SetMusicVolume(float volume) { }
        public void SetSFXVolume(float volume) { }
        public void SetBrightness(float brightness) { }
        public void ResetToDefaults() { }

        protected override void Awake()
        {
            base.Awake();
        }
    }
}