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
        private bool hasAnimator = false;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (rb == null)
            {
                rb = GetComponent<Rigidbody2D>();
            }

            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            hasAnimator = animator != null;

            Debug.Log("[PLAYERCONTROLLER] Initialized");
        }

        void Update()
        {
            if (!inputLocked)
            {
                moveInput.x = Input.GetAxisRaw("Horizontal");
                moveInput.y = Input.GetAxisRaw("Vertical");
            }
            else
            {
                moveInput = Vector2.zero;
            }

            if (hasAnimator)
            {
                UpdateAnimations();
            }
        }

        void FixedUpdate()
        {
            rb.velocity = moveInput.normalized * moveSpeed;
        }

        public void LockMovement(bool locked)
        {
            inputLocked = locked;

            if (locked)
            {
                moveInput = Vector2.zero;
                rb.velocity = Vector2.zero;
            }

            Debug.Log($"[PLAYERCONTROLLER] Input {(locked ? "locked" : "unlocked")} (WASD + E key)");
        }

        public bool IsMovementLocked()
        {
            return inputLocked;
        }

        void UpdateAnimations()
        {
            float moveX = moveInput.x;
            float moveY = moveInput.y;

            animator.SetFloat("MoveX", moveX);
            animator.SetFloat("MoveY", moveY);
            animator.SetBool("IsMoving", moveInput.magnitude > 0.1f);
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
            rb.velocity = Vector2.zero;
            Debug.Log($"[PLAYERCONTROLLER] Position set to {position}");
        }
    }
}
