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
            if (globalLight == null)
            {
                globalLight = FindObjectOfType<Light2D>();
                if (globalLight == null)
                {
                    Debug.LogWarning("[LIGHTINGCONTROLLER] No Global Light 2D found - lighting transitions disabled");
                    return;
                }
            }

            DayManager.Instance.OnPhaseChanged += OnPhaseChanged;
            SaveManager.Instance.OnSaveDeleted += OnSaveDeleted;
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

        private void OnSaveDeleted()
        {
            ApplyDayLighting(instant: true);
            Debug.Log("[LIGHTINGCONTROLLER] Lighting reset to Morning defaults");
        }

        private void OnPhaseChanged(DayPhase newPhase)
        {
            switch (newPhase)
            {
                case DayPhase.Morning:
                    ApplyDayLighting(instant: false);
                    break;

                case DayPhase.Night:
                    ApplyNightLighting(instant: false);
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

        private void StartTransition(Color targetColor, float targetIntensity)
        {
            if (_transitionCoroutine != null)
                StopCoroutine(_transitionCoroutine);

            _transitionCoroutine = StartCoroutine(TransitionLighting(targetColor, targetIntensity));
        }

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

            globalLight.color = targetColor;
            globalLight.intensity = targetIntensity;

            _transitionCoroutine = null;
        }

        public void SetCustomLighting(Color color, float intensity)
        {
            if (globalLight == null) return;
            StartTransition(color, intensity);
        }

        public void SetCustomLightingInstant(Color color, float intensity)
        {
            if (globalLight == null) return;
            globalLight.color = color;
            globalLight.intensity = intensity;
        }

        private void OnDestroy()
        {
            if (DayManager.Instance != null)
            {
                DayManager.Instance.OnPhaseChanged -= OnPhaseChanged;
            }

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.OnSaveDeleted -= OnSaveDeleted;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnInitializationComplete -= Initialize;
            }
        }
    }
}