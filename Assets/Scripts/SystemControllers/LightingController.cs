/*
controls the global lighting based on time of day (morning vs night)
uses URPs Light2D component - the globalLight is set to "Global" type
so it affects every sprite and tilemap in the scene

morning = warm bright lights, night = cool dim blue tint
transitions between them smoothly with a coroutine that lerps
color and intensity over transitionDuration seconds

also snaps lighting instantly on init and save/load so you dont
get a weird transition when the game first starts or when loading
a save thats mid-night

same event subscription pattern as AudioManager - defers until
GameManager finishes init then subscribes to phase changes

TODO: per-room lighting presets (different ambient for bedroom vs hallway vs hidden rooms)
TODO: flickering light effect for hidden rooms (creepy atmosphere)
TODO: pill state should affect lighting (on-pill = sterile, off-pill = warmer)
*/
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

namespace SUNSET16.Core
{
    public class LightingController : Singleton<LightingController>
    {
        [Header("Lighting Presets")]
        [SerializeField] private Color dayAmbientColor = new Color(1f, 0.95f, 0.9f);
        [SerializeField] private Color nightAmbientColor = new Color(0.3f, 0.4f, 0.6f);
        [SerializeField] private float dayIntensity = 1.0f;
        [SerializeField] private float nightIntensity = 0.4f;

        [Header("Global Light")]
        [SerializeField] private Light2D globalLight;

        [Header("Transition Settings")]
        [SerializeField] private float transitionDuration = 2.0f;

        private Coroutine _transitionCoroutine;

        protected override void Awake()
        {
            base.Awake();
        }

        //same deferred init pattern as AudioManager
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
            //try to find a Light2D in the scene if one isnt assigned
            if (globalLight == null)
            {
                globalLight = FindObjectOfType<Light2D>();
                if (globalLight == null)
                {
                    Debug.LogWarning("[LIGHTINGCONTROLLER] No Global Light 2D found - lighting transitions disabled");
                    return;
                }
            }

            //hook into phase changes + save/load events
            DayManager.Instance.OnPhaseChanged += OnPhaseChanged;
            SaveManager.Instance.OnSaveDeleted += OnSaveDeleted;
            SaveManager.Instance.OnGameLoaded += OnGameLoaded;

            //snap to the correct lighting state immediately (no transition on startup)
            if (DayManager.Instance.CurrentPhase == DayPhase.Morning)
            {
                ApplyDayLighting(instant: true);
            }
            else
            {
                ApplyNightLighting(instant: true);
            }

            Debug.Log("[LIGHTINGCONTROLLER] Initialized");
        }

        //save got deleted = fresh game, snap back to morning defaults
        private void OnSaveDeleted()
        {
            ApplyDayLighting(instant: true);
            Debug.Log("[LIGHTINGCONTROLLER] Lighting reset to Morning defaults");
        }

        //loaded a save - snap to whatever phase we loaded into without transition
                private void OnGameLoaded()
        {
            if (DayManager.Instance.CurrentPhase == DayPhase.Morning)
            {
                ApplyDayLighting(instant: true);
            }
            else
            {
                ApplyNightLighting(instant: true);
            }
            Debug.Log($"[LIGHTINGCONTROLLER] Lighting re-applied after load ({DayManager.Instance.CurrentPhase})");
        }

        //phase changed during gameplay - smooth transition so it looks nice
        private void OnPhaseChanged(DayPhase newPhase)
        {
            switch (newPhase)
            {
                case DayPhase.Morning:
                    ApplyDayLighting(instant: false); //smooth fade to warm
                    break;

                case DayPhase.Night:
                    ApplyNightLighting(instant: false); //smooth fade to cool blue
                    break;
            }
        }

        private void ApplyDayLighting(bool instant)
        {
            if (globalLight == null) return;

            if (instant)
            {
                globalLight.color = dayAmbientColor;
                globalLight.intensity = dayIntensity;
            }
            else
            {
                StartTransition(dayAmbientColor, dayIntensity);
            }

            Debug.Log($"[LIGHTINGCONTROLLER] Day lighting applied (instant: {instant})");
        }

        private void ApplyNightLighting(bool instant)
        {
            if (globalLight == null) return;

            if (instant)
            {
                globalLight.color = nightAmbientColor;
                globalLight.intensity = nightIntensity;
            }
            else
            {
                StartTransition(nightAmbientColor, nightIntensity);
            }

            Debug.Log($"[LIGHTINGCONTROLLER] Night lighting applied (instant: {instant})");
        }

        //kills any running transition and starts a new one
        //only one can run at a time or theyd fight each other
        private void StartTransition(Color targetColor, float targetIntensity)
        {
            if (_transitionCoroutine != null)
                StopCoroutine(_transitionCoroutine);

            _transitionCoroutine = StartCoroutine(TransitionLighting(targetColor, targetIntensity));
        }

        //lerps both color and intensity over transitionDuration
        private IEnumerator TransitionLighting(Color targetColor, float targetIntensity)
        {
            Color startColor = globalLight.color;
            float startIntensity = globalLight.intensity;
            float timer = 0;

            while (timer < transitionDuration)
            {
                timer += Time.deltaTime;
                float t = timer / transitionDuration;

                globalLight.color = Color.Lerp(startColor, targetColor, t);
                globalLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, t);

                yield return null;
            }

            //snap to exact values at the end so theres no floating point drift
            globalLight.color = targetColor;
            globalLight.intensity = targetIntensity;

            _transitionCoroutine = null;
        }

        //these two are for other scripts to override lighting (like for endings)
        public void SetCustomLighting(Color color, float intensity)
        {
            if (globalLight == null) return;
            StartTransition(color, intensity); //smooth version
        }

        public void SetCustomLightingInstant(Color color, float intensity)
        {
            if (globalLight == null) return;
            globalLight.color = color; //snap version
            globalLight.intensity = intensity;
        }

        //unsub from everything so we dont get ghost callbacks
        private void OnDestroy()
        {
            if (DayManager.Instance != null)
            {
                DayManager.Instance.OnPhaseChanged -= OnPhaseChanged;
            }

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.OnSaveDeleted -= OnSaveDeleted;
                SaveManager.Instance.OnGameLoaded -= OnGameLoaded;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnInitializationComplete -= Initialize;
            }
        }
    }
}