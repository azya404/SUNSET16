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

            if (mainCamera == null)
                mainCamera = GetComponent<Camera>();

            if (mainCamera == null)
                mainCamera = Camera.main;
        }

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

            if (useBounds)
            {
                targetPosition.x = Mathf.Clamp(targetPosition.x, _minX, _maxX);
                targetPosition.y = Mathf.Clamp(targetPosition.y, _minY, _maxY);
            }

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

        public void PanToPosition(Vector3 position, float duration = 1f)
        {
            if (_panCoroutine != null)
                StopCoroutine(_panCoroutine);

            _panCoroutine = StartCoroutine(PanCoroutine(position, duration));
        }

        private IEnumerator PanCoroutine(Vector3 targetPosition, float duration)
        {
            _isFollowing = false;

            Vector3 startPosition = transform.position;
            targetPosition.z = offset.z;
            float timer = 0;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = timer / duration;

                transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                yield return null;
            }

            transform.position = targetPosition;

            yield return new WaitForSeconds(0.5f);

            _isFollowing = true;
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

        public void OnRoomLoaded(Vector3 playerSpawnPosition)
        {
            Vector3 cameraPosition = playerSpawnPosition + offset;

            if (useBounds)
            {
                cameraPosition.x = Mathf.Clamp(cameraPosition.x, _minX, _maxX);
                cameraPosition.y = Mathf.Clamp(cameraPosition.y, _minY, _maxY);
            }

            transform.position = cameraPosition;

            Debug.Log($"[CAMERACONTROLLER] Positioned at {cameraPosition} for room load");
        }
    }
}