/*
player movement - WASD top-down 2D with rigidbody physics

doesnt use our Singleton<T> base class cos the player lives in room scenes
not CoreScene, so it gets destroyed when rooms unload - meaning
DontDestroyOnLoad would cause problems. uses a manual singleton
pattern instead (Instance property + destroy duplicate in Awake)

movement uses Rigidbody2D with zero gravity cos its top-down not a platformer
GetAxisRaw gives snappy -1, 0, 1 input (no smoothing like GetAxis)
.normalized prevents diagonal movement being faster (sqrt(2) without it)
FixedUpdate for the actual physics, Update for reading input

LockMovement() freezes the player completely - TaskManager and PuzzleManager
call this when youre doing a task or puzzle so you cant just walk away lol
also zeroes the velocity so you dont slide after being locked

animator is optional - if ones assigned it passes MoveX, MoveY, IsMoving
parameters for walk/idle animations. if not assigned it just skips that

spriteRenderer is optional - if assigned, flipX is set when moving left
so we can reuse the right-walk animation frames for left movement
lastFacingX tracks the last horizontal direction so the sprite doesnt
snap back to un-flipped when moving purely up or down

TODO: sprint (hold Shift)

footstep SFX is handled via Animation Events directly on the walk clips (not AudioManager)
- each walk clip fires PlayFootstep() on the frames where a foot lands
- PlayFootstep() uses a dedicated AudioSource (footstepSource) with PlayOneShot
- separate source from ambient/sfx so footstep volume is tunable independently
- Animation Events are frame-accurate so the click always lands on the step, not in Update
*/
using UnityEngine;

namespace SUNSET16.Core
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private Rigidbody2D rb;

        [Header("Input State")]
        private Vector2 moveInput;
        private bool inputLocked = false;

        [Header("Animation (Optional)")]
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        private bool hasAnimator = false;
        private float lastFacingX = 1f; //default facing right

        [Header("Footstep Audio (Optional)")]
        [SerializeField] private AudioSource footstepSource;
        [SerializeField] private AudioClip   footstepClip;
        [SerializeField] [Range(0f, 1f)] private float footstepVolume = 0.6f;

        void Awake()
        {
            //manual singleton - same idea as Singleton<T> but without DontDestroyOnLoad
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject); //already got one, bye
                return;
            }

            if (rb == null)
            {
                rb = GetComponent<Rigidbody2D>(); //grab it if not assigned in Inspector
            }

            rb.gravityScale = 0f; //top-down so no gravity
            rb.freezeRotation = true; //dont spin on collision
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; //prevents phasing thru walls at high speed
            hasAnimator = animator != null;

            Debug.Log("[PLAYERCONTROLLER] Initialized");
        }

        void Update()
        {
            if (!inputLocked)
            {
                //GetAxisRaw = -1, 0, or 1 (no smoothing, snappy feel)
                moveInput.x = Input.GetAxisRaw("Horizontal");
                moveInput.y = Input.GetAxisRaw("Vertical");
            }
            else
            {
                moveInput = Vector2.zero; //locked = no input at all
            }

            if (hasAnimator)
            {
                UpdateAnimations();
            }
        }

        void FixedUpdate()
        {
            //.normalized so diagonal isnt faster (would be ~1.4x without it)
            rb.velocity = moveInput.normalized * moveSpeed;
        }

        //TaskManager, PuzzleManager, overlays etc call this to freeze/unfreeze the player
        public void LockMovement(bool locked)
        {
            inputLocked = locked;

            if (locked)
            {
                //zero everything so the player doesnt keep sliding from residual velocity
                moveInput = Vector2.zero;
                rb.velocity = Vector2.zero;
            }

            Debug.Log($"[PLAYERCONTROLLER] Input {(locked ? "locked" : "unlocked")} (WASD + E key)");
        }

        public bool IsMovementLocked()
        {
            return inputLocked;
        }

        //pass movement data to the animator if one exists
        //the Animator Controller uses these to pick walk/idle animations
        void UpdateAnimations()
        {
            float moveX = moveInput.x;
            float moveY = moveInput.y;

            animator.SetFloat("MoveX", moveX);
            animator.SetFloat("MoveY", moveY);
            animator.SetBool("IsMoving", moveInput.magnitude > 0.1f); //small threshold so tiny drift doesnt count as moving

            //flip sprite for left movement - reuses right-walk frames, no extra sprites needed
            //only update lastFacingX when actually moving horizontally so
            //the sprite doesnt un-flip when you switch to moving up/down
            if (moveX != 0f)
            {
                lastFacingX = moveX;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = (lastFacingX < 0f);
            }
        }

        public float GetMoveSpeed()
        {
            return moveSpeed;
        }

        public void SetMoveSpeed(float speed)
        {
            moveSpeed = Mathf.Max(0, speed);
            Debug.Log($"[PLAYERCONTROLLER] Move speed set to {moveSpeed}");
        }

        public Vector3 GetPosition()
        {
            return transform.position;
        }

        public void SetPosition(Vector3 position)
        {
            transform.position = position;
            rb.velocity = Vector2.zero; //stop any movement when teleporting
            Debug.Log($"[PLAYERCONTROLLER] Position set to {position}");
        }

        // called by Animation Events on each walk clip at the frame a foot lands
        // method name must match exactly what is typed in the Animation Event inspector
        // footstepSource/footstepClip are optional - silently skipped if not assigned
        public void PlayFootstep()
        {
            if (footstepSource != null && footstepClip != null)
                footstepSource.PlayOneShot(footstepClip, footstepVolume);
        }
    }
}