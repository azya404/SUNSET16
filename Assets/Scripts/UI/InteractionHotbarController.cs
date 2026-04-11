/*
persistent hotbar that lives in CoreScene alongside HUDController and DOLOSManager
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using SUNSET16.Core;
using UnityEngine.Video;

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
        // tracks every zone the player is currently inside
        // key = the InteractionSystem that owns the zone, value = its prompt text
        // last entry added wins for display — only fades out when dict is fully empty
        private Dictionary<InteractionSystem, string> _activePrompts = new Dictionary<InteractionSystem, string>();

        protected override void Awake()
        {
            base.Awake();

            if (characterSprite == null)
                Debug.LogError("[HOTBAR] characterSprite is not assigned in the Inspector.");
            if (textboxCanvasGroup == null)
                Debug.LogError("[HOTBAR] textboxCanvasGroup is not assigned in the Inspector.");
            if (promptText == null)
                Debug.LogError("[HOTBAR] promptText is not assigned in the Inspector.");

            if (textboxCanvasGroup != null)
            {
                textboxCanvasGroup.alpha          = 0f;
                textboxCanvasGroup.interactable   = false;
                textboxCanvasGroup.blocksRaycasts = false;
            }
        }

        private void Start()
        {
            if (RoomManager.Instance != null)
                RoomManager.Instance.OnRoomUnloaded += HandleRoomUnloaded;
            else
                Debug.LogWarning("[HOTBAR] RoomManager not found — scene transition cleanup will not work.");
        }

        private void OnDestroy()
        {
            if (RoomManager.Instance != null)
                RoomManager.Instance.OnRoomUnloaded -= HandleRoomUnloaded;
        }

        // called by RoomManager when a room scene unloads
        // OnTriggerExit2D doesnt fire reliably during scene unloads so any
        // prompt still showing at that point would get stuck without this
        private void HandleRoomUnloaded(string roomName)
        {
            ForceHide();
        }

        // called by InteractionSystem when player enters a trigger zone
        public void RegisterPrompt(InteractionSystem source, string text)
        {
            _activePrompts[source] = text;
            UpdateDisplay(text);
        }

        // called by InteractionSystem when player exits a trigger zone or interaction is disabled
        public void UnregisterPrompt(InteractionSystem source)
        {
            _activePrompts.Remove(source);

            if (_activePrompts.Count == 0)
            {
                FadeTo(0f);
            }
            else
            {
                // still inside at least one other zone...like show whichever is still active
                string remaining = GetLastPrompt();
                UpdateDisplay(remaining);
            }
        }

        // instantly hides the textbox and clears all active zones
        // called by InteractionSystem.Interact() the moment E is pressed
        // so the hotbar is gone before any overlay starts opening
        public void ForceHide()
        {
            _activePrompts.Clear();

            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            if (textboxCanvasGroup != null)
            {
                textboxCanvasGroup.alpha          = 0f;
                textboxCanvasGroup.blocksRaycasts = false;
            }
        }

        private void UpdateDisplay(string text)
        {
            if (promptText != null)
                promptText.text = text;
            FadeTo(1f);
        }

        private string GetLastPrompt()
        {
            string last = "";
            foreach (var entry in _activePrompts)
                last = entry.Value;
            return last;
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
                textboxCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
                yield return null;
            }
            textboxCanvasGroup.alpha = targetAlpha;
            textboxCanvasGroup.blocksRaycasts = targetAlpha > 0f;
            _fadeCoroutine = null;
        }

        public void characterState(bool active)
        {
            characterSprite.gameObject.SetActive(active);
        }

        public bool isCharacterActive()
        {
            return characterSprite.gameObject.activeSelf;
        }
    }
}
