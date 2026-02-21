using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

namespace SUNSET16.Core
{
    public class RoomManager : Singleton<RoomManager>
    {
        [Header("Current State")]
        private string _currentRoomScene = "";

        [Header("Transition Settings")]
        [SerializeField] private float fadeOutDuration = 0.5f;
        [SerializeField] private float fadeInDuration = 0.5f;

        private bool _isTransitioning = false;

        [Header("Spawn Position Management")]
        private Vector3 _nextSpawnPosition = Vector3.zero;
        private bool _hasCustomSpawnPosition = false;

        public event Action<string> OnRoomLoaded;
        public event Action<string> OnRoomUnloaded;

        protected override void Awake()
        {
            base.Awake();
        }

        public void LoadRoom(string roomSceneName)
        {
            if (_isTransitioning)
            {
                Debug.LogWarning("[ROOMMANAGER] Already transitioning - cannot load another room");
                return;
            }

            if (string.IsNullOrEmpty(roomSceneName))
            {
                Debug.LogError("[ROOMMANAGER] Room scene name is null or empty");
                return;
            }

            StartCoroutine(LoadRoomCoroutine(roomSceneName));
        }

        private IEnumerator LoadRoomCoroutine(string roomSceneName)
        {
            _isTransitioning = true;

            string roomId = ExtractRoomIdFromSceneName(roomSceneName);

            yield return StartCoroutine(FadeOut());

            if (!string.IsNullOrEmpty(_currentRoomScene))
            {
                yield return SceneManager.UnloadSceneAsync(_currentRoomScene);
                OnRoomUnloaded?.Invoke(_currentRoomScene);
            }

            yield return SceneManager.LoadSceneAsync(roomSceneName, LoadSceneMode.Additive);

            _currentRoomScene = roomSceneName;
            SetCurrentRoomName(roomSceneName);

            if (!string.IsNullOrEmpty(roomId) && HiddenRoomManager.Instance != null)
            {
                HiddenRoomManager.Instance.EnterRoom(roomId);
            }

            if (PlayerController.Instance != null)
            {
                Vector3 spawnPos = GetSpawnPosition();
                PlayerController.Instance.SetPosition(spawnPos);
                Debug.Log($"[ROOMMANAGER] Player spawned at {spawnPos}");
            }

            OnRoomLoaded?.Invoke(roomSceneName);
            yield return StartCoroutine(FadeIn());

            _isTransitioning = false;
            Debug.Log($"[ROOMMANAGER] Room loaded: {roomSceneName}");
        }

        private string ExtractRoomIdFromSceneName(string sceneName)
        {
            if (sceneName.Contains("_"))
            {
                string[] parts = sceneName.Split('_');
                if (parts.Length >= 2)
                {
                    return parts[parts.Length - 1];
                }
            }
            return sceneName;
        }

        private IEnumerator FadeOut()
        {
            yield break; //ui fade system
        }

        private IEnumerator FadeIn()
        {
            yield break;
        }
        public string GetCurrentRoomName()
        {
            return _currentRoomScene;
        }

        public bool IsInRoom(string roomName)
        {
            return _currentRoomScene == roomName;
        }

        public bool GetIsTransitioning()
        {
            return _isTransitioning;
        }

        public void SetCurrentRoomName(string roomName)
        {
            _currentRoomScene = roomName;
            // reminder for self, VALIDATION
        }

        public void SetNextSpawnPosition(Vector3 position)
        {
            _nextSpawnPosition = position;
            _hasCustomSpawnPosition = true;
            Debug.Log($"[ROOMMANAGER] Next spawn position set to {position}");
        }

        public Vector3 GetSpawnPosition()
        {
            if (_hasCustomSpawnPosition)
            {
                Vector3 pos = _nextSpawnPosition;
                _hasCustomSpawnPosition = false;
                return pos;
            }

            return Vector3.zero;
        }
    }
}