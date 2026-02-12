using UnityEditor;
using UnityEditor.SceneManagement;

namespace SUNSET16.Core.Editor
{
    [InitializeOnLoad]
    public static class EditorStartScene
    {
        private const string START_SCENE_PATH = "Assets/Scenes/MainMenuScene.unity";

        static EditorStartScene()
        {
            SceneAsset startScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(START_SCENE_PATH);

            if (startScene != null)
            {
                EditorSceneManager.playModeStartScene = startScene;
            }
            else
            {
                EditorSceneManager.playModeStartScene = null;
                UnityEngine.Debug.LogWarning(
                    $"[EditorStartScene] MainMenuScene not found at '{START_SCENE_PATH}'. " +
                    "Play Mode will use the currently open scene. " +
                    "Create MainMenuScene or update START_SCENE_PATH if it was moved."
                );
            }
        }
    }
}