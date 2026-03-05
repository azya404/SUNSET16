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
SoftenAmbient/RestoreAmbient called by MirrorInteraction on proximity
FadeOutAndPauseAmbient/ResumeAmbientWithFadeIn called during pill sequence
volume tracks masterVolume only - can add dedicated ambient slider later

mirrorSource plays the mirror overlay audio and fades out on button click
pillSFXSource plays the pill choice SFX with independent fade in/out control

subscribes to events in Start or defers to OnInitializationComplete
if GameManager hasnt finished yet (same pattern as other system controllers)
OnDestroy unsubs from everything cos singletons can outlive what they sub to

TODO: morning music should change based on pill choice
TODO: audio ducking (lower music when sfx plays)
TODO: per-room ambient clip dictionary instead of single bedroomAmbientClip
*/
using System;
using UnityEngine;
using System.Collections;
using SUNSET16.UI;

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
        [SerializeField] private AudioSource pillSFXSource;   //pill choice SFX - independent fade in/out

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

        [Header("Ambient Fade Settings")]
        [SerializeField] private float ambientFadeOutDuration = 0.8f; //bedroom fade-out before overlay pauses it
        [SerializeField] private float ambientFadeInDuration  = 1.5f; //bedroom fade-in when overlay closes

        [Header("Temp")]
        [SerializeField] private DOLOSAnnouncement announcement;

        //local volume caches - updated when SettingsManager fires events
        private float _masterVolume = 1.0f;
        private float _musicVolume  = 1.0f;
        private float _sfxVolume    = 1.0f;

        private Coroutine _crossfadeCoroutine;   //reference to running crossfade so we can cancel it
        private Coroutine _ambientFadeCoroutine; //reference to ambient fade so new fades cancel old ones
        private Coroutine _mirrorFadeCoroutine;  //reference to mirror fade so fade-in and fade-out dont clash
        private Coroutine _pillSFXFadeCoroutine; //reference to pill SFX fade

        private bool _ambientPaused = false; //tracks whether ambient was paused by an interaction

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
            SettingsManager.Instance.OnMusicVolumeChanged  += OnMusicVolumeChanged;
            SettingsManager.Instance.OnSFXVolumeChanged    += OnSFXVolumeChanged;

            PillStateManager.Instance.OnPillTaken    += OnPillTaken;
            PillStateManager.Instance.OnEndingReached += OnEndingReached;

            DayManager.Instance.OnPhaseChanged += OnPhaseChanged;

            RoomManager.Instance.OnRoomLoaded += OnRoomLoaded;

            _masterVolume = SettingsManager.Instance.MasterVolume;
            _musicVolume  = SettingsManager.Instance.MusicVolume;
            _sfxVolume    = SettingsManager.Instance.SFXVolume;
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
                musicSource.loop        = true;
                musicSource.playOnAwake = false;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop        = false;
                sfxSource.playOnAwake = false;
            }

            if (ambientSource == null)
            {
                ambientSource = gameObject.AddComponent<AudioSource>();
                ambientSource.loop        = true;
                ambientSource.playOnAwake = false;
            }

            if (mirrorSource == null)
            {
                mirrorSource = gameObject.AddComponent<AudioSource>();
                mirrorSource.loop        = true;
                mirrorSource.playOnAwake = false;
            }

            if (pillSFXSource == null)
            {
                pillSFXSource = gameObject.AddComponent<AudioSource>();
                pillSFXSource.loop        = false;
                pillSFXSource.playOnAwake = false;
                pillSFXSource.volume      = 0f;
            }
        }

        // ─── MUSIC ────────────────────────────────────────────────────────────────

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
        private IEnumerator CrossfadeMusic(AudioClip newClip)
        {
            float timer       = 0;
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

            musicSource.volume  = targetVolume;
            _crossfadeCoroutine = null;
        }

        // ─── SFX ──────────────────────────────────────────────────────────────────

        //plays a one-shot SFX - allows overlapping sounds
        public void PlaySFX(AudioClip clip, float volumeScale = 1.0f)
        {
            if (clip == null)
            {
                Debug.LogWarning("[AUDIOMANAGER] Attempted to play null SFX clip");
                return;
            }
            sfxSource.PlayOneShot(clip, volumeScale * _sfxVolume * _masterVolume);
        }

        public void PlayDoorOpen()    => PlaySFX(doorOpen);
        public void PlayDoorClose()   => PlaySFX(doorClose);
        public void PlayPillTake()    => PlaySFX(pillTake);
        public void PlayTaskComplete()=> PlaySFX(taskComplete);
        public void PlayUIClick()     => PlaySFX(uiClick);
        public void PlayFootstep()    => PlaySFX(footstep, 0.5f);

        // ─── PILL SFX (independent fade control) ─────────────────────────────────

        //plays the pill choice SFX at volume 0 ready for FadePillSFXIn
        public void PlayPillSFX(AudioClip clip)
        {
            if (clip == null || pillSFXSource == null) return;
            pillSFXSource.clip   = clip;
            pillSFXSource.volume = 0f;
            pillSFXSource.Play();
        }

        //fades pill SFX from 0 to full volume - fire and forget
        public void FadePillSFXIn(float duration)
        {
            if (_pillSFXFadeCoroutine != null) StopCoroutine(_pillSFXFadeCoroutine);
            _pillSFXFadeCoroutine = StartCoroutine(FadePillSFXCoroutine(_sfxVolume * _masterVolume, duration));
        }

        //fades pill SFX to 0 - fire and forget
        public void FadePillSFXOut(float duration)
        {
            if (_pillSFXFadeCoroutine != null) StopCoroutine(_pillSFXFadeCoroutine);
            _pillSFXFadeCoroutine = StartCoroutine(FadePillSFXCoroutine(0f, duration));
        }

        private IEnumerator FadePillSFXCoroutine(float targetVolume, float duration)
        {
            float startVolume = pillSFXSource != null ? pillSFXSource.volume : 0f;
            float timer       = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                if (pillSFXSource != null)
                    pillSFXSource.volume = Mathf.Lerp(startVolume, targetVolume, timer / duration);
                yield return null;
            }
            if (pillSFXSource != null) pillSFXSource.volume = targetVolume;
            _pillSFXFadeCoroutine = null;
        }

        // ─── AMBIENT (bedroom) ────────────────────────────────────────────────────

        //called by RoomManager.OnRoomLoaded — starts the right ambient for each room
        private void OnRoomLoaded(string roomName)
        {
            StopAmbient();
            if (roomName.Contains("Bedroom") && bedroomAmbientClip != null)
            {
                PlayAmbient(bedroomAmbientClip);
                Debug.Log("[AUDIOMANAGER] Bedroom ambient started");
            }

            // TEMP - REMOVE LATER
            if (roomName.Contains("Bedroom"))
                DOLOSManager.Instance.TriggerAnnouncement(announcement);
        }

        //starts an ambient loop at full volume
        public void PlayAmbient(AudioClip clip)
        {
            if (clip == null)
            {
                Debug.LogWarning("[AUDIOMANAGER] Attempted to play null ambient clip");
                return;
            }
            ambientSource.clip   = clip;
            ambientSource.volume = _masterVolume;
            ambientSource.Play();
            _ambientPaused = false;
        }

        //fades ambient down to a partial volume when player is near an interactable
        //called by MirrorInteraction.OnTriggerEnter2D
        public void SoftenAmbient(float targetVolume, float duration)
        {
            if (_ambientFadeCoroutine != null) StopCoroutine(_ambientFadeCoroutine);
            _ambientFadeCoroutine = StartCoroutine(FadeAmbientCoroutine(targetVolume, duration, null));
        }

        //restores ambient to full master volume when player leaves proximity
        //called by MirrorInteraction.OnTriggerExit2D
        public void RestoreAmbient(float duration)
        {
            if (_ambientFadeCoroutine != null) StopCoroutine(_ambientFadeCoroutine);
            _ambientFadeCoroutine = StartCoroutine(FadeAmbientCoroutine(_masterVolume, duration, null));
        }

        //fades ambient to 0 then pauses it - called when mirror overlay opens
        public void FadeOutAndPauseAmbient()
        {
            if (_ambientFadeCoroutine != null) StopCoroutine(_ambientFadeCoroutine);
            _ambientFadeCoroutine = StartCoroutine(FadeAmbientCoroutine(0f, ambientFadeOutDuration, () =>
            {
                if (ambientSource != null) ambientSource.Pause();
                _ambientPaused = true;
                _ambientFadeCoroutine = null;
                Debug.Log("[AUDIOMANAGER] Ambient faded out and paused");
            }));
        }

        //unpauses ambient at volume 0 then fades it back in - called when overlay closes
        public void ResumeAmbientWithFadeIn()
        {
            if (ambientSource == null || !_ambientPaused) return;
            ambientSource.volume = 0f;
            ambientSource.UnPause();
            _ambientPaused = false;
            if (_ambientFadeCoroutine != null) StopCoroutine(_ambientFadeCoroutine);
            _ambientFadeCoroutine = StartCoroutine(FadeAmbientCoroutine(_masterVolume, ambientFadeInDuration, () =>
            {
                _ambientFadeCoroutine = null;
                Debug.Log("[AUDIOMANAGER] Ambient resumed and faded in");
            }));
        }

        //pauses ambient immediately (no fade) - kept for non-mirror contexts
        public void PauseAmbient()
        {
            if (ambientSource != null && ambientSource.isPlaying)
            {
                ambientSource.Pause();
                _ambientPaused = true;
                Debug.Log("[AUDIOMANAGER] Ambient paused");
            }
        }

        //resumes ambient immediately (no fade) - kept for non-mirror contexts
        public void ResumeAmbient()
        {
            if (ambientSource != null && _ambientPaused)
            {
                ambientSource.UnPause();
                _ambientPaused = false;
                Debug.Log("[AUDIOMANAGER] Ambient resumed");
            }
        }

        //stops ambient completely - next PlayAmbient restarts from the beginning
        public void StopAmbient()
        {
            if (_ambientFadeCoroutine != null) StopCoroutine(_ambientFadeCoroutine);
            if (ambientSource != null)
            {
                ambientSource.Stop();
                _ambientPaused = false;
            }
        }

        private IEnumerator FadeAmbientCoroutine(float targetVolume, float duration, Action onComplete)
        {
            float startVolume = ambientSource != null ? ambientSource.volume : 0f;
            float timer       = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                if (ambientSource != null)
                    ambientSource.volume = Mathf.Lerp(startVolume, targetVolume, timer / duration);
                yield return null;
            }
            if (ambientSource != null) ambientSource.volume = targetVolume;
            onComplete?.Invoke();
        }

        // ─── MIRROR AMBIENT ───────────────────────────────────────────────────────

        //starts mirror ambient at 0 and fades it in - called when overlay opens
        public void FadeMirrorAmbientIn(float duration)
        {
            if (mirrorAmbientClip == null)
            {
                Debug.LogWarning("[AUDIOMANAGER] mirrorAmbientClip not assigned");
                return;
            }
            if (_mirrorFadeCoroutine != null) StopCoroutine(_mirrorFadeCoroutine);
            mirrorSource.clip   = mirrorAmbientClip;
            mirrorSource.volume = 0f;
            mirrorSource.Play();
            _mirrorFadeCoroutine = StartCoroutine(FadeMirrorCoroutine(_masterVolume, duration, false));
        }

        //fades mirror ambient to 0 then stops - called on button click (fire and forget)
        public void FadeMirrorAmbientOut(float duration)
        {
            if (mirrorSource == null || !mirrorSource.isPlaying) return;
            if (_mirrorFadeCoroutine != null) StopCoroutine(_mirrorFadeCoroutine);
            _mirrorFadeCoroutine = StartCoroutine(FadeMirrorCoroutine(0f, duration, true));
        }

        //immediate stop - safety net called by CloseOverlay
        public void StopMirrorAmbient()
        {
            if (_mirrorFadeCoroutine != null) StopCoroutine(_mirrorFadeCoroutine);
            if (mirrorSource != null)
            {
                mirrorSource.Stop();
                mirrorSource.volume = _masterVolume;
            }
        }

        private IEnumerator FadeMirrorCoroutine(float targetVolume, float duration, bool stopOnComplete)
        {
            float startVolume = mirrorSource.volume;
            float timer       = 0f;
            while (timer < duration)
            {
                timer              += Time.deltaTime;
                mirrorSource.volume = Mathf.Lerp(startVolume, targetVolume, timer / duration);
                yield return null;
            }
            mirrorSource.volume = targetVolume;
            if (stopOnComplete)
            {
                mirrorSource.Stop();
                mirrorSource.volume = _masterVolume; //reset for next use
            }
            _mirrorFadeCoroutine = null;
        }

        // ─── VOLUME APPLICATION ───────────────────────────────────────────────────

        private void ApplyMusicVolume()
        {
            if (musicSource != null && _crossfadeCoroutine == null)
                musicSource.volume = _musicVolume * _masterVolume;
        }

        private void ApplyAmbientVolume()
        {
            if (ambientSource != null && !_ambientPaused)
                ambientSource.volume = _masterVolume;
        }

        // ─── EVENT HANDLERS ───────────────────────────────────────────────────────

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

        //fires when the player makes their pill choice
        //MirrorInteraction plays the choice-specific SFX directly via PlayPillSFX
        //pillTake here is for any future non-mirror pill context
        private void OnPillTaken(int day, PillChoice choice)
        {
            if (choice == PillChoice.Taken)
                PlayMusic(onPillMusic);
            else if (choice == PillChoice.NotTaken)
                PlayMusic(offPillMusic);

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
                    break; //morning music handled by OnPillTaken
            }
        }

        private void OnEndingReached(string ending)
        {
            if (ending == "Bad")
                PlayMusic(badEndingMusic, fade: true);
            else if (ending == "Good")
                PlayMusic(goodEndingMusic, fade: true);

            Debug.Log($"[AUDIOMANAGER] Playing {ending} ending music");
        }

        // ─── CLEANUP ──────────────────────────────────────────────────────────────

        private void OnDestroy()
        {
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.OnMasterVolumeChanged -= OnMasterVolumeChanged;
                SettingsManager.Instance.OnMusicVolumeChanged  -= OnMusicVolumeChanged;
                SettingsManager.Instance.OnSFXVolumeChanged    -= OnSFXVolumeChanged;
            }

            if (PillStateManager.Instance != null)
            {
                PillStateManager.Instance.OnPillTaken     -= OnPillTaken;
                PillStateManager.Instance.OnEndingReached -= OnEndingReached;
            }

            if (DayManager.Instance != null)
                DayManager.Instance.OnPhaseChanged -= OnPhaseChanged;

            if (RoomManager.Instance != null)
                RoomManager.Instance.OnRoomLoaded -= OnRoomLoaded;

            if (GameManager.Instance != null)
                GameManager.Instance.OnInitializationComplete -= SubscribeToEvents;
        }
    }
}