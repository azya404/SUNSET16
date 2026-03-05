using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrightnessFlicker : MonoBehaviour
{
    public float durationSeconds = 5f;

    [Header("Brightness range")]
    public float minIntensity = 0.2f;
    public float maxIntensity = 1.2f;

    [Header("How fast it flickers")]
    public float minStepTime = 0.03f;
    public float maxStepTime = 0.12f;

    private Light lightComp;
    private float originalIntensity;
    private Coroutine routine;

    private void Awake()
    {
        lightComp = GetComponent<Light>();
        if (lightComp == null)
            Debug.LogError("BrightnessFlicker: No Light component on this GameObject.");
    }

    public void StartFlicker()
    {
        if (lightComp == null) return;

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(FlickerRoutine());
    }

    private IEnumerator FlickerRoutine()
    {
        originalIntensity = lightComp.intensity;

        float endTime = Time.time + durationSeconds;

        while (Time.time < endTime)
        {
            lightComp.intensity = Random.Range(minIntensity, maxIntensity);
            yield return new WaitForSeconds(Random.Range(minStepTime, maxStepTime));
        }

        lightComp.intensity = originalIntensity;
        routine = null;
    }

    private void OnDisable()
    {
        if (lightComp != null) lightComp.intensity = originalIntensity;
    }
}
