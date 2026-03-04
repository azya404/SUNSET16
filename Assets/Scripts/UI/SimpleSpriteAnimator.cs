/*
dead simple sprite animation for UI Images
cycles through a Sprite array at a set fps, loops forever
nothing fancy - if you need it to do something fancier use a proper animator

resets to frame 0 on re-enable so it always starts from the beginning
useful for UI elements that get toggled on and off repeatedly

moved here from TechDemo/SimpleSpriteAnimator.cs - only change is the namespace
logic is 100% identical, now lives in SUNSET16.UI with the rest of the UI scripts

TODO: option to pause when Time.timeScale = 0 (so animations freeze with the game)
TODO: ping-pong mode (play forward then backward) instead of just looping
*/
using UnityEngine;
using UnityEngine.UI;

namespace SUNSET16.UI
{
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
            // reset to first frame whenever the GameObject becomes active
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
