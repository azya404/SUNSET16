/*
barebones sprite animation - just cycles through sprites like a flipbook
we use this for the computer screen in the bedroom to make it look alive

drag your sprites into the frames array in the Inspector, set the speed,
and it loops forever. resets when the object gets enabled so it always
starts clean. uses modulo to wrap around to the first frame

only works on UI Images (not SpriteRenderers) - if you need to animate
gameplay sprites use the Animator instead
*/
using UnityEngine;
using UnityEngine.UI;

namespace SUNSET16.TechDemo
{
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
            //start from the beginning whenever this gets turned on
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

            //time to swap to the next frame?
            if (timer >= 1f / framesPerSecond)
            {
                timer = 0f;
                currentFrame = (currentFrame + 1) % frames.Length;
                image.sprite = frames[currentFrame];
            }
        }
    }
}
