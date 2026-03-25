/*
THE MASTER ORCHESTRATOR - this is the boss manager that runs the entire game
GameManager is the first thing that initializes and it coordinates all other managers

from the original Core Systems brainstorming doc (feb 4):
- it was supposed to just "verify other managers exist" and provide a central access point
- but during implementation it evolved into the full initialization sequence controller
- it now handles: init order, save loading, auto-save on quit, and ending events

the initialization sequence is CRITICAL (this was a big lesson):
1. GameManager.Start() runs (after all Awake() methods finish)
2. it checks that ALL required managers exist in the scene
3. then initializes them IN ORDER: DayManager -> PillStateManager -> SettingsManager -> SaveManager
4. subscribes to ending/game-over events
5. loads saved game if one exists
6. fires OnInitializationComplete so other systems know its safe to start

if ANY manager is missing from CoreScene, the game wont start
this is intentional - better to fail loudly than have weird bugs from missing managers

HOW THIS EVOLVED:
- we originally planned a StartNewGame() and TriggerEnding() method - havent needed those yet
- added OnApplicationPaused/Resumed events for WebGL tab switching
- added HandleEndingReached, HandleGameEndedEarly, HandleGameComplete callbacks
- removed the coroutine-based verification (just do null checks directly in Start)

TODO: implement StartNewGame() for the main menu "New Game" button
TODO: implement scene loading for endings (good ending cutscene, bad ending cutscene)
TODO: add a game state enum (MainMenu, Playing, Paused, GameOver) for cleaner flow control
*/
using UnityEngine;
using System; //need to actions later on

namespace SUNSET16.Core
{
    //inherits from Singleton<GameManager> so we get the Instance property and DontDestroyOnLoad for free
    public class GameManager : Singleton<GameManager>
    {
        public bool IsInitialized { get; private set; } //other systems check this before doing anything
        public event Action OnInitializationComplete; //fires after ALL managers are ready - safe to subscribe to
        public event Action OnApplicationPaused;  //fires when WebGL tab is hidden or game is minimized
        public event Action OnApplicationResumed; //fires when player comes back to the game

        protected override void Awake()
        {
            base.Awake(); //Singleton.Awake() handles the instance check and DontDestroyOnLoad
        }

        //Start() runs AFTER all Awake() calls finish across all GameObjects
        //this is why we do initialization here - all managers have their singletons set up by now
        private void Start()
        {
            Initialize();
        }

        /*
        THE INITIALIZATION SEQUENCE
        this is the most important method in the entire game
        if this fails, nothing works - thats why theres so much logging
        the order matters cos some managers depend on others being ready first
        */
        private void Initialize()
        {
            Debug.Log("[GAMEMANAGER] ===== INITIALIZATION SEQUENCE START =====");

            //STEP 1: verify all required managers exist in the scene
            //if any are missing, someone forgot to add the GameObject to CoreScene
            if (DayManager.Instance == null ||
                PillStateManager.Instance == null ||
                SettingsManager.Instance == null ||
                SaveManager.Instance == null)
            {
                Debug.LogError("[GAMEMANAGER] CRITICAL: One or more managers missing from CoreScene!");
                return; //bail out completely - game cant run without all managers
            }

            //STEP 2: initialize state managers first (they dont depend on each other)
            Debug.Log("[GAMEMANAGER] Phase 1: Initializing state managers...");
            DayManager.Instance.Initialize(); //sets starting day (1), morning phase
            PillStateManager.Instance.Initialize(); //clears all pill choices
            SettingsManager.Instance.Initialize(); //loads volume/brightness from PlayerPrefs

            //STEP 3: subscribe to game-ending events so we can handle them centrally
            PillStateManager.Instance.OnEndingReached += HandleEndingReached;
            DayManager.Instance.OnGameEndedEarly += HandleGameEndedEarly;
            DayManager.Instance.OnGameComplete += HandleGameComplete;

            //STEP 4: initialize save system LAST (it needs other managers ready to load into)
            Debug.Log("[GAMEMANAGER] Phase 2: Initializing save system...");
            SaveManager.Instance.Initialize();

            //STEP 5: if theres a saved game, load it (this overwrites the defaults we just set)
            if (SaveManager.Instance.SaveExists)
            {
                Debug.Log("[GAMEMANAGER] Loading saved game...");
                SaveManager.Instance.LoadGame();
            }

            //STEP 6: mark as ready and tell everyone
            IsInitialized = true;
            OnInitializationComplete?.Invoke(); //other systems waiting for this can now start
            Debug.Log("[GAMEMANAGER] ===== INITIALIZATION COMPLETE =====");

            //STEP 7: load the starting room
            //new game (no save) = play intro cutscene first, then bedroom
            //continue (save exists) = skip cutscene, go straight to bedroom
            //CoreScene stays loaded underneath either way - RoomManager handles additive loading
            if (RoomManager.Instance != null)
            {
                string startingRoom = SaveManager.Instance.SaveExists ? "BedroomScene" : "CutSceneDay1";
                Debug.Log($"[GAMEMANAGER] Loading starting room: {startingRoom}");
                // cutscene: skip both fades — main menu already faded to black, cutscene plays raw
                // bedroom (continue): normal fades apply
                bool isCutscene = startingRoom == "CutSceneDay1";
                RoomManager.Instance.LoadRoom(startingRoom, skipFadeIn: isCutscene, skipFadeOut: isCutscene);
            }
            else
            {
                Debug.LogError("[GAMEMANAGER] RoomManager missing from CoreScene - cannot load starting room!");
            }
        }

        //auto-save when the application is closing (important for WebGL where closing = closing browser tab)
        protected override void OnApplicationQuit()
        {
            if (IsInitialized)
            {
                Debug.Log("[GAMEMANAGER] Application quitting - auto-saving...");
                SaveManager.Instance.SaveGame(); //save before everything gets destroyed
            }
            base.OnApplicationQuit(); //Singleton.OnApplicationQuit() sets the quitting flag
        }

        //handles when WebGL tab is hidden/shown or game is minimized/restored
        //pauseStatus = true means game is being paused (tab hidden)
        //pauseStatus = false means game is being resumed (tab visible again)
        private void OnApplicationPause(bool pauseStatus)
        {
            if (!IsInitialized)
                return; //dont do anything if we havent finished initializing

            if (pauseStatus)
            {
                Debug.Log("[GAMEMANAGER] Application paused"); // no longer auto saving
                // SaveManager.Instance.SaveGame(); //should fix the application pausing right?
                //NOTE: we disabled auto-save on pause cos it was causing issues
                //TODO: figure out if we should auto-save when tab is hidden on WebGL
                OnApplicationPaused?.Invoke();
            }
            else
            {
                Debug.Log("[GAMEMANAGER] Application resumed");
                OnApplicationResumed?.Invoke();
            }
        }

        //called from the pause menu or main menu "Quit" button
        //saves the game then exits (or stops play mode in editor)
        public void QuitGame()
        {
            SaveManager.Instance.SaveGame();
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false; //stops play mode in unity editor
            #else
                Application.Quit(); //actually closes the application in a build
            #endif
        }

        //callback for when PillStateManager determines an ending has been reached (3+ pills taken or refused)
        //TODO: this should trigger the actual ending cutscene/sequence
        private void HandleEndingReached(string ending)
        {
            Debug.Log($"[GAMEMANAGER] Ending reached: {ending}");
        }

        //callback for when the game ends before day 5 (early ending from pill threshold)
        //TODO: load the appropriate ending scene
        private void HandleGameEndedEarly(int day)
        {
            Debug.Log($"[GAMEMANAGER] Game ended early on Day {day}");
        }

        //callback for when the player reaches day 5 night naturally (full playthrough)
        //TODO: determine which ending to show based on pill choices and load it
        private void HandleGameComplete()
        {
            Debug.Log("[GAMEMANAGER] Game reached natural completion (Day 5)");
        }
    }
}
