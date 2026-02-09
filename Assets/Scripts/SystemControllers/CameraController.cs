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
        }

        private void LateUpdate()
        {
        }

        private void FollowTarget()
        {
        }

        public void SetTarget(Transform newTarget)
        {
        }

        public void PanToPosition(Vector3 position, float duration = 1f)
        {
        }

        private IEnumerator PanCoroutine(Vector3 targetPosition, float duration)
        {
            yield break;
        }

        public void SetBounds(float minX, float maxX, float minY, float maxY)
        {
        }

        public void DisableBounds()
        {
        }

        public void OnRoomLoaded(Vector3 playerSpawnPosition)
        {
        }
    }
}