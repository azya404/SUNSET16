/*
THE BOUNCER - editor utility that forces Unity Play Mode to always start from MainMenuScene
this ensures that when you hit Play in the editor, you always start from the main menu
regardless of which scene you have open (super useful during development)

HOW IT WORKS:
- [InitializeOnLoad] makes Unity run the static constructor when the editor loads
- the static constructor runs ONCE when Unity opens or when scripts recompile
- it sets EditorSceneManager.playModeStartScene to the MainMenuScene asset
- if MainMenuScene doesnt exist at the path, it clears the setting (uses current scene)

WHY THIS MATTERS:
- without this, if youre editing the Bedroom scene and hit Play, it starts in Bedroom
  but the managers arent initialized so everything breaks
- with this, Play always goes MainMenu -> CoreScene -> proper init chain
- the warning message is helpful: tells you the scene wasnt found and what to do

THIS IS EDITOR-ONLY CODE:
- uses UnityEditor namespace (does NOT compile in builds)
- lives in the Editor folder which Unity excludes from runtime builds automatically
- static class with no instances (just the static constructor)

NAMESPACE: SUNSET16.Core.Editor
*/
using UnityEditor;
using UnityEditor.SceneManagement;

namespace SUNSET16.Core.Editor
{
    [InitializeOnLoad]
    public static class EditorStartScene
    {
        //this path must match the actual location of your MainMenuScene in the project
        private const string START_SCENE_PATH = "Assets/Scenes/MainMenuScene.unity";

        //static constructor - Unity runs this automatically when editor loads or scripts recompile
        //theres no Start() or Awake() cos this isnt a MonoBehaviour - its editor-only
        static EditorStartScene()
        {
            //try to find the MainMenuScene asset at the expected path
            SceneAsset startScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(START_SCENE_PATH);

            if (startScene != null)
            {
                //found it - force Play Mode to always start from this scene
                EditorSceneManager.playModeStartScene = startScene;
            }
            else
            {
                //scene not found - clear the setting so Play uses whatever scene is currently open
                //this is a graceful fallback rather than crashing
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