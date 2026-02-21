/*
handles all the audio - music tracks and sound effects
listens to events from PillStateManager, DayManager, SettingsManager
and reacts by switching music or adjusting volume

music crossfades between tracks using a coroutine so theres no jarring
hard cuts when the pill state changes or night hits
on-pill music is dull and drowsy, off-pill is tense and alert

sfx uses PlayOneShot so multiple sounds can overlap without cutting
each other off - theres convenience methods (PlayDoorOpen, PlayPillTake, etc)
so other scripts dont have to manage clips themselves

subscribes to events in Start or defers to OnInitializationComplete
if GameManager hasnt finished yet (same pattern as other system controllers)
OnDestroy unsubs from everything cos singletons can outlive what they sub to

TODO: most AudioClip fields are empty rn - need actual audio assets
TODO: ambient sound layers (station hum, ventilation, etc)
TODO: morning music should change based on pill choice
TODO: audio ducking (lower music when sfx plays)
*/
using UnityEngine;
using System.Collections;

namespace SUNSET16.Core
{
    public class AudioManager : Singleton<AudioManager>
    {
        //[SerializeField] makes private fields visible in the Unity Inspector
        //designers can drag audio clips here without touching code

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;     //looping music player
        [SerializeField] private AudioSource sfxSource;       //one-shot sound effects player

        [Header("Music Tracks")]
        [SerializeField] private AudioClip onPillMusic;       //dull, atmospheric, drowsy feeling
        [SerializeField] private AudioClip offPillMusic;      //tense, alert, worlds-awakening feeling
        [SerializeField] private AudioClip nightMusic;        //nighttime exploration theme
        [SerializeField] private AudioClip menuMusic;         //main menu theme
        [SerializeField] private AudioClip badEndingMusic;    //somber, hopeless - player became a drone
        [SerializeField] private AudioClip goodEndingMusic;   //triumphant, hopeful - player escapes

        [Header("Sound Effects")]
        [SerializeField] private AudioClip doorOpen;
        [SerializeField] private AudioClip doorClose;
        [SerializeField] private AudioClip pillTake;
        [SerializeField] private AudioClip taskComplete;
        [SerializeField] private AudioClip uiClick;
        [SerializeField] private AudioClip footstep;

        [Header("Crossfade Settings")]
        [SerializeField] private float crossfadeDuration = 1.0f; //how long the fade between tracks takes

        //local volume caches - updated when SettingsManager fires events
        private float _masterVolume = 1.0f;
        private float _musicVolume = 1.0f;
        private float _sfxVolume = 1.0f;

        private Coroutine _crossfadeCoroutine; //reference to running crossfade so we can cancel it

        protected override void Awake()
        {
            base.Awake();
            InitializeAudioSources();
        }

        //Start() runs after all Awake() calls - checks if managers are ready
        //if GameManager is already initialized, subscribe immediately
        //otherwise wait for the OnInitializationComplete event (deferred pattern)
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

        //hook into all the events we care about
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

        //ensures AudioSource components exist even if not assigned in Inspector
        //creates them at runtime if needed (safety fallback)
        private void InitializeAudioSources()
        {
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;        //music loops forever
                musicSource.playOnAwake = false; //dont play until we tell it to
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;         //sfx plays once
                sfxSource.playOnAwake = false;
            }
        }

        //plays a music track, optionally crossfading from the current track
        //if the same clip is already playing, does nothing (prevents restart)
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

        //crossfade coroutine: fade old track out -> swap clips -> fade new track in
        //this avoids the jarring hard-cut between music tracks
        private IEnumerator CrossfadeMusic(AudioClip newClip)
        {
            float timer = 0;
            float startVolume = musicSource.volume;

            //PHASE 1: fade OUT the current track
            while (timer < crossfadeDuration)
            {
                timer += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0, timer / crossfadeDuration);
                yield return null; //wait one frame then continue
            }

            //swap to new clip while volume is at 0 (inaudible transition)
            musicSource.clip = newClip;
            musicSource.Play();

            //PHASE 2: fade IN the new track
            timer = 0;
            float targetVolume = _musicVolume * _masterVolume;
            while (timer < crossfadeDuration)
            {
                timer += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(0, targetVolume, timer / crossfadeDuration);
                yield return null;
            }

            musicSource.volume = targetVolume;
            _crossfadeCoroutine = null; //done, clear the reference
        }

        //plays a one-shot SFX at the given volume (calculated with master + sfx volumes)
        //PlayOneShot is better than Play() for SFX cos it allows overlapping sounds
        public void PlaySFX(AudioClip clip, float volumeScale = 1.0f)
        {
            if (clip == null)
            {
                Debug.LogWarning("[AUDIOMANAGER] Attempted to play null SFX clip");
                return;
            }

            sfxSource.PlayOneShot(clip, volumeScale * _sfxVolume * _masterVolume);
        }

        //convenience methods - other scripts call these instead of managing clips directly
        //this way if we want to change the door sound, we only change it in the Inspector once
        public void PlayDoorOpen() => PlaySFX(doorOpen);
        public void PlayDoorClose() => PlaySFX(doorClose);
        public void PlayPillTake() => PlaySFX(pillTake);
        public void PlayTaskComplete() => PlaySFX(taskComplete);
        public void PlayUIClick() => PlaySFX(uiClick);
        public void PlayFootstep() => PlaySFX(footstep, 0.5f); //footsteps at half volume

        //EVENT HANDLERS - these fire when SettingsManager volume events come in
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

        //fires when the player makes their pill choice
        //immediately plays the pill SFX and switches to the appropriate music
        private void OnPillTaken(int day, PillChoice choice)
        {
            PlayPillTake(); //pill bottle sfx
            if (choice == PillChoice.Taken)
            {
                PlayMusic(onPillMusic); //switch to dull, compliant music
            }
            else if (choice == PillChoice.NotTaken)
            {
                PlayMusic(offPillMusic); //switch to tense, awakening music
            }

            Debug.Log($"[AUDIOMANAGER] Pill music changed for Day {day}: {choice}");
        }

        //fires when the day phase changes
        private void OnPhaseChanged(DayPhase phase)
        {
            switch (phase)
            {
                case DayPhase.Night:
                    PlayMusic(nightMusic);
                    break;

                case DayPhase.Morning:
                    break; //morning music is handled by OnPillTaken (plays after pill choice)
            }
        }

        //fires when the pill threshold is met (3+ of same choice)
        //plays the appropriate ending music
        private void OnEndingReached(string ending)
        {
            if (ending == "Bad")
            {
                PlayMusic(badEndingMusic, fade: true); //somber, hopeless
            }
            else if (ending == "Good")
            {
                PlayMusic(goodEndingMusic, fade: true); //triumphant, hopeful
            }

            Debug.Log($"[AUDIOMANAGER] Playing {ending} ending music");
        }

        //CLEANUP: unsubscribe from ALL events to prevent memory leaks
        //this is critical cos singletons can outlive the objects they subscribe to
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