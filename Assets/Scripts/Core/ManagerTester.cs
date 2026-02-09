using UnityEngine;

namespace SUNSET16.Core
{
    public class ManagerTester : MonoBehaviour
    {
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 420, 700));

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
            }
            else
            {
                GUILayout.Label("Managers not initialized yet...");
            }

            GUILayout.EndArea();
        }
    }
}