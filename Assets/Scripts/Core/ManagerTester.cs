using UnityEngine;

namespace SUNSET16.Core
{
    public class ManagerTester : MonoBehaviour
    {
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 420, 1000));

            GUILayout.Label("=== SUNSET16 Manager Tester ===", GUI.skin.box);

            if (GameManager.Instance != null && GameManager.Instance.IsInitialized)
            {
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
                string history = "";
                for (int d = 1; d <= 5; d++)
                {
                    PillChoice c = PillStateManager.Instance.GetPillChoice(d);
                    string label = c == PillChoice.Taken ? "P" :
                                   c == PillChoice.NotTaken ? "N" : "-";
                    string forced = d <= 2 ? "*" : " ";
                    history += $"D{d}{forced}:{label}  ";
                }
                GUILayout.Label(history); 

                GUILayout.Label($"Ending: {PillStateManager.Instance.DetermineEnding()}");

                GUILayout.Space(10);

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

                GUI.enabled = canAdvance;
                if (GUILayout.Button("Advance Phase"))
                {
                    DayManager.Instance.AdvancePhase();
                }
                GUI.enabled = true;

                GUILayout.Space(5);
                bool canChoose = !PillStateManager.Instance.HasTakenPillToday()
                              && DayManager.Instance.CurrentPhase == DayPhase.Morning
                              && !DayManager.Instance.IsGameOver;

                if (isForced)
                {
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
                        foreach (string id in allRooms)
                        {
                            DoorState state = HiddenRoomManager.Instance.GetDoorState(id);
                            if (state == DoorState.Discovered || state == DoorState.Entered)
                            {
                                HiddenRoomManager.Instance.EnterRoom(id);
                                break;
                            }
                        }
                    }
                    GUI.enabled = true;
                }
                else
                {
                    GUILayout.Label("HiddenRoomManager not available");
                }

                GUILayout.Space(5);
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
                    bool canSpawnPuzzle = !DayManager.Instance.IsGameOver
                                       && DayManager.Instance.CurrentPhase == DayPhase.Night
                                       && !isPuzzleCompleted
                                       && PuzzleManager.Instance.ActivePuzzle == null;

                    GUI.enabled = canSpawnPuzzle;
                    if (GUILayout.Button("Spawn Puzzle"))
                    {
                        Debug.Log($"[MANAGERTESTER] Manually spawning puzzle for Day {currentDay}");
                        string roomToEnter = $"room_{currentDay - 1}";
                        HiddenRoomManager.Instance.EnterRoom(roomToEnter);
                    }
                    GUI.enabled = true;

                    bool canCompletePuzzle = !DayManager.Instance.IsGameOver
                                           && DayManager.Instance.CurrentPhase == DayPhase.Night
                                           && !isPuzzleCompleted;

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