using UnityEngine;

/// <summary>
/// Controls the CRT Barrel Warp effect via a global shader property.
/// The Fullscreen Pass Renderer Feature must stay ALWAYS ENABLED in Renderer2D.
/// Injection Point must be set to "After Rendering".
/// This script tells the shader when to warp (overlay open) vs pass through.
///
/// Attached to ComputerInteract GO. Called by ComputerInteraction.cs.
/// </summary>
public class CRTBarrelWarpController : MonoBehaviour
{
    private static readonly int WarpActiveID = Shader.PropertyToID("_CRTWarpActive");

    private void Awake()
    {
        Shader.SetGlobalFloat(WarpActiveID, 0f);
    }

    private void OnDisable()
    {
        Shader.SetGlobalFloat(WarpActiveID, 0f);
    }

    /// <summary>Call with true when Frame 2 opens, false when it closes.</summary>
    public void SetWarpActive(bool active)
    {
        Shader.SetGlobalFloat(WarpActiveID, active ? 1f : 0f);
    }
}
