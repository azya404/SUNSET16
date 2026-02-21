using UnityEngine;
using UnityEngine.UI;

namespace SUNSET16.TechDemo
{
    /// <summary>
    /// Simple sprite animator for looping UI images.
    /// Cycles through sprites at specified frame rate.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class SimpleSpriteAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private Sprite[] frames; // Assign sprites in order
        [SerializeField] private float framesPerSecond = 8f; // Animation speed

        private Image image;
        private int currentFrame = 0;
        private float timer = 0f;

        void Awake()
        {
            image = GetComponent<Image>();
        }

        void OnEnable()
        {
            // Reset animation when enabled
            currentFrame = 0;
            timer = 0f;
            if (frames.Length > 0 && image != null)
            {
                image.sprite = frames[0];
            }
        }

        void Update()
        {
            if (frames.Length == 0) return;

            timer += Time.deltaTime;

            // Check if it's time to advance to next frame
            if (timer >= 1f / framesPerSecond)
            {
                timer = 0f;
                currentFrame = (currentFrame + 1) % frames.Length;
                image.sprite = frames[currentFrame];
            }
        }
    }
}
