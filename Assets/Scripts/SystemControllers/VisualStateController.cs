/*
controls post-processing to visually show the pill state
probably the most narratively important system cos it SHOWS the player
what the pill does without telling them

on-pill: world gets desaturated and gray, vignette closes in (tunnel vision)
the more pills you take the worse it gets - by ending its near grayscale
with heavy vignette, feels claustrophobic and lifeless

off-pill: colors get MORE vibrant, vignette opens up, but you get these
chromatic aberration glitches randomly (represents withdrawal/awakening)
the more you refuse the more colorful it gets until the good ending
which is full vivid color with no vignette at all

uses URPs Volume component with ColorAdjustments, Vignette, and
ChromaticAberration overrides. the glitch is a coroutine that fires
chromatic bursts at random intervals (3-7 seconds) in a
full -> off -> half -> off pattern that looks like a camera malfunction

the progressive system is key - effects accumulate across days
not just the current day. 2 pills = more gray than 1 pill etc
makes the visual difference between paths more dramatic over time

TODO: color grading LUT swap for more dramatic visual difference
TODO: screen distortion for ending sequences
TODO: brightness goes through postExposure here - might want to separate
*/
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
            //subscribe to pill events, settings, and save/load
            PillStateManager.Instance.OnPillTaken += OnPillTaken;
            PillStateManager.Instance.OnEndingReached += OnEndingReached;
            SettingsManager.Instance.OnBrightnessChanged += ApplyBrightness;
            SaveManager.Instance.OnSaveDeleted += ResetVisuals;
            SaveManager.Instance.OnGameLoaded += OnGameLoaded;
            ApplyBrightness(SettingsManager.Instance.Brightness); //apply current brightness immediately

            Debug.Log("[VISUALSTATECONTROLLER] Initialized");
        }

        //grab the post-processing overrides from the Volume profile
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

            //TryGet pulls the override from the volume profile if it exists
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

        //fires when the player makes their pill choice
        private void OnPillTaken(int day, PillChoice choice)
        {
            _lastChoice = choice;
            UpdateVisuals(instant: false); //smooth transition to new visual state

            //off-pill = start the glitch effect, on-pill = stop it
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

        //ending reached - slam to extreme values
        private void OnEndingReached(string ending)
        {
            StopGlitchEffect(); //no more random glitches during ending

            if (ending == "Bad")
            {
                SetCustomEffect(-100f, 0.8f); //full grayscale + heavy vignette = oppressive
            }
            else if (ending == "Good")
            {
                SetCustomEffect(50f, 0f); //full vibrant color + no vignette = free
            }

            Debug.Log($"[VISUALSTATECONTROLLER] Ending visuals applied: {ending}");
        }

        //calculates target saturation and vignette based on pill history
        //on-pill = more pills = more gray + more vignette
        //off-pill = more refusals = more vibrant + less vignette
        private void UpdateVisuals(bool instant)
        {
            if (_colorAdjustments == null || _vignette == null) return;

            int pillsTaken = PillStateManager.Instance.GetPillsTakenCount();
            int pillsRefused = PillStateManager.Instance.GetPillsRefusedCount();

            float targetSaturation;
            float targetVignette;

            if (_lastChoice == PillChoice.Taken)
            {
                targetSaturation = onPillSaturation; //base gray
                targetVignette = Mathf.Clamp(baseVignette + (pillsTaken * vignettePerPill), 0f, maxVignette); //progressively worse
            }
            else
            {
                targetSaturation = Mathf.Clamp(offPillSaturation + (pillsRefused * saturationPerRefusal), 0f, maxSaturation); //progressively more vibrant
                targetVignette = Mathf.Clamp(offPillVignette - (pillsRefused * vignetteReductionPerRefusal), 0f, offPillVignette); //progressively clearer
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

        //the glitch: chromatic aberration bursts that look like camera malfunction
        //full -> off -> half -> off pattern at random intervals
        private IEnumerator GlitchRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(glitchMinInterval, glitchMaxInterval));
                _chromaticAberration.intensity.value = glitchIntensity; //BURST
                yield return new WaitForSeconds(0.1f);
                _chromaticAberration.intensity.value = 0; //off
                yield return new WaitForSeconds(0.05f);
                _chromaticAberration.intensity.value = glitchIntensity * 0.5f; //half burst
                yield return new WaitForSeconds(0.1f);
                _chromaticAberration.intensity.value = 0; //off again
            }
        }

        //brightness slider maps 0-1 to -1 to +1 exposure
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

        //loaded a save - need to figure out what visual state we should be in
        //walks backwards through days to find the last pill choice
        private void OnGameLoaded()
        {
            //kill any running animations first
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

            //walk backwards through days to find the most recent pill choice
            int currentDay = DayManager.Instance.CurrentDay;
            PillChoice lastKnownChoice = PillChoice.None;

            for (int d = currentDay; d >= 1; d--)
            {
                PillChoice c = PillStateManager.Instance.GetPillChoice(d);
                if (c != PillChoice.None)
                {
                    lastKnownChoice = c;
                    break; //found it, stop looking
                }
            }

            _lastChoice = lastKnownChoice;

            //apply the appropriate visual state based on what we found
            if (PillStateManager.Instance.IsEndingReached)
            {
                OnEndingReached(PillStateManager.Instance.DetermineEnding());
            }
            else if (_lastChoice != PillChoice.None)
            {
                UpdateVisuals(instant: true); //snap to correct state, no smooth transition on load

                if (_lastChoice == PillChoice.NotTaken)
                    StartGlitchEffect(); //was off-pill, bring the glitches back
                else
                    StopGlitchEffect();
            }
            else
            {
                ResetVisuals(); //no pill history = clean slate
            }

            Debug.Log($"[VISUALSTATECONTROLLER] Visuals re-applied after load (lastChoice: {_lastChoice})");
        }

        //back to factory settings - zero everything out
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
                _colorAdjustments.saturation.value = 0f; //normal color
                _colorAdjustments.postExposure.value = Mathf.Lerp(-1f, 1f, SettingsManager.Instance.Brightness);
            }

            if (_vignette != null)
            {
                _vignette.intensity.value = 0f; //no vignette
            }

            if (_chromaticAberration != null)
            {
                _chromaticAberration.intensity.value = 0f; //no glitch
            }

            Debug.Log("[VISUALSTATECONTROLLER] Visuals reset to defaults");
        }

        //unsub from everything
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