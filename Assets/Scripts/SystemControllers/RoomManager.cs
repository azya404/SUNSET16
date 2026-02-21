/*
manages loading and unloading room scenes using additive scene loading
this is how the player moves between rooms on the space station

the CoreScene stays loaded forever (DontDestroyOnLoad) and room scenes
get loaded ON TOP of it additively - so when you walk through a door
the old room unloads and the new one loads in, but all the managers persist

when a door triggers a room load it also sets the spawn position
so the player appears at the right door in the new room instead of (0,0,0)
thats a one-shot thing tho - resets after being used so it doesnt
carry over to the next load accidentally

the fade in/out is just stubs rn (yield break = does nothing)
was gonna be a full CanvasGroup alpha lerp but we havent built that yet

the room ID extraction from scene name is a bit hacky (splits on underscore)
probably should use a dictionary or something more explicit

TODO: actual FadeOut/FadeIn with a fullscreen black overlay
TODO: scene name -> room ID mapping should be a dictionary
TODO: loading screen or progress bar for slower loads
TODO: validate room scenes exist before trying to load them
*/
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

        //called by DoorController when player walks through a door
        public void LoadRoom(string roomSceneName)
        {
            //dont allow another load while were mid-transition
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

        //the actual room swap logic - runs as a coroutine so we can yield between steps
        private IEnumerator LoadRoomCoroutine(string roomSceneName)
        {
            _isTransitioning = true;

            string roomId = ExtractRoomIdFromSceneName(roomSceneName);

            yield return StartCoroutine(FadeOut()); //TODO: this does nothing rn

            //unload the current room if theres one loaded
            if (!string.IsNullOrEmpty(_currentRoomScene))
            {
                yield return SceneManager.UnloadSceneAsync(_currentRoomScene);
                OnRoomUnloaded?.Invoke(_currentRoomScene);
            }

            //load the new room additively (CoreScene stays loaded underneath)
            yield return SceneManager.LoadSceneAsync(roomSceneName, LoadSceneMode.Additive);

            _currentRoomScene = roomSceneName;
            SetCurrentRoomName(roomSceneName);

            //tell HiddenRoomManager we entered this room (if applicable)
            if (!string.IsNullOrEmpty(roomId) && HiddenRoomManager.Instance != null)
            {
                HiddenRoomManager.Instance.EnterRoom(roomId);
            }

            //put the player at the right spawn point
            if (PlayerController.Instance != null)
            {
                Vector3 spawnPos = GetSpawnPosition();
                PlayerController.Instance.SetPosition(spawnPos);
                Debug.Log($"[ROOMMANAGER] Player spawned at {spawnPos}");
            }

            OnRoomLoaded?.Invoke(roomSceneName);
            yield return StartCoroutine(FadeIn()); //TODO: also does nothing

            _isTransitioning = false;
            Debug.Log($"[ROOMMANAGER] Room loaded: {roomSceneName}");
        }

        //hacky but works - splits "Room_Hallway" into ["Room", "Hallway"] and takes the last part
        //should probably be a dictionary but this is fine for now
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

        //TODO: replace with actual screen fade (CanvasGroup + alpha lerp)
        private IEnumerator FadeOut()
        {
            yield break; //does nothing rn lol
        }

        private IEnumerator FadeIn()
        {
            yield break; //same
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

        //DoorController calls this BEFORE LoadRoom so we know where to put the player
        public void SetNextSpawnPosition(Vector3 position)
        {
            _nextSpawnPosition = position;
            _hasCustomSpawnPosition = true;
            Debug.Log($"[ROOMMANAGER] Next spawn position set to {position}");
        }

        //returns the spawn pos and resets the flag so it doesnt carry over
        //if no custom pos was set, defaults to (0,0,0)
        public Vector3 GetSpawnPosition()
        {
            if (_hasCustomSpawnPosition)
            {
                Vector3 pos = _nextSpawnPosition;
                _hasCustomSpawnPosition = false; //one-shot, reset after use
                return pos;
            }

            return Vector3.zero; //fallback
        }
    }
}