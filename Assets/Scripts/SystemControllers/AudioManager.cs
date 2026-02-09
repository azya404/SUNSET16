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
        }

        private void Start()
        {
        }

        private void SubscribeToEvents()
        {
        }

        private void InitializeAudioSources()
        {
        }

        public void PlayMusic(AudioClip clip, bool fade = true)
        {
        }

        private IEnumerator CrossfadeMusic(AudioClip newClip)
        {
            yield break;
        }

        public void PlaySFX(AudioClip clip, float volumeScale = 1.0f)
        {
        }

        public void PlayDoorOpen() { }
        public void PlayDoorClose() { }
        public void PlayPillTake() { }
        public void PlayTaskComplete() { }
        public void PlayUIClick() { }
        public void PlayFootstep() { }

        private void OnMasterVolumeChanged(float volume) { }
        private void OnMusicVolumeChanged(float volume) { }
        private void OnSFXVolumeChanged(float volume) { }

        private void ApplyMusicVolume() { }

        private void OnPillTaken(int day, PillChoice choice) { }
        private void OnPhaseChanged(DayPhase phase) { }
        private void OnEndingReached(string ending) { }

        private void OnDestroy()
        {
        }
    }
}