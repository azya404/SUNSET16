using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Enables / disables the CRT Barrel Warp Fullscreen Pass Renderer Feature
/// when the ComputerInteraction overlay opens and closes Frame 2.
///
/// Attach this to the ComputerInteract GO in BedroomScene.
/// Wire up via ComputerInteraction.cs — call SetWarpActive(true/false)
/// when Frame 2 opens and closes.
/// </summary>
public class CRTBarrelWarpController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag your Universal Renderer Data asset here (Assets/Settings/...)")]
    [SerializeField] private UniversalRendererData _rendererData;

    [Tooltip("Must match the Name field you give the Fullscreen Pass Renderer Feature exactly")]
    [SerializeField] private string _featureName = "CRT Barrel Warp";

    private ScriptableRendererFeature _feature;

    // -----------------------------------------------------------------------

    private void Awake()
    {
        if (_rendererData == null)
        {
            Debug.LogWarning("[CRTBarrelWarpController] No RendererData assigned.");
            return;
        }

        foreach (var feature in _rendererData.rendererFeatures)
        {
            if (feature.name == _featureName)
            {
                _feature = feature;
                break;
            }
        }

        if (_feature == null)
            Debug.LogWarning($"[CRTBarrelWarpController] Renderer feature '{_featureName}' not found.");

        // Always start disabled
        SetWarpActive(false);
    }

    // -----------------------------------------------------------------------

    /// <summary>Call with true when Frame 2 opens, false when it closes.</summary>
    public void SetWarpActive(bool active)
    {
        if (_feature == null) return;
        _feature.SetActive(active);

        // Force the renderer to rebuild so the change takes effect immediately
        _rendererData.SetDirty();
    }
}
