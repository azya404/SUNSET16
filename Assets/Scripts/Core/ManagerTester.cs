/*
THE DEBUG DASHBOARD - a Unity OnGUI debug panel for testing all game systems in play mode
this is NOT part of the actual game UI - its a development tool that draws buttons directly on screen

HOW IT WORKS:
- MonoBehaviour (not Singleton) - gets added to a test GameObject in the scene
- OnGUI() is called every frame by Unity and draws immediate-mode GUI
  (this is the old Unity UI system, before Canvas/UI toolkit - simpler for debug tools)
- shows the current game state (day, phase, pill history, task status, etc)
- has buttons to trigger actions (take pill, advance phase, save/load, complete tasks)

THIS IS SUPER USEFUL FOR TESTING cos you can:
- simulate an entire 5-day playthrough without needing real task/puzzle implementations
- test save/load by saving, refreshing, and checking if state persists
- test forced pill choices on days 1-2
- test ending triggers (take 3 pills or refuse 3 pills)
- test hidden room discovery and entry flow
- test puzzle completion flow
- adjust settings (volume) in real time

SECTIONS:
1. Status display (day, phase, game over, pill history)
2. Phase advancement with sleep validation
3. Pill choice buttons (forced vs free)
4. Save/Load/Delete buttons
5. Task system testing (spawn/complete)
6. Hidden room testing (discover/enter)
7. Puzzle system testing (spawn/complete)
8. Settings controls (volume)
9. Tablet toggle

NOTE: this should be DISABLED in production builds (either remove the GameObject or use #if UNITY_EDITOR)
TODO: maybe wrap this in #if UNITY_EDITOR or a DEBUG define so it doesnt ship
TODO: could add more testing for AudioManager, LightingController, VisualStateController
*/
using UnityEngine;

namespace SUNSET16.Core
{
    public class ManagerTester : MonoBehaviour
    {
        //OnGUI is an old Unity callback that runs every frame to draw immediate-mode GUI
        //its basically: every frame, redraw all the buttons/labels from scratch
        //not great for real game UI but perfect for debug tools
        private void OnGUI()
        {
            //draw everything in a fixed area in the top-left corner
            GUILayout.BeginArea(new Rect(10, 10, 420, 1000));

            GUILayout.Label("=== SUNSET16 Manager Tester ===", GUI.skin.box);

            //only show the debug panel if GameManager is ready
            //if its not ready yet, the managers are still initializing
            if (GameManager.Instance != null && GameManager.Instance.IsInitialized)
            {
                // CURRENT STATE DISPLAY
                // shows day, phase, game over status, pill counts, and pill history
                GUILayout.Label($"Day: {DayManager.Instance.CurrentDay}  |  " +
                                $"Phase: {DayManager.Instance.CurrentPhase}  |  " +
                                $"Game Over: {DayManager.Instance.IsGameOver}");
                GUILayout.Label($"Pills Taken: {PillStateManager.Instance.GetPillsTakenCount()}  |  " +
                                $"Pills Refused: {PillStateManager.Instance.GetPillsRefusedCount()}");

                int currentDay = DayManager.Instance.CurrentDay;
                bool isForced = PillStateManager.Instance.IsForcedChoice(currentDay);
                GUILayout.Label($"Today Forced: {isForced}" +
                    (isForced ? $" ({PillStateManager.Instance.GetForcedChoice(currentDay)})" : ""));
                GUILayout.Label($"Chose Today: {PillStateManager.Instance.HasTakenPillToday()}");

                GUILayout.Space(5);
                GUILayout.Label("--- Pill History ---", GUI.skin.box);
                //pill history display: P=taken, N=not taken, -=no choice, *=forced day
                string history = "";
                for (int d = 1; d <= 5; d++)
                {
                    PillChoice c = PillStateManager.Instance.GetPillChoice(d);
                    string label = c == PillChoice.Taken ? "P" :
                                   c == PillChoice.NotTaken ? "N" : "-";
                    string forced = d <= 2 ? "*" : " "; //asterisk marks scripted days
                    history += $"D{d}{forced}:{label}  ";
                }
                GUILayout.Label(history); 

                GUILayout.Label($"Ending: {PillStateManager.Instance.DetermineEnding()}");

                GUILayout.Space(10);

                // PHASE ADVANCEMENT + SLEEP VALIDATION
                // checks if the player can advance (task done, puzzle done if off-pill)
                bool canAdvance = !DayManager.Instance.IsGameOver;
                string advanceStatus = "";

                if (DayManager.Instance.CurrentPhase == DayPhase.Night)
                {
                    bool taskDone = TaskManager.Instance != null && TaskManager.Instance.IsTaskCompleted(currentDay);
                    bool puzzleDone = true;

                    PillChoice choice = PillStateManager.Instance.GetPillChoice(currentDay);
                    if (choice == PillChoice.NotTaken && PuzzleManager.Instance != null)
                    {
                        string puzzleId = $"puzzle_day_{currentDay}";
                        puzzleDone = PuzzleManager.Instance.IsPuzzleCompleted(puzzleId);
                    }

                    bool canSleep = taskDone && puzzleDone;
                    advanceStatus = canSleep ? "Can Sleep: YES" : "Can Sleep: NO";
                    if (!canSleep)
                    {
                        advanceStatus += " (";
                        if (!taskDone) advanceStatus += "!Task ";
                        if (!puzzleDone) advanceStatus += "!Puzzle";
                        advanceStatus += ")";
                    }

                    GUILayout.Label(advanceStatus);
                }

                //GUI.enabled = false disables the next buttons (grayed out, cant click)
                //this prevents the player from advancing when conditions arent met
                GUI.enabled = canAdvance;
                if (GUILayout.Button("Advance Phase"))
                {
                    DayManager.Instance.AdvancePhase();
                }
                GUI.enabled = true; //always re-enable after!

                GUILayout.Space(5);

                // PILL CHOICE BUTTONS
                // forced days show one button, free days show two (take/refuse)

                bool canChoose = !PillStateManager.Instance.HasTakenPillToday()
                              && DayManager.Instance.CurrentPhase == DayPhase.Morning
                              && !DayManager.Instance.IsGameOver;

                if (isForced)
                {
                    //scripted days (1-2): only show the forced choice button
                    PillChoice forced = PillStateManager.Instance.GetForcedChoice(currentDay);
                    string buttonLabel = forced == PillChoice.Taken
                        ? "TAKE Pill (scripted)"
                        : "REFUSE Pill (scripted)";

                    GUI.enabled = canChoose;
                    if (GUILayout.Button(buttonLabel))
                    {
                        PillStateManager.Instance.TakePill(forced);
                    }
                    GUI.enabled = true;
                    GUILayout.Label($"(Day {currentDay}: choice is scripted)");
                }
                else
                {
                    //free choice days (3-5): show both Take and Refuse buttons
                    GUI.enabled = canChoose;
                    if (GUILayout.Button("TAKE Pill"))
                    {
                        PillStateManager.Instance.TakePill(PillChoice.Taken);
                    }
                    if (GUILayout.Button("REFUSE Pill"))
                    {
                        PillStateManager.Instance.TakePill(PillChoice.NotTaken);
                    }
                    GUI.enabled = true;
                }

                GUILayout.Space(5);

                //SAVE/LOAD/DELETE

                if (GUILayout.Button("Save Game"))
                {
                    SaveManager.Instance.SaveGame();
                }
                if (GUILayout.Button("Load Game"))
                {
                    SaveManager.Instance.LoadGame();
                }
                if (GUILayout.Button("Delete Save"))
                {
                    SaveManager.Instance.DeleteSave();
                }

                GUILayout.Space(5);
                // TASK SYSTEM TESTING
                // shows task completion status and buttons to spawn/complete tasks
                GUILayout.Label("--- Task System ---", GUI.skin.box);
                if (TaskManager.Instance != null && TaskManager.Instance.IsInitialized)
                {
                    bool taskDone = TaskManager.Instance.IsTaskCompletedToday;
                    GUILayout.Label($"Task Today: {(taskDone ? "COMPLETED" : "Not Done")}");

                    string taskHistory = "";
                    for (int d = 1; d <= 5; d++)
                    {
                        string status = TaskManager.Instance.IsTaskCompleted(d) ? "D" : "-";
                        taskHistory += $"D{d}:{status}  ";
                    }
                    GUILayout.Label(taskHistory);

                    bool canSpawnTask = !DayManager.Instance.IsGameOver
                                     && DayManager.Instance.CurrentPhase == DayPhase.Morning
                                     && PillStateManager.Instance.HasTakenPillToday()
                                     && !taskDone
                                     && TaskManager.Instance.ActiveTask == null;

                    GUI.enabled = canSpawnTask;
                    if (GUILayout.Button("Spawn Task"))
                    {
                        TaskManager.Instance.SpawnTask();
                    }
                    GUI.enabled = true;

                    bool canComplete = !DayManager.Instance.IsGameOver
                                    && DayManager.Instance.CurrentPhase == DayPhase.Morning
                                    && PillStateManager.Instance.HasTakenPillToday()
                                    && !taskDone;

                    GUI.enabled = canComplete;
                    if (GUILayout.Button("Complete Task (instant)"))
                    {
                        TaskManager.Instance.CompleteCurrentTask();
                    }
                    GUI.enabled = true;
                }
                else
                {
                    GUILayout.Label("TaskManager not available");
                }

                GUILayout.Space(5);

                // HIDDEN ROOM TESTING
                // shows room states (L=locked, D=discovered, E=entered)
                // buttons to discover and enter rooms for testing
                GUILayout.Label("--- Hidden Rooms ---", GUI.skin.box);
                if (HiddenRoomManager.Instance != null && HiddenRoomManager.Instance.IsInitialized)
                {
                    string[] roomIds = HiddenRoomManager.Instance.GetAllRoomIds();
                    string roomStatus = "";
                    foreach (string id in roomIds)
                    {
                        DoorState state = HiddenRoomManager.Instance.GetDoorState(id);
                        string stateChar = state == DoorState.Locked ? "L" :
                                           state == DoorState.Discovered ? "D" : "E";
                        roomStatus += $"{id}:{stateChar}  ";
                    }
                    GUILayout.Label(roomStatus);
                    GUILayout.Label("(L=Locked, D=Discovered, E=Entered)");

                    bool isNight = DayManager.Instance.CurrentPhase == DayPhase.Night;
                    bool isOffPill = PillStateManager.Instance.GetPillChoice(currentDay) == PillChoice.NotTaken;
                    bool taskComplete = TaskManager.Instance != null && TaskManager.Instance.IsTaskCompleted(currentDay);
                    bool canAccessHiddenRooms = isNight && isOffPill && taskComplete;

                    string validationStatus = $"Access: {(canAccessHiddenRooms ? "ALLOWED" : "BLOCKED")}";
                    if (!canAccessHiddenRooms)
                    {
                        validationStatus += " (";
                        if (!isNight) validationStatus += "!Night ";
                        if (!isOffPill) validationStatus += "!OffPill ";
                        if (!taskComplete) validationStatus += "!TaskDone";
                        validationStatus += ")";
                    }
                    GUILayout.Label(validationStatus);

                    bool canTestDiscover = isNight && !DayManager.Instance.IsGameOver;
                    GUI.enabled = canTestDiscover;
                    if (GUILayout.Button("Discover Next Room (test)"))
                    {
                        string[] allRooms = HiddenRoomManager.Instance.GetAllRoomIds();
                        foreach (string id in allRooms)
                        {
                            DoorState state = HiddenRoomManager.Instance.GetDoorState(id);
                            RoomType type = HiddenRoomManager.Instance.GetRoomType(id);

                            if (state == DoorState.Locked && type == RoomType.Hidden)
                            {
                                HiddenRoomManager.Instance.DiscoverRoom(id);
                                break;
                            }
                        }
                    }
                    GUI.enabled = true;

                    GUI.enabled = canTestDiscover;
                    if (GUILayout.Button("Enter First Discovered Room"))
                    {
                        string[] allRooms = HiddenRoomManager.Instance.GetAllRoomIds();

                        string roomToEnter = null;
                        foreach (string id in allRooms)
                        {
                            DoorState state = HiddenRoomManager.Instance.GetDoorState(id);
                            if (state == DoorState.Discovered)
                            {
                                roomToEnter = id;
                                break;
                            }
                        }

                        if (roomToEnter == null)
                        {
                            foreach (string id in allRooms)
                            {
                                DoorState state = HiddenRoomManager.Instance.GetDoorState(id);
                                if (state == DoorState.Entered)
                                {
                                    roomToEnter = id;
                                    break;
                                }
                            }
                        }

                        if (roomToEnter != null)
                        {
                            HiddenRoomManager.Instance.EnterRoom(roomToEnter);
                        }
                    }
                    GUI.enabled = true;
                }
                else
                {
                    GUILayout.Label("HiddenRoomManager not available");
                }

                GUILayout.Space(5);
                // PUZZLE SYSTEM TESTING
                // shows puzzle completion status and buttons to spawn/complete puzzles
                GUILayout.Label("--- Puzzle System ---", GUI.skin.box);
                if (PuzzleManager.Instance != null && PuzzleManager.Instance.IsInitialized)
                {
                    string puzzleId = $"puzzle_day_{currentDay}";
                    bool isPuzzleCompleted = PuzzleManager.Instance.IsPuzzleCompleted(puzzleId);
                    GUILayout.Label($"Day {currentDay} Puzzle: {(isPuzzleCompleted ? "COMPLETED" : "Not Done")}");

                    string puzzleHistory = "";
                    for (int d = 1; d <= 5; d++)
                    {
                        string pId = $"puzzle_day_{d}";
                        string status = PuzzleManager.Instance.IsPuzzleCompleted(pId) ? "D" : "-";
                        puzzleHistory += $"D{d}:{status}  ";
                    }
                    GUILayout.Label(puzzleHistory);

                    PillChoice spawnPillChoice = PillStateManager.Instance.GetPillChoice(currentDay);
                    bool isOffPillForSpawn = spawnPillChoice == PillChoice.NotTaken;
                    bool canSpawnPuzzle = !DayManager.Instance.IsGameOver
                                       && DayManager.Instance.CurrentPhase == DayPhase.Night
                                       && !isPuzzleCompleted
                                       && PuzzleManager.Instance.ActivePuzzle == null
                                       && isOffPillForSpawn;

                    GUI.enabled = canSpawnPuzzle;
                    if (GUILayout.Button("Spawn Puzzle"))
                    {
                        Debug.Log($"[MANAGERTESTER] Manually spawning puzzle for Day {currentDay}");
                        string roomToEnter = $"room_{currentDay - 1}";
                        HiddenRoomManager.Instance.EnterRoom(roomToEnter);
                    }
                    GUI.enabled = true;

                    PillChoice currentPillChoice = PillStateManager.Instance.GetPillChoice(currentDay);
                    bool isOffPillDay = currentPillChoice == PillChoice.NotTaken;

                    bool hasEnteredRoomThisNight = false;
                    if (HiddenRoomManager.Instance != null && HiddenRoomManager.Instance.IsInitialized)
                    {
                        hasEnteredRoomThisNight = HiddenRoomManager.Instance.HasEnteredRoomThisNight();
                    }

                    bool canCompletePuzzle = !DayManager.Instance.IsGameOver
                                           && DayManager.Instance.CurrentPhase == DayPhase.Night
                                           && !isPuzzleCompleted
                                           && isOffPillDay
                                           && hasEnteredRoomThisNight;

                    GUI.enabled = canCompletePuzzle;
                    if (GUILayout.Button("Complete Puzzle (instant)"))
                    {
                        PuzzleManager.Instance.CompletePuzzle(puzzleId);
                        Debug.Log($"[MANAGERTESTER] Manually completed puzzle: {puzzleId}");
                    }
                    GUI.enabled = true;
                }
                else
                {
                    GUILayout.Label("PuzzleManager not available");
                }

                GUILayout.Space(5);

                //SETTINGS + TABLET TOGGLE
                GUILayout.Label("--- Settings ---", GUI.skin.box);

                GUILayout.Label($"Volume: {SettingsManager.Instance.MasterVolume:F2}");
                if (GUILayout.Button("Volume +0.1"))
                {
                    float newVolume = SettingsManager.Instance.MasterVolume + 0.1f;
                    SettingsManager.Instance.SetMasterVolume(newVolume);
                }
                if (GUILayout.Button("Volume -0.1"))
                {
                    float newVolume = SettingsManager.Instance.MasterVolume - 0.1f;
                    SettingsManager.Instance.SetMasterVolume(newVolume);
                }
                GUILayout.Space(5);

                if (TabletUIController.Instance != null && TabletUIController.Instance.IsInitialized)
                {
                    if (GUILayout.Button(TabletUIController.Instance.IsVisible ? "Close Tablet" : "Open Tablet"))
                    {
                        TabletUIController.Instance.ToggleTablet();
                    }
                }
            }
            else
            {
                GUILayout.Label("Managers not initialized yet...");
            }

            GUILayout.EndArea();
        }
    }
}