/*
handles all the audio - music tracks, sound effects, and ambient loops
listens to events from PillStateManager, DayManager, SettingsManager, RoomManager
and reacts by switching music, adjusting volume, or swapping ambient

music crossfades between tracks using a coroutine so theres no jarring
hard cuts when the pill state changes or night hits
on-pill music is dull and drowsy, off-pill is tense and alert

sfx uses PlayOneShot so multiple sounds can overlap without cutting
each other off - theres convenience methods (PlayDoorOpen, PlayPillTake, etc)
so other scripts dont have to manage clips themselves

ambient is a separate looping AudioSource independent of music
starts automatically when BedroomScene loads (via OnRoomLoaded event)
PauseAmbient/ResumeAmbient called by MirrorInteraction during pill sequence
volume tracks masterVolume only - can add dedicated ambient slider later

subscribes to events in Start or defers to OnInitializationComplete
if GameManager hasnt finished yet (same pattern as other system controllers)
OnDestroy unsubs from everything cos singletons can outlive what they sub to

TODO: most AudioClip fields are empty rn - need actual audio assets
TODO: morning music should change based on pill choice
TODO: audio ducking (lower music when sfx plays)
TODO: per-room ambient clip dictionary instead of single bedroomAmbientClip
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
        [SerializeField] private AudioSource ambientSource;   //looping ambient (Albert's theme, etc)
        [SerializeField] private AudioSource mirrorSource;    //mirror overlay audio - fades out on button click

        [Header("Ambient")]
        [SerializeField] private AudioClip bedroomAmbientClip; //ambient loop for BedroomScene (Albert's theme)
        [SerializeField] private AudioClip mirrorAmbientClip;  //plays while mirror overlay is open

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
        private bool _ambientPaused = false;   //tracks whether ambient was paused by an interaction

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

            RoomManager.Instance.OnRoomLoaded += OnRoomLoaded;

            _masterVolume = SettingsManager.Instance.MasterVolume;
            _musicVolume = SettingsManager.Instance.MusicVolume;
            _sfxVolume = SettingsManager.Instance.SFXVolume;
            ApplyMusicVolume();
            ApplyAmbientVolume();

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

            if (ambientSource == null)
            {
                ambientSource = gameObject.AddComponent<AudioSource>();
                ambientSource.loop = true;          //ambient loops forever
                ambientSource.playOnAwake = false;
            }

            if (mirrorSource == null)
            {
                mirrorSource = gameObject.AddComponent<AudioSource>();
                mirrorSource.loop = true;           //mirror ambient loops while overlay is open
                mirrorSource.playOnAwake = false;
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
            ApplyAmbientVolume();
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

        private void ApplyAmbientVolume()
        {
            if (ambientSource != null && !_ambientPaused)
            {
                ambientSource.volume = _masterVolume;
            }
        }

        //called by RoomManager.OnRoomLoaded — starts the right ambient for each room
        //stops any current ambient first so rooms dont bleed into each other
        private void OnRoomLoaded(string roomName)
        {
            StopAmbient();

            if (roomName.Contains("Bedroom") && bedroomAmbientClip != null)
            {
                PlayAmbient(bedroomAmbientClip);
                Debug.Log("[AUDIOMANAGER] Bedroom ambient started");
            }
        }

        //starts an ambient loop — replaces whatever was playing
        public void PlayAmbient(AudioClip clip)
        {
            if (clip == null)
            {
                Debug.LogWarning("[AUDIOMANAGER] Attempted to play null ambient clip");
                return;
            }

            ambientSource.clip = clip;
            ambientSource.volume = _masterVolume;
            ambientSource.Play();
            _ambientPaused = false;
        }

        //pauses ambient without losing playback position
        //called by MirrorInteraction (and future: ComputerInteraction, PodInteraction)
        public void PauseAmbient()
        {
            if (ambientSource != null && ambientSource.isPlaying)
            {
                ambientSource.Pause();
                _ambientPaused = true;
                Debug.Log("[AUDIOMANAGER] Ambient paused");
            }
        }

        //resumes ambient from where it was paused
        public void ResumeAmbient()
        {
            if (ambientSource != null && _ambientPaused)
            {
                ambientSource.UnPause();
                _ambientPaused = false;
                Debug.Log("[AUDIOMANAGER] Ambient resumed");
            }
        }

        //stops ambient completely (used when leaving a room)
        //next PlayAmbient call will restart from the beginning
        public void StopAmbient()
        {
            if (ambientSource != null)
            {
                ambientSource.Stop();
                _ambientPaused = false;
            }
        }

        //starts the mirror overlay audio loop
        //called by MirrorInteraction.ShowOverlay
        public void PlayMirrorAmbient()
        {
            if (mirrorAmbientClip == null)
            {
                Debug.LogWarning("[AUDIOMANAGER] mirrorAmbientClip not assigned");
                return;
            }

            mirrorSource.clip   = mirrorAmbientClip;
            mirrorSource.volume = _masterVolume;
            mirrorSource.Play();
        }

        //starts a coroutine to fade mirror audio out over duration
        //called just before the screen fade — runs in parallel, fire-and-forget
        public void FadeMirrorAmbientOut(float duration)
        {
            if (mirrorSource != null && mirrorSource.isPlaying)
                StartCoroutine(FadeMirrorAmbientCoroutine(duration));
        }

        private IEnumerator FadeMirrorAmbientCoroutine(float duration)
        {
            float startVolume = mirrorSource.volume;
            float timer       = 0f;

            while (timer < duration)
            {
                timer            += Time.deltaTime;
                mirrorSource.volume = Mathf.Lerp(startVolume, 0f, timer / duration);
                yield return null;
            }

            mirrorSource.Stop();
            mirrorSource.volume = _masterVolume; //reset volume for next use
        }

        //immediate stop — called by MirrorInteraction.CloseOverlay as a safety net
        //in case the overlay is dismissed before the fade completes
        public void StopMirrorAmbient()
        {
            if (mirrorSource != null)
            {
                mirrorSource.Stop();
                mirrorSource.volume = _masterVolume;
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

            if (RoomManager.Instance != null)
            {
                RoomManager.Instance.OnRoomLoaded -= OnRoomLoaded;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnInitializationComplete -= SubscribeToEvents;
            }
        }
    }
}