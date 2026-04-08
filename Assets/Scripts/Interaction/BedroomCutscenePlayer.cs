/*
Handles inline video cutscene playback within BedroomScene.
Used by PodInteraction (sleep transitions) and MirrorInteraction (Day 2 mirror).

RenderTexture is created at runtime so no persistent .renderTexture asset is dirtied on play.
Caller must ensure screen is already black before calling Play() — this script does not fade.
CutsceneCanvas Sort Order must be set HIGHER than PillChoiceFade's canvas sort order in the Inspector
so the video renders on top of the black overlay and is actually visible to the player.

VideoPlayer Render Mode must be set to Render Texture in Inspector,
but leave Target Texture UNASSIGNED — it is assigned at runtime here.
*/
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace SUNSET16.Interaction
{
    public class BedroomCutscenePlayer : MonoBehaviour
    {
        [Tooltip("VideoPlayer component on this GO — Render Mode: Render Texture, Target Texture: leave blank.")]
        [SerializeField] private VideoPlayer videoPlayer;

        [Tooltip("RawImage child of CutsceneCanvas that displays the video frame.")]
        [SerializeField] private RawImage cutsceneImage;

        [Tooltip("CanvasGroup on the CutsceneCanvas root — snapped to alpha 1 when playing, 0 when idle.")]
        [SerializeField] private CanvasGroup cutsceneCanvasGroup;

        private RenderTexture _rt;

        // Call this from PodInteraction or MirrorInteraction with screen already black.
        // Yields until the video ends. Cleans up the RenderTexture after itself.
        public IEnumerator Play(string videoFileName)
        {
            // create RT at runtime — no saved asset, no dirty-on-play file modification
            _rt = new RenderTexture(1920, 1080, 0);
            videoPlayer.targetTexture = _rt;
            cutsceneImage.texture     = _rt;

            videoPlayer.url = Application.streamingAssetsPath + "/" + videoFileName;

            // prepare first so no blank first frame
            videoPlayer.Prepare();
            yield return new WaitUntil(() => videoPlayer.isPrepared);

            bool finished = false;
            videoPlayer.loopPointReached += _ => finished = true;

            // snap canvas visible — screen is already black from caller's fade, snap is invisible
            cutsceneCanvasGroup.alpha          = 1f;
            cutsceneCanvasGroup.blocksRaycasts = true;

            videoPlayer.Play();
            yield return new WaitUntil(() => finished);

            // hide before returning control to caller
            cutsceneCanvasGroup.alpha          = 0f;
            cutsceneCanvasGroup.blocksRaycasts = false;

            videoPlayer.Stop();
            videoPlayer.targetTexture = null;

            // release and destroy the runtime RT — no lingering dirty asset
            _rt.Release();
            Destroy(_rt);
            _rt = null;
        }
    }
}
