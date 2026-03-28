/*
idle frame-by-frame animator for the bedroom computer screen
cycles through sprite frames at a set FPS — always looping

TODO: second mode (flicker/glitch variant) to be added later

to use:
1. assign sprites in the Frames array in numbered order
2. set FPS (12 is smooth for an idle screen glow)
3. plays automatically on Start
*/
using UnityEngine;

namespace SUNSET16.Effects
{
    public class ComputerIdle : MonoBehaviour
    {
        [Header("Animation")]
        [Tooltip("Sprites to cycle through in order. Drag them in numbered order.")]
        [SerializeField] private Sprite[] frames;

        [Tooltip("Frames per second. 12 is smooth for idle animations.")]
        [SerializeField] private float fps = 12f;

        private SpriteRenderer _sr;
        private int   _currentFrame;
        private float _timer;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            if (frames == null || frames.Length == 0)
            {
                Debug.LogWarning($"[ComputerIdle] No frames assigned on {gameObject.name}");
                enabled = false;
                return;
            }

            _sr.sprite = frames[0];
        }

        private void Update()
        {
            _timer += Time.deltaTime;

            if (_timer >= 1f / fps)
            {
                _timer -= 1f / fps;
                _currentFrame = (_currentFrame + 1) % frames.Length;
                _sr.sprite = frames[_currentFrame];
            }
        }
    }
}
