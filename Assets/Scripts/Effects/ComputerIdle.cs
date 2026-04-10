/*
idle frame-by-frame animator for the bedroom computer screen
cycles through sprite frames at a set FPS — always looping

two sprite sets:
  idleFrames      — plays when player is outside the proximity trigger
  proximityFrames — plays when player is inside the proximity trigger

implements IProximityResponder so InteractionSystem on the same GO
calls OnPlayerEnterZone / OnPlayerExitZone automatically — no separate
collider or trigger handling needed in this script

to use:
1. assign idleFrames in numbered order (1-24 from ComputerIdle folder)
2. assign proximityFrames in numbered order (1-24 from ComputerFlicker folder)
3. set FPS (12 is smooth for idle animations)
4. plays automatically on Start, switches on player proximity
*/
using UnityEngine;
using SUNSET16.Core;

namespace SUNSET16.Effects
{
    public class ComputerIdle : MonoBehaviour, IProximityResponder
    {
        [Header("Animation")]
        [Tooltip("Sprites to cycle when player is outside proximity zone. Drag in numbered order.")]
        [SerializeField] private Sprite[] idleFrames;

        [Tooltip("Sprites to cycle when player is inside proximity zone. Drag in numbered order.")]
        [SerializeField] private Sprite[] proximityFrames;

        [Tooltip("Frames per second. 12 is smooth for idle animations.")]
        [SerializeField] private float fps = 12f;

        private SpriteRenderer _sr;
        private int   _currentFrame;
        private float _timer;
        private bool  _playerInRange;

        private Sprite[] ActiveFrames => _playerInRange && proximityFrames != null && proximityFrames.Length > 0
            ? proximityFrames
            : idleFrames;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            if (idleFrames == null || idleFrames.Length == 0)
            {
                Debug.LogWarning($"[ComputerIdle] No idle frames assigned on {gameObject.name}");
                enabled = false;
                return;
            }

            _sr.sprite = ActiveFrames[0];
        }

        private void Update()
        {
            Sprite[] active = ActiveFrames;
            if (active == null || active.Length == 0) return;

            _timer += Time.deltaTime;

            if (_timer >= 1f / fps)
            {
                _timer -= 1f / fps;
                _currentFrame = (_currentFrame + 1) % active.Length;
                _sr.sprite = active[_currentFrame];
            }
        }

        // ─── IProximityResponder ──────────────────────────────────────────────────

        public void OnPlayerEnterZone()
        {
            _playerInRange = true;
            _currentFrame  = 0;
            _timer         = 0f;
        }

        public void OnPlayerExitZone()
        {
            _playerInRange = false;
            _currentFrame  = 0;
            _timer         = 0f;
        }
    }
}
