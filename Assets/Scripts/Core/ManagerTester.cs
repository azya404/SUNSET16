using UnityEngine;

namespace SUNSET16.Core
{
    public class ManagerTester : MonoBehaviour
    {
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 400, 600));

            GUILayout.Label("=== SUNSET16 Manager Tester ===", GUI.skin.box);

            if (GameManager.Instance != null && GameManager.Instance.IsInitialized)
            {
                GUILayout.Label($"Day: {DayManager.Instance.CurrentDay}");
                GUILayout.Label($"Phase: {DayManager.Instance.CurrentPhase}");
                GUILayout.Label($"Pills Taken: {PillStateManager.Instance.GetPillsTakenCount()}");
                GUILayout.Label($"Pills Refused: {PillStateManager.Instance.GetPillsRefusedCount()}");
                GUILayout.Label($"Chose Today: {PillStateManager.Instance.HasTakenPillToday()}");
                GUILayout.Label($"Volume: {SettingsManager.Instance.MasterVolume:F2}");

                GUILayout.Label($"Save Exists: {SaveManager.Instance.SaveExists}");

                GUILayout.Space(10);

                if (GUILayout.Button("Advance Phase"))
                {
                    DayManager.Instance.AdvancePhase();
                }

                GUILayout.Space(5);
                if (GUILayout.Button("TAKE Pill"))
                {
                    PillStateManager.Instance.TakePill(PillChoice.Taken);
                }

                if (GUILayout.Button("REFUSE Pill"))
                {
                    PillStateManager.Instance.TakePill(PillChoice.NotTaken);
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
                if (GUILayout.Button("Determine Ending"))
                {
                    string ending = PillStateManager.Instance.DetermineEnding();
                    Debug.Log($"Current Ending: {ending}");
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