using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the CRT scan line effect on the ComputerCanvas overlay.
/// Spawns a single orange horizontal line that travels from top to bottom
/// at randomised intervals. A new line never starts until the previous one
/// has fully exited the bottom of the screen.
/// </summary>
public class CRTScrollingLineController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RawImage _rawImage;

    [Header("Line Movement")]
    [Tooltip("How fast the line travels from top to bottom. 0.1 = slow, 0.5 = fast.")]
    [SerializeField] private float _lineSpeed = 0.2f;

    [Header("Spawn Timing")]
    [Tooltip("How many seconds between each spawn probability check.")]
    [SerializeField] private float _checkInterval = 5f;

    [Tooltip("Probability (0–1) that a line spawns when the interval check fires. 0.5 = 50% chance.")]
    [SerializeField][Range(0f, 1f)] private float _spawnChance = 0.6f;

    // -----------------------------------------------------------------------
    private Material _mat;
    private bool     _lineActive;
    private float    _lineY;
    private float    _timer;

    // -----------------------------------------------------------------------
    private void Awake()
    {
        // Instance the material so we don't modify the shared asset in the project
        if (_rawImage != null)
            _mat = _rawImage.material = new Material(_rawImage.material);
    }

    private void OnEnable()
    {
        _lineActive = false;
        _lineY      = 1f;
        _timer      = _checkInterval; // fire a check immediately on first enable
        if (_mat != null)
            _mat.SetFloat("_LineActive", 0f);
    }

    private void Update()
    {
        if (_lineActive)
        {
            // Move line downward — Y goes from 1 (top) to 0 (bottom) in screen UV space
            _lineY -= _lineSpeed * Time.deltaTime;
            _mat.SetFloat("_LineY", _lineY);

            if (_lineY <= 0f)
            {
                // Line has exited — deactivate and start the interval timer fresh
                _lineActive = false;
                _mat.SetFloat("_LineActive", 0f);
                _timer = 0f;
            }
        }
        else
        {
            _timer += Time.deltaTime;

            if (_timer >= _checkInterval)
            {
                _timer = 0f;

                if (Random.value <= _spawnChance)
                    SpawnLine();
                // If check fails, timer resets and we wait another interval
            }
        }
    }

    private void SpawnLine()
    {
        _lineY      = 1f;
        _lineActive = true;
        _mat.SetFloat("_LineY",     _lineY);
        _mat.SetFloat("_LineActive", 1f);
    }

    private void OnDisable()
    {
        // Always hide the line when the overlay closes
        if (_mat != null)
            _mat.SetFloat("_LineActive", 0f);
    }
}
