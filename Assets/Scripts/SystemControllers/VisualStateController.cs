using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

namespace SUNSET16.Core
{
    public class VisualStateController : Singleton<VisualStateController>
    {
        [Header("Post-Processing")]
        [SerializeField] private Volume postProcessVolume;
        private ColorAdjustments _colorAdjustments;
        private Vignette _vignette;
        private ChromaticAberration _chromaticAberration;

        [Header("Pill State Effects")]
        [SerializeField] private float onPillSaturation = -50f;
        [SerializeField] private float offPillSaturation = 20f;
        [SerializeField] private float saturationPerRefusal = 10f;
        [SerializeField] private float maxSaturation = 50f;
        [SerializeField] private float transitionSpeed = 1.0f;

        [Header("Vignette Settings")]
        [SerializeField] private float baseVignette = 0.3f;
        [SerializeField] private float vignettePerPill = 0.1f;
        [SerializeField] private float maxVignette = 0.7f;
        [SerializeField] private float offPillVignette = 0.1f;
        [SerializeField] private float vignetteReductionPerRefusal = 0.03f;

        [Header("Glitch Settings")]
        [SerializeField] private float glitchIntensity = 0.5f;
        [SerializeField] private float glitchMinInterval = 3f;
        [SerializeField] private float glitchMaxInterval = 7f;

        private PillChoice _lastChoice = PillChoice.None;
        private Coroutine _glitchCoroutine;
        private Coroutine _transitionCoroutine;

        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsInitialized)
            {
                Initialize();
            }
            else if (GameManager.Instance != null)
            {
                GameManager.Instance.OnInitializationComplete += Initialize;
            }
        }

        private void Initialize()
        {
            InitializePostProcessing();
            PillStateManager.Instance.OnPillTaken += OnPillTaken;
            PillStateManager.Instance.OnEndingReached += OnEndingReached;
            SettingsManager.Instance.OnBrightnessChanged += ApplyBrightness;
            SaveManager.Instance.OnSaveDeleted += ResetVisuals;
            SaveManager.Instance.OnGameLoaded += OnGameLoaded;
            ApplyBrightness(SettingsManager.Instance.Brightness);

            Debug.Log("[VISUALSTATECONTROLLER] Initialized");
        }

        private void InitializePostProcessing()
        {
            if (postProcessVolume == null)
            {
                postProcessVolume = FindObjectOfType<Volume>();
                if (postProcessVolume == null)
                {
                    Debug.LogWarning("[VISUALSTATECONTROLLER] No Volume found - visual effects disabled");
                    return;
                }
            }

            if (postProcessVolume.profile.TryGet(out _colorAdjustments))
            {
                Debug.Log("[VISUALSTATECONTROLLER] ColorAdjustments found");
            }
            else
            {
                Debug.LogWarning("[VISUALSTATECONTROLLER] ColorAdjustments not found in Volume Profile");
            }

            postProcessVolume.profile.TryGet(out _vignette);
            postProcessVolume.profile.TryGet(out _chromaticAberration);
        }

        private void OnPillTaken(int day, PillChoice choice)
        {
            _lastChoice = choice;
            UpdateVisuals(instant: false);

            if (choice == PillChoice.NotTaken)
            {
                StartGlitchEffect();
            }
            else if (choice == PillChoice.Taken)
            {
                StopGlitchEffect();
            }

            Debug.Log($"[VISUALSTATECONTROLLER] Visuals updated for Day {day}: {choice}");
        }

        private void OnEndingReached(string ending)
        {
            StopGlitchEffect();

            if (ending == "Bad")
            {
                SetCustomEffect(-100f, 0.8f);
            }
            else if (ending == "Good")
            {
                SetCustomEffect(50f, 0f);
            }

            Debug.Log($"[VISUALSTATECONTROLLER] Ending visuals applied: {ending}");
        }

        private void UpdateVisuals(bool instant)
        {
            if (_colorAdjustments == null || _vignette == null) return;

            int pillsTaken = PillStateManager.Instance.GetPillsTakenCount();
            int pillsRefused = PillStateManager.Instance.GetPillsRefusedCount();

            float targetSaturation;
            float targetVignette;

            if (_lastChoice == PillChoice.Taken)
            {
                targetSaturation = onPillSaturation;
                targetVignette = Mathf.Clamp(baseVignette + (pillsTaken * vignettePerPill), 0f, maxVignette);
            }
            else
            {
                targetSaturation = Mathf.Clamp(offPillSaturation + (pillsRefused * saturationPerRefusal), 0f, maxSaturation);
                targetVignette = Mathf.Clamp(offPillVignette - (pillsRefused * vignetteReductionPerRefusal), 0f, offPillVignette);
            }

            if (instant)
            {
                _colorAdjustments.saturation.value = targetSaturation;
                _vignette.intensity.value = targetVignette;
            }
            else
            {
                if (_transitionCoroutine != null)
                    StopCoroutine(_transitionCoroutine);
                _transitionCoroutine = StartCoroutine(TransitionVisuals(targetSaturation, targetVignette));
            }
        }

        private IEnumerator TransitionVisuals(float targetSaturation, float targetVignette)
        {
            float startSaturation = _colorAdjustments.saturation.value;
            float startVignette = _vignette.intensity.value;
            float timer = 0;
            float duration = 2.0f / transitionSpeed;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = timer / duration;

                _colorAdjustments.saturation.value = Mathf.Lerp(startSaturation, targetSaturation, t);
                _vignette.intensity.value = Mathf.Lerp(startVignette, targetVignette, t);

                yield return null;
            }

            _colorAdjustments.saturation.value = targetSaturation;
            _vignette.intensity.value = targetVignette;
            _transitionCoroutine = null;
        }

        private void StartGlitchEffect()
        {
            if (_chromaticAberration == null) return;

            if (_glitchCoroutine != null)
                StopCoroutine(_glitchCoroutine);

            _glitchCoroutine = StartCoroutine(GlitchRoutine());
        }

        private void StopGlitchEffect()
        {
            if (_glitchCoroutine != null)
            {
                StopCoroutine(_glitchCoroutine);
                _glitchCoroutine = null;
            }

            if (_chromaticAberration != null)
            {
                _chromaticAberration.intensity.value = 0;
            }
        }

        private IEnumerator GlitchRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(glitchMinInterval, glitchMaxInterval));
                _chromaticAberration.intensity.value = glitchIntensity;
                yield return new WaitForSeconds(0.1f);
                _chromaticAberration.intensity.value = 0;
                yield return new WaitForSeconds(0.05f);
                _chromaticAberration.intensity.value = glitchIntensity * 0.5f;
                yield return new WaitForSeconds(0.1f);
                _chromaticAberration.intensity.value = 0;
            }
        }

        private void ApplyBrightness(float brightness)
        {
            if (_colorAdjustments == null) return;
            _colorAdjustments.postExposure.value = Mathf.Lerp(-1f, 1f, brightness);
        }

        public void SetCustomEffect(float saturation, float vignetteIntensity)
        {
            if (_colorAdjustments != null)
                _colorAdjustments.saturation.value = saturation;

            if (_vignette != null)
                _vignette.intensity.value = vignetteIntensity;
        }

        private void OnGameLoaded()
        {
            // Stop any running animations before applying loaded state
            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
                _transitionCoroutine = null;
            }
            if (_glitchCoroutine != null)
            {
                StopCoroutine(_glitchCoroutine);
                _glitchCoroutine = null;
            }

            int currentDay = DayManager.Instance.CurrentDay;
            PillChoice lastKnownChoice = PillChoice.None;

            for (int d = currentDay; d >= 1; d--)
            {
                PillChoice c = PillStateManager.Instance.GetPillChoice(d);
                if (c != PillChoice.None)
                {
                    lastKnownChoice = c;
                    break;
                }
            }

            _lastChoice = lastKnownChoice;

            if (PillStateManager.Instance.IsEndingReached)
            {
                OnEndingReached(PillStateManager.Instance.DetermineEnding());
            }
            else if (_lastChoice != PillChoice.None)
            {
                UpdateVisuals(instant: true);

                if (_lastChoice == PillChoice.NotTaken)
                    StartGlitchEffect();
                else
                    StopGlitchEffect();
            }
            else
            {
                ResetVisuals();
            }

            Debug.Log($"[VISUALSTATECONTROLLER] Visuals re-applied after load (lastChoice: {_lastChoice})");
        }

        public void ResetVisuals()
        {
            _lastChoice = PillChoice.None;
            StopGlitchEffect();
            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
                _transitionCoroutine = null;
            }

            if (_colorAdjustments != null)
            {
                _colorAdjustments.saturation.value = 0f;
                _colorAdjustments.postExposure.value = Mathf.Lerp(-1f, 1f, SettingsManager.Instance.Brightness);
            }

            if (_vignette != null)
            {
                _vignette.intensity.value = 0f;
            }

            if (_chromaticAberration != null)
            {
                _chromaticAberration.intensity.value = 0f;
            }

            Debug.Log("[VISUALSTATECONTROLLER] Visuals reset to defaults");
        }

        private void OnDestroy()
        {
            if (PillStateManager.Instance != null)
            {
                PillStateManager.Instance.OnPillTaken -= OnPillTaken;
                PillStateManager.Instance.OnEndingReached -= OnEndingReached;
            }

            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.OnBrightnessChanged -= ApplyBrightness;
            }

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.OnSaveDeleted -= ResetVisuals;
                SaveManager.Instance.OnGameLoaded -= OnGameLoaded;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnInitializationComplete -= Initialize;
            }
        }
    }
}