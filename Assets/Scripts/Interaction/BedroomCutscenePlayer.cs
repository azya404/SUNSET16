/*
Handles inline video cutscene playback and screen fading within BedroomScene.
Used by PodInteraction (sleep transitions) and MirrorInteraction (Day 2 mirror).

FADE PANEL:
  podFadeGroup must point to a dedicated PodFadeCanvas (Sort Order 11, always active,
  black Image child, CanvasGroup alpha 0 at rest). This is the ONLY reliable fade for
  in-scene cutscenes — PillChoiceFade lives under MirrorOverlayCanvas which starts
  inactive and cannot render until ShowOverlay() activates its parent.

VIDEO DISPLAY:
  cutsceneCanvasGroup / cutsceneImage live on CutsceneCanvas (Sort Order 9).
  RenderTexture is created at runtime with explicit Create() to ensure GPU allocation
  before VideoPlayer begins writing frames.

PUBLIC API — called from PodInteraction and MirrorInteraction:
  FadeOut()              fade podFadeGroup 0→1 (screen goes black)
  FadeIn()               fade podFadeGroup 1→0 (screen reveals)
  PlayVideo(fileName)    assumes FadeOut already called — reveals video, plays to end,
                         returns to black. Caller must call FadeIn() afterwards.
*/
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace SUNSET16.Interaction
{
    public class BedroomCutscenePlayer : MonoBehaviour
    {
        [Header("Video Display")]
        [Tooltip("VideoPlayer on this GO — Render Mode: Render Texture, Target Texture: leave blank.")]
        [SerializeField] private VideoPlayer videoPlayer;
        [Tooltip("RawImage on CutsceneImage child — displays the video RenderTexture.")]
        [SerializeField] private RawImage    cutsceneImage;
        [Tooltip("CanvasGroup on CutsceneCanvas (Sort Order 9) — snapped show/hide around video.")]
        [SerializeField] private CanvasGroup cutsceneCanvasGroup;

        [Header("Fade Panel")]
        [Tooltip("CanvasGroup on PodFadeCanvas (Sort Order 11, always active, black Image child). " +
                 "This is the dedicated fade panel — do NOT point this at PillChoiceFade.")]
        [SerializeField] private CanvasGroup podFadeGroup;
        [SerializeField] private float       fadeDuration = 0.5f;

        private RenderTexture _rt;

        // ─── Public API ───────────────────────────────────────────────────────────

        // Fade the screen to black. Call before any scene changes or video playback.
        public IEnumerator FadeOut()
        {
            yield return StartCoroutine(FadePodPanel(0f, 1f));
        }

        // Fade the screen back in. Call after scene reloads or after PlayVideo returns.
        public IEnumerator FadeIn()
        {
            yield return StartCoroutine(FadePodPanel(1f, 0f));
        }

        // Play a video from StreamingAssets. Screen must already be black (FadeOut called).
        // Fades in to reveal video, waits for end, fades back to black, then returns.
        // Caller should call FadeIn() after this to reveal the reloaded scene.
        public IEnumerator PlayVideo(string videoFileName)
        {
            // build RT explicitly — Create() ensures GPU memory is allocated before
            // VideoPlayer starts writing frames (auto-creation timing is unreliable)
            _rt = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
            _rt.Create();
            videoPlayer.targetTexture = _rt;
            cutsceneImage.texture     = _rt;

            videoPlayer.url = Application.streamingAssetsPath + "/" + videoFileName;
            videoPlayer.Prepare();
            yield return new WaitUntil(() => videoPlayer.isPrepared);

            bool finished = false;
            videoPlayer.loopPointReached += _ => finished = true;

            // snap video canvas visible (screen is black from FadeOut — snap is invisible)
            cutsceneCanvasGroup.alpha          = 1f;
            cutsceneCanvasGroup.blocksRaycasts = true;

            videoPlayer.Play();

            // fade in to reveal video (podFadeGroup 1→0, video beneath becomes visible)
            yield return StartCoroutine(FadePodPanel(1f, 0f));

            // wait for video to finish playing
            yield return new WaitUntil(() => finished);

            // fade back to black before returning control
            yield return StartCoroutine(FadePodPanel(0f, 1f));

            // hide video canvas
            cutsceneCanvasGroup.alpha          = 0f;
            cutsceneCanvasGroup.blocksRaycasts = false;

            videoPlayer.Stop();
            videoPlayer.targetTexture = null;

            _rt.Release();
            Destroy(_rt);
            _rt = null;
        }

        // ─── Private ──────────────────────────────────────────────────────────────

        private IEnumerator FadePodPanel(float from, float to)
        {
            if (podFadeGroup == null)
            {
                Debug.LogWarning("[BEDCUTSCENE] podFadeGroup not assigned — skipping fade");
                yield break;
            }

            podFadeGroup.alpha          = from;
            podFadeGroup.blocksRaycasts = (to >= 1f);

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                podFadeGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
                yield return null;
            }

            podFadeGroup.alpha = to;
        }
    }
}
