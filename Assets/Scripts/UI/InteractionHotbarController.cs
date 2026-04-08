/*
persistent hotbar that lives in CoreScene alongside HUDController and DOLOSManager
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using SUNSET16.Core;

namespace SUNSET16.UI
{
    public class InteractionHotbarController : Singleton<InteractionHotbarController>
    {
        [Header("Hotbar Visuals")]
        [SerializeField] private Image characterSprite;

        [Header("Textbox")]
        [SerializeField] private CanvasGroup textboxCanvasGroup;
        [SerializeField] private TMP_Text    promptText;
        [SerializeField] private float       fadeDuration = 0.2f;

        private Coroutine _fadeCoroutine;

        protected override void Awake()
        {
            base.Awake();

            if (characterSprite == null)
                Debug.LogError("[HOTBAR] characterSprite is not assigned in the Inspector.");
            if (textboxCanvasGroup == null)
                Debug.LogError("[HOTBAR] textboxCanvasGroup is not assigned in the Inspector.");
            if (promptText == null)
                Debug.LogError("[HOTBAR] promptText is not assigned in the Inspector.");

            // textbox starts invisible
            if (textboxCanvasGroup != null)
            {
                textboxCanvasGroup.alpha          = 0f;
                textboxCanvasGroup.interactable   = false;
                textboxCanvasGroup.blocksRaycasts = false;
            }
        }

        public void ShowPrompt(string text)
        {
            if (promptText != null)
                promptText.text = text;
            FadeTo(1f);
        }

        public void HidePrompt()
        {
            FadeTo(0f);
        }

        private void FadeTo(float targetAlpha)
        {
            if (textboxCanvasGroup == null) return;

            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);

            _fadeCoroutine = StartCoroutine(FadeCoroutine(targetAlpha));
        }

        private IEnumerator FadeCoroutine(float targetAlpha)
        {
            float startAlpha = textboxCanvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                textboxCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed /fadeDuration);
                yield return null;
            }
            textboxCanvasGroup.alpha = targetAlpha;
            textboxCanvasGroup.blocksRaycasts = targetAlpha > 0f;
            _fadeCoroutine = null;
        }
    }
}
