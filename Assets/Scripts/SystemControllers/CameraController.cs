/*
camera following + panning for 2D
follows the player around with smooth lerp movement and can also
do cinematic pans (like when a hidden room door is discovered)

uses LateUpdate instead of Update cos LateUpdate runs AFTER the player
has already moved - without this the camera jitters cos its trying
to move at the same time as the player

offset is (0, 0, -10) cos in 2D the camera needs to be at negative z
to actually see the sprites which sit at z=0

has optional bounds clamping so the camera cant scroll past the
edges of a room - RoomManager or CameraSetup sets these

TODO: camera zoom for close-up interactions (change orthographic size)
TODO: screen shake for dramatic moments
TODO: bounds should auto-detect from tilemap or collider
*/
using UnityEngine;
using System.Collections;

namespace SUNSET16.Core
{
    public class CameraController : Singleton<CameraController>
    {
        [Header("Camera Settings")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private float followSpeed = 5f;
        [SerializeField] private Vector3 offset = new Vector3(0, 0, -10f);

        [Header("Camera Bounds")]
        [SerializeField] private bool useBounds = false;
        private float _minX, _maxX, _minY, _maxY;

        private Transform _target; 
        private bool _isFollowing = true;
        private Coroutine _panCoroutine;

        protected override void Awake()
        {
            base.Awake();

            //try to grab the camera component, fall back to Camera.main
            if (mainCamera == null)
                mainCamera = GetComponent<Camera>();

            if (mainCamera == null)
                mainCamera = Camera.main;
        }

        //LateUpdate so we move AFTER the player has already moved this frame
        private void LateUpdate()
        {
            if (_isFollowing && _target != null)
            {
                FollowTarget();
            }
        }

        private void FollowTarget()
        {
            Vector3 targetPosition = _target.position + offset;

            //clamp to bounds so the camera cant show outside the room
            if (useBounds)
            {
                targetPosition.x = Mathf.Clamp(targetPosition.x, _minX, _maxX);
                targetPosition.y = Mathf.Clamp(targetPosition.y, _minY, _maxY);
            }

            //Lerp gives the smooth trailing effect instead of rigid 1:1 following
            //higher followSpeed = snappier, lower = more cinematic float
            transform.position = Vector3.Lerp(
                transform.position,
                targetPosition,
                followSpeed * Time.deltaTime
            );
        }

        public void SetTarget(Transform newTarget)
        {
            _target = newTarget;
            Debug.Log($"[CAMERACONTROLLER] Target set to {(newTarget != null ? newTarget.name : "null")}");
        }

        //used for hidden room discovery - camera pans to show the door then comes back
        public void PanToPosition(Vector3 position, float duration = 1f)
        {
            if (_panCoroutine != null)
                StopCoroutine(_panCoroutine); //cancel any existing pan

            _panCoroutine = StartCoroutine(PanCoroutine(position, duration));
        }

        private IEnumerator PanCoroutine(Vector3 targetPosition, float duration)
        {
            _isFollowing = false; //stop following so the pan doesnt fight with FollowTarget

            Vector3 startPosition = transform.position;
            targetPosition.z = offset.z; //keep the z offset
            float timer = 0;

            //lerp from current pos to target over duration
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = timer / duration;

                transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                yield return null;
            }

            transform.position = targetPosition;

            yield return new WaitForSeconds(0.5f); //hold on the target for half a sec so player can see it

            _isFollowing = true; //resume following the player
            _panCoroutine = null;
        }

        public void SetBounds(float minX, float maxX, float minY, float maxY)
        {
            _minX = minX;
            _maxX = maxX;
            _minY = minY;
            _maxY = maxY;
            useBounds = true;

            Debug.Log($"[CAMERACONTROLLER] Bounds set: X({minX}, {maxX}), Y({minY}, {maxY})");
        }

        public void DisableBounds()
        {
            useBounds = false;
            Debug.Log("[CAMERACONTROLLER] Bounds disabled");
        }

        //called when a new room loads - snaps camera to player pos instantly
        //no smooth lerp here cos the player just teleported to a new scene
        public void OnRoomLoaded(Vector3 playerSpawnPosition)
        {
            Vector3 cameraPosition = playerSpawnPosition + offset;

            if (useBounds)
            {
                cameraPosition.x = Mathf.Clamp(cameraPosition.x, _minX, _maxX);
                cameraPosition.y = Mathf.Clamp(cameraPosition.y, _minY, _maxY);
            }

            transform.position = cameraPosition; //snap, dont lerp

            Debug.Log($"[CAMERACONTROLLER] Positioned at {cameraPosition} for room load");
        }
    }
}