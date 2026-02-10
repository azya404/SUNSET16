using UnityEngine;
using System.Collections;

namespace SUNSET16.Core
{
    public class AudioManager : Singleton<AudioManager>
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;    
        [SerializeField] private AudioSource sfxSource;   

        [Header("Music Tracks")]
        [SerializeField] private AudioClip onPillMusic;    
        [SerializeField] private AudioClip offPillMusic;   
        [SerializeField] private AudioClip nightMusic;   
        [SerializeField] private AudioClip menuMusic;    
        [SerializeField] private AudioClip badEndingMusic;   
        [SerializeField] private AudioClip goodEndingMusic;  

        [Header("Sound Effects")]
        [SerializeField] private AudioClip doorOpen;
        [SerializeField] private AudioClip doorClose;
        [SerializeField] private AudioClip pillTake;
        [SerializeField] private AudioClip taskComplete;
        [SerializeField] private AudioClip uiClick;
        [SerializeField] private AudioClip footstep;

        [Header("Crossfade Settings")]
        [SerializeField] private float crossfadeDuration = 1.0f;

        private float _masterVolume = 1.0f;
        private float _musicVolume = 1.0f;
        private float _sfxVolume = 1.0f;

        private Coroutine _crossfadeCoroutine;

        protected override void Awake()
        {
            base.Awake();
            InitializeAudioSources();
        }

        private void Start()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsInitialized)
            {
                SubscribeToEvents();
            }
            else if (GameManager.Instance != null)
            {
                GameManager.Instance.OnInitializationComplete += SubscribeToEvents;
            }
        }

        private void SubscribeToEvents()
        {
            SettingsManager.Instance.OnMasterVolumeChanged += OnMasterVolumeChanged;
            SettingsManager.Instance.OnMusicVolumeChanged += OnMusicVolumeChanged;
            SettingsManager.Instance.OnSFXVolumeChanged += OnSFXVolumeChanged;

            PillStateManager.Instance.OnPillTaken += OnPillTaken;

            DayManager.Instance.OnPhaseChanged += OnPhaseChanged;

            PillStateManager.Instance.OnEndingReached += OnEndingReached;

            _masterVolume = SettingsManager.Instance.MasterVolume;
            _musicVolume = SettingsManager.Instance.MusicVolume;
            _sfxVolume = SettingsManager.Instance.SFXVolume;
            ApplyMusicVolume();

            Debug.Log("[AUDIOMANAGER] Subscribed to events and initialized");
        }

        private void InitializeAudioSources()
        {
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
            }
        }

        public void PlayMusic(AudioClip clip, bool fade = true)
        {
            if (clip == null)
            {
                Debug.LogWarning("[AUDIOMANAGER] Attempted to play null music clip");
                return;
            }

            if (musicSource.clip == clip && musicSource.isPlaying)
                return;

            if (fade && musicSource.isPlaying)
            {
                if (_crossfadeCoroutine != null)
                    StopCoroutine(_crossfadeCoroutine);
                _crossfadeCoroutine = StartCoroutine(CrossfadeMusic(clip));
            }
            else
            {
                musicSource.clip = clip;
                musicSource.Play();
                ApplyMusicVolume();
            }
        }

        private IEnumerator CrossfadeMusic(AudioClip newClip)
        {
            float timer = 0;
            float startVolume = musicSource.volume;

            while (timer < crossfadeDuration)
            {
                timer += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0, timer / crossfadeDuration);
                yield return null;
            }

            musicSource.clip = newClip;
            musicSource.Play();

            timer = 0;
            float targetVolume = _musicVolume * _masterVolume;
            while (timer < crossfadeDuration)
            {
                timer += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(0, targetVolume, timer / crossfadeDuration);
                yield return null;
            }

            musicSource.volume = targetVolume;
            _crossfadeCoroutine = null;
        }

        public void PlaySFX(AudioClip clip, float volumeScale = 1.0f)
        {
            if (clip == null)
            {
                Debug.LogWarning("[AUDIOMANAGER] Attempted to play null SFX clip");
                return;
            }

            sfxSource.PlayOneShot(clip, volumeScale * _sfxVolume * _masterVolume);
        }

        public void PlayDoorOpen() => PlaySFX(doorOpen);
        public void PlayDoorClose() => PlaySFX(doorClose);
        public void PlayPillTake() => PlaySFX(pillTake);
        public void PlayTaskComplete() => PlaySFX(taskComplete);
        public void PlayUIClick() => PlaySFX(uiClick);
        public void PlayFootstep() => PlaySFX(footstep, 0.5f);

        private void OnMasterVolumeChanged(float volume)
        {
            _masterVolume = volume;
            ApplyMusicVolume();
        }

        private void OnMusicVolumeChanged(float volume)
        {
            _musicVolume = volume;
            ApplyMusicVolume();
        }

        private void OnSFXVolumeChanged(float volume)
        {
            _sfxVolume = volume;
        }

        private void ApplyMusicVolume()
        {
            if (musicSource != null && _crossfadeCoroutine == null)
            {
                musicSource.volume = _musicVolume * _masterVolume;
            }
        }

        private void OnPillTaken(int day, PillChoice choice)
        {

            PlayPillTake();
            if (choice == PillChoice.Taken)
            {
                PlayMusic(onPillMusic);
            }
            else if (choice == PillChoice.NotTaken)
            {
                PlayMusic(offPillMusic);
            }

            Debug.Log($"[AUDIOMANAGER] Pill music changed for Day {day}: {choice}");
        }
        private void OnPhaseChanged(DayPhase phase)
        {
            switch (phase)
            {
                case DayPhase.Night:
                    PlayMusic(nightMusic);
                    break;

                case DayPhase.Morning:
                    break;
            }
        }

        private void OnEndingReached(string ending)
        {
            if (ending == "Bad")
            {
                PlayMusic(badEndingMusic, fade: true);
            }
            else if (ending == "Good")
            {
                PlayMusic(goodEndingMusic, fade: true);
            }

            Debug.Log($"[AUDIOMANAGER] Playing {ending} ending music");
        }

        private void OnDestroy()
        {
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.OnMasterVolumeChanged -= OnMasterVolumeChanged;
                SettingsManager.Instance.OnMusicVolumeChanged -= OnMusicVolumeChanged;
                SettingsManager.Instance.OnSFXVolumeChanged -= OnSFXVolumeChanged;
            }

            if (PillStateManager.Instance != null)
            {
                PillStateManager.Instance.OnPillTaken -= OnPillTaken;
                PillStateManager.Instance.OnEndingReached -= OnEndingReached;
            }

            if (DayManager.Instance != null)
            {
                DayManager.Instance.OnPhaseChanged -= OnPhaseChanged;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnInitializationComplete -= SubscribeToEvents;
            }
        }
    }
}