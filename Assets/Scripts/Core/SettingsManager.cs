/*
THE PREFERENCES KEEPER - handles player settings that persist even across new games
this is SEPARATE from save data (SaveManager) - settings are user preferences, not game progress

settings include: master volume, music volume, sfx volume, and screen brightness
these are saved to PlayerPrefs (browser localStorage for WebGL) and loaded on startup

KEY DIFFERENCE FROM SAVE DATA:
- settings persist even if the player deletes their save / starts a new game
- settings are loaded once on init and updated whenever the player changes them
- save data is game progress that gets wiped on "delete save"

HOW IT WORKS:
1. on Initialize(), loads stored values from PlayerPrefs (or defaults if first time)
2. when player adjusts a slider in SettingsPanel UI, SetXVolume/SetBrightness is called
3. the value is clamped 0-1, stored locally, saved to PlayerPrefs immediately
4. an event fires so the actual audio/visual systems can react (AudioManager, LightingController)

WEBGL NOTE: PlayerPrefs.Save() is called after EVERY setting change
this is CRITICAL for WebGL cos otherwise the data sits in memory and gets lost
on desktop builds Unity auto-saves PlayerPrefs periodically, but WebGL doesnt do that
(we learned this the hard way - settings kept resetting in the browser lol)

HOW THIS EVOLVED:
- originally had API reference links for PlayerPrefs (still in the code lol)
- brightness was going to control a post-processing Volume directly but we simplified 
  it to just fire an event that LightingController and VisualStateController listen to
- added ResetToDefaults() which wasnt in the original plan

TODO: add accessibility options (colorblind mode, text size, etc)
TODO: add mouse sensitivity / keybinding settings if we expand controls
*/
using UnityEngine;
using System;

namespace SUNSET16.Core
{
    public class SettingsManager : Singleton<SettingsManager>
    {
        //default values for first-time players (before they change anything)
        //chose 0.8 for master and 0.7 for music/sfx cos full volume is kinda aggressive
        //brightness 1.0 = fully clear (no darkening), 0.0 = fully black
        private const float DEFAULT_MASTER_VOLUME = 0.8f;
        private const float DEFAULT_MUSIC_VOLUME = 0.7f;
        private const float DEFAULT_SFX_VOLUME = 0.7f;
        private const float DEFAULT_BRIGHTNESS = 1.0f;
    
        //current values - public get for other systems to read, private set so only we can change them
        public float MasterVolume { get; private set; }
        public float MusicVolume { get; private set; }
        public float SFXVolume { get; private set; }
        public float Brightness { get; private set; }
        public bool IsInitialized { get; private set; }

        //events for other systems to subscribe to
        //AudioManager listens to volume changes, LightingController listens to brightness
        public event Action<float> OnMasterVolumeChanged;
        public event Action<float> OnMusicVolumeChanged;
        public event Action<float> OnSFXVolumeChanged;
        public event Action<float> OnBrightnessChanged;

        //called by GameManager during initialization sequence
        public void Initialize()
        {
            LoadSettings();
            IsInitialized = true;
            Debug.Log("[SETTINGSMANAGER] works :)"); //lol keeping this
        }

        //loads settings from PlayerPrefs or uses defaults if no stored values exist
        //the second parameter in GetFloat is the default value if the key doesnt exist
        private void LoadSettings()
        {
            MasterVolume = PlayerPrefs.GetFloat("SUNSET16_MasterVolume", DEFAULT_MASTER_VOLUME);
            MusicVolume = PlayerPrefs.GetFloat("SUNSET16_MusicVolume", DEFAULT_MUSIC_VOLUME);
            SFXVolume = PlayerPrefs.GetFloat("SUNSET16_SFXVolume", DEFAULT_SFX_VOLUME);
            Brightness = PlayerPrefs.GetFloat("SUNSET16_Brightness", DEFAULT_BRIGHTNESS);

            //AudioListener.volume is Unitys GLOBAL volume multiplier
            //this affects ALL audio in the game - its the master knob
            AudioListener.volume = MasterVolume;
        }

        /*API calls info to dig into before coding more
            https://docs.unity3d.com/ScriptReference/PlayerPrefs.Save.html
            https://docs.unity3d.com/ScriptReference/PlayerPrefs.GetFloat.html
            https://docs.unity3d.com/ScriptReference/PlayerPrefs.SetFloat.html
        */

        //each Set method follows the same pattern:
        //1. clamp value to 0-1 range (Mathf.Clamp01)
        //2. fire the change event (so AudioManager/LightingController can react)
        //3. save to PlayerPrefs immediately (important for WebGL!)

        public void SetMasterVolume(float volume)
        {
            MasterVolume = Mathf.Clamp01(volume); //Clamp01 = clamp between 0 and 1
            AudioListener.volume = MasterVolume;   //apply directly to Unitys global volume
            OnMasterVolumeChanged?.Invoke(MasterVolume);
            SaveSettings(); //save immediately for WebGL persistence
        }

        public void SetMusicVolume(float volume)
        {
            MusicVolume = Mathf.Clamp01(volume);
            OnMusicVolumeChanged?.Invoke(MusicVolume); //AudioManager adjusts its music AudioSource
            SaveSettings();
        }

        public void SetSFXVolume(float volume)
        {
            SFXVolume = Mathf.Clamp01(volume);
            OnSFXVolumeChanged?.Invoke(SFXVolume); //AudioManager adjusts its sfx AudioSource
            SaveSettings();
        }

        public void SetBrightness(float brightness)
        {
            Brightness = Mathf.Clamp01(brightness);
            OnBrightnessChanged?.Invoke(Brightness); //LightingController adjusts post-processing or light intensity
            SaveSettings();
        }

        //resets all settings to defaults (called from settings panel "Reset" button)
        //uses the Set methods so events fire and everything updates properly
        public void ResetToDefaults()
        {
            SetMasterVolume(DEFAULT_MASTER_VOLUME);
            SetMusicVolume(DEFAULT_MUSIC_VOLUME);
            SetSFXVolume(DEFAULT_SFX_VOLUME);
            SetBrightness(DEFAULT_BRIGHTNESS);

            Debug.Log("[SettingsManager] Reset to default settings");
        }

        //saves all current settings to PlayerPrefs
        //PlayerPrefs.Save() is called EVERY TIME cos WebGL needs explicit saves
        //on desktop this would be fine without Save() but we do it anyway for consistency
        private void SaveSettings()
        {
            PlayerPrefs.SetFloat("SUNSET16_MasterVolume", MasterVolume);
            PlayerPrefs.SetFloat("SUNSET16_MusicVolume", MusicVolume);
            PlayerPrefs.SetFloat("SUNSET16_SFXVolume", SFXVolume);
            PlayerPrefs.SetFloat("SUNSET16_Brightness", Brightness);
            PlayerPrefs.Save(); //CRITICAL for WebGL - without this, settings vanish on page close
        }

        protected override void Awake()
        {
            base.Awake(); //Singleton handles instance management
        }
    }
}
