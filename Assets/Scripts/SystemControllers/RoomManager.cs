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

        public event Action<string> OnRoomLoaded;
        public event Action<string> OnRoomUnloaded;

        protected override void Awake()
        {
            base.Awake();
        }

        public void LoadRoom(string roomSceneName)
        {
            throw new NotImplementedException();
        }

        private IEnumerator LoadRoomCoroutine(string roomSceneName)
        {
            yield break; //fade out -> unload -> load additive -> set active -> fade in
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
    }
}