using UnityEngine;
using UnityEngine.UI;

namespace SUNSET16.UI
{
    /// <summary>
    /// Cycles through a Sprite array on a UI Image at a configurable frame rate.
    /// The animation loops continuously and resets to frame 0 on re-enable.
    ///
    /// Moved from TechDemo/SimpleSpriteAnimator.cs — logic unchanged, namespace updated.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class SimpleSpriteAnimator : MonoBehaviour
    {
        [Header("Animation")]
        [Tooltip("Sprites played in order, looping back to the first after the last.")]
        [SerializeField] private Sprite[] frames;
        [Tooltip("Playback speed in frames per second.")]
        [SerializeField] private float framesPerSecond = 8f;

        private Image _image;
        private int   _currentFrame;
        private float _timer;

        private void Awake()
        {
            _image = GetComponent<Image>();
        }

        private void OnEnable()
        {
            // Reset to first frame whenever the GameObject becomes active
            _currentFrame = 0;
            _timer        = 0f;
            if (frames != null && frames.Length > 0 && _image != null)
                _image.sprite = frames[0];
        }

        private void Update()
        {
            if (frames == null || frames.Length == 0) return;

            _timer += Time.deltaTime;

            if (_timer >= 1f / framesPerSecond)
            {
                _timer        = 0f;
                _currentFrame = (_currentFrame + 1) % frames.Length;
                _image.sprite = frames[_currentFrame];
            }
        }
    }
}
