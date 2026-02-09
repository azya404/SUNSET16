using UnityEditor;
using UnityEditor.SceneManagement;

namespace SUNSET16.Core.Editor
{
    [InitializeOnLoad]
    public static class EditorStartScene
    {
        private const string CORE_SCENE_PATH = "Assets/Scenes/CoreScene.unity";

        static EditorStartScene()
        {
            SceneAsset coreScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(CORE_SCENE_PATH);

            if (coreScene != null)
            {
                EditorSceneManager.playModeStartScene = coreScene;
            }
            else
            {
                EditorSceneManager.playModeStartScene = null;
                UnityEngine.Debug.LogWarning(
                    $"[EditorStartScene] CoreScene not found at '{CORE_SCENE_PATH}'. " +
                    "Play Mode will use the currently open scene. " +
                    "Create CoreScene or update CORE_SCENE_PATH if it was moved."
                );
            }
        }
    }
}