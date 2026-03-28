using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Light_Image : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image lightImage;
    [SerializeField] private RectTransform lightRectTransform;

    [Header("Sway")]
    [SerializeField] private float swayDistance = 12f;
    [SerializeField] private float swaySpeed = 1.5f;

    [Header("Alpha (0 to 255)")]
    [SerializeField] [Range(0, 255)] private int normalAlpha = 7;
    [SerializeField] [Range(0, 255)] private int warmupAlpha = 12;
    [SerializeField] [Range(0, 255)] private int successAlpha = 20;
    [SerializeField] [Range(0, 255)] private int failureAlpha = 0;
    [SerializeField] [Range(0, 255)] private int minimumAlpha = 7;
    [SerializeField] private float failureOffHoldTime = 0.75f;

private bool allowBelowMinimumAlpha;
private bool freezeFlicker;

    [Header("Timing")]
    [SerializeField] private float totalTime = 9f;
    [SerializeField] private float branchTime = 2.9f;

    [Header("Flicker")]
    [SerializeField] private bool flickerEnabled = true;
    [SerializeField] [Range(0, 20)] private int flickerAmount = 2;
    [SerializeField] private float flickerIntervalMin = 0.03f;
    [SerializeField] private float flickerIntervalMax = 0.12f;

    private Vector2 startAnchoredPosition;
    private float baseAlpha;
    private float flickerOffset;
    private float flickerTimer;
    private float nextFlickerTime;
    private Coroutine resultCoroutine;

    private void Reset()
    {
        lightImage = GetComponent<Image>();
        lightRectTransform = GetComponent<RectTransform>();
    }

    private void Awake()
    {
        if (lightImage == null)
        {
            lightImage = GetComponent<Image>();
        }

        if (lightRectTransform == null)
        {
            lightRectTransform = GetComponent<RectTransform>();
        }

        if (lightRectTransform != null)
        {
            startAnchoredPosition = lightRectTransform.anchoredPosition;
        }

        baseAlpha = normalAlpha;
        flickerOffset = 0f;
        set_alpha_byte(Mathf.RoundToInt(baseAlpha));
        set_next_flicker_time();
    }

    private void OnValidate()
    {
        if (branchTime > totalTime)
        {
            branchTime = totalTime;
        }

        if (flickerIntervalMin < 0.01f)
        {
            flickerIntervalMin = 0.01f;
        }

        if (flickerIntervalMax < flickerIntervalMin)
        {
            flickerIntervalMax = flickerIntervalMin;
        }
    }

    private void Update()
    {
        update_sway();
        update_flicker();
        apply_alpha();
    }

    private void update_sway()
    {
        if (lightRectTransform == null)
        {
            return;
        }

        float xOffset = Mathf.Sin(Time.time * swaySpeed) * swayDistance;
        lightRectTransform.anchoredPosition = new Vector2(
            startAnchoredPosition.x + xOffset,
            startAnchoredPosition.y
        );
    }

    // private void update_flicker()
    // {
    //     if (!flickerEnabled)
    //     {
    //         flickerOffset = 0f;
    //         return;
    //     }

    //     flickerTimer += Time.deltaTime;

    //     if (flickerTimer >= nextFlickerTime)
    //     {
    //         flickerTimer = 0f;
    //         flickerOffset = Random.Range(-flickerAmount, flickerAmount + 1);
    //         set_next_flicker_time();
    //     }
    // }
    private void update_flicker()
    {
        if (!flickerEnabled || freezeFlicker)
        {
            flickerOffset = 0f;
            return;
        }

        flickerTimer += Time.deltaTime;

        if (flickerTimer >= nextFlickerTime)
        {
            flickerTimer = 0f;
            flickerOffset = Random.Range(-flickerAmount, flickerAmount + 1);
            set_next_flicker_time();
        }
    }
    private void set_next_flicker_time()
    {
        nextFlickerTime = Random.Range(flickerIntervalMin, flickerIntervalMax);
    }

    // private void apply_alpha()
    // {
    //     int finalAlpha = Mathf.RoundToInt(baseAlpha + flickerOffset);
    //     finalAlpha = Mathf.Clamp(finalAlpha, minimumAlpha, successAlpha);
    //     set_alpha_byte(finalAlpha);
    // }
    // private void apply_alpha()
    // {
    //     int finalAlpha = Mathf.RoundToInt(baseAlpha + flickerOffset);

    //     int minimumClamp = minimumAlpha;
    //     if (allowBelowMinimumAlpha)
    //     {
    //         minimumClamp = 0;
    //     }

    //     finalAlpha = Mathf.Clamp(finalAlpha, minimumClamp, successAlpha);
    //     set_alpha_byte(finalAlpha);
    // }
    private void apply_alpha()
    {
        if (freezeFlicker && baseAlpha <= 0f)
        {
            set_alpha_byte(0);
            return;
        }

        int finalAlpha = Mathf.RoundToInt(baseAlpha + flickerOffset);

        int minimumClamp = minimumAlpha;
        if (allowBelowMinimumAlpha)
        {
            minimumClamp = 0;
        }

        finalAlpha = Mathf.Clamp(finalAlpha, minimumClamp, successAlpha);
        set_alpha_byte(finalAlpha);
    }

    private void set_alpha_byte(int alphaValue)
    {
        if (lightImage == null)
        {
            return;
        }

        alphaValue = Mathf.Clamp(alphaValue, 0, 255);

        Color32 imageColor = lightImage.color;
        imageColor.a = (byte)alphaValue;
        lightImage.color = imageColor;
    }

    public void play_success()
    {
        start_result_sequence(true);
    }

    public void play_failure()
    {
        start_result_sequence(false);
    }

    // public void reset_light()
    // {
    //     if (resultCoroutine != null)
    //     {
    //         StopCoroutine(resultCoroutine);
    //         resultCoroutine = null;
    //     }

    //     baseAlpha = normalAlpha;
    // }
    public void reset_light()
    {
        if (resultCoroutine != null)
        {
            StopCoroutine(resultCoroutine);
            resultCoroutine = null;
        }

        allowBelowMinimumAlpha = false;
        freezeFlicker = false;
        flickerOffset = 0f;
        baseAlpha = normalAlpha;
    }

    private void start_result_sequence(bool wasSuccessful)
    {
        if (resultCoroutine != null)
        {
            StopCoroutine(resultCoroutine);
        }

        resultCoroutine = StartCoroutine(result_sequence(wasSuccessful));
    }

    private IEnumerator result_sequence(bool wasSuccessful)
{
    float elapsedTime = 0f;
    float startAlpha = baseAlpha;

    allowBelowMinimumAlpha = false;
    freezeFlicker = false;

    while (elapsedTime < totalTime)
    {
        elapsedTime += Time.deltaTime;

        if (elapsedTime <= branchTime)
        {
            float progress = elapsedTime / branchTime;
            baseAlpha = Mathf.Lerp(startAlpha, warmupAlpha, progress);
        }
        else
        {
            float secondPartLength = totalTime - branchTime;
            float secondPartTime = elapsedTime - branchTime;

            if (wasSuccessful)
            {
                float secondPartProgress = 0f;

                if (secondPartLength > 0f)
                {
                    secondPartProgress = secondPartTime / secondPartLength;
                }

                baseAlpha = Mathf.Lerp(warmupAlpha, successAlpha, secondPartProgress);
            }
            else
            {
                allowBelowMinimumAlpha = true;

                float usableLength = secondPartLength - failureOffHoldTime;
                if (usableLength < 0.02f)
                {
                    usableLength = 0.02f;
                }

                float fadeOutLength = usableLength * 0.5f;
                float fadeInLength = usableLength * 0.5f;

                if (secondPartTime < fadeOutLength)
                {
                    freezeFlicker = false;
                    float fadeOutProgress = secondPartTime / fadeOutLength;
                    baseAlpha = Mathf.Lerp(warmupAlpha, 0f, fadeOutProgress);
                }
                else if (secondPartTime < fadeOutLength + failureOffHoldTime)
                {
                    freezeFlicker = true;
                    baseAlpha = 0f;
                    flickerOffset = 0f;
                    set_alpha_byte(0);
                }
                else
                {
                    freezeFlicker = false;

                    float fadeInTime = secondPartTime - fadeOutLength - failureOffHoldTime;
                    float fadeInProgress = fadeInTime / fadeInLength;
                    baseAlpha = Mathf.Lerp(0f, normalAlpha, fadeInProgress);
                }
            }
        }

        yield return null;
    }

    if (wasSuccessful)
    {
        baseAlpha = successAlpha;
    }
    else
    {
        baseAlpha = normalAlpha;
    }

    allowBelowMinimumAlpha = false;
    freezeFlicker = false;
    flickerOffset = 0f;
    resultCoroutine = null;
}
    // private IEnumerator result_sequence(bool wasSuccessful)
    // {
    //     float elapsedTime = 0f;
    //     float startAlpha = baseAlpha;

    //     while (elapsedTime < totalTime)
    //     {
    //         elapsedTime += Time.deltaTime;

    //         if (elapsedTime <= branchTime)
    //         {
    //             float progress = elapsedTime / branchTime;
    //             baseAlpha = Mathf.Lerp(startAlpha, warmupAlpha, progress);
    //         }
    //         else
    //         {
    //             float secondPartLength = totalTime - branchTime;
    //             float secondPartTime = elapsedTime - branchTime;
    //             float secondPartProgress = 0f;

    //             if (secondPartLength > 0f)
    //             {
    //                 secondPartProgress = secondPartTime / secondPartLength;
    //             }

    //             if (wasSuccessful)
    //             {
    //                 baseAlpha = Mathf.Lerp(warmupAlpha, successAlpha, secondPartProgress);
    //             }
    //             else
    //             {
    //                 if (secondPartProgress < 0.5f)
    //                 {
    //                     float dimProgress = secondPartProgress / 0.5f;
    //                     baseAlpha = Mathf.Lerp(warmupAlpha, failureAlpha, dimProgress);
    //                 }
    //                 else
    //                 {
    //                     float returnProgress = (secondPartProgress - 0.5f) / 0.5f;
    //                     baseAlpha = Mathf.Lerp(failureAlpha, normalAlpha, returnProgress);
    //                 }
    //             }
    //         }

    //         yield return null;
    //     }

    //     if (wasSuccessful)
    //     {
    //         baseAlpha = successAlpha;
    //     }
    //     else
    //     {
    //         baseAlpha = normalAlpha;
    //     }

    //     resultCoroutine = null;
    // }
}
// public class Light_Image : MonoBehaviour

// {
//     [Header("References")]
//     [SerializeField] private Image lightImage;
//     [SerializeField] private RectTransform lightRectTransform;

//     [Header("Sway")]
//     [SerializeField] private float swayDistance = 12f;
//     [SerializeField] private float swaySpeed = 1.5f;

//     [Header("Alpha")]
//     [SerializeField] [Range(0f, 1f)] private float normalAlpha = 0.0275f;   // about 7 / 255
//     [SerializeField] [Range(0f, 1f)] private float warmupAlpha = 0.0500f;   // soft build-up
//     [SerializeField] [Range(0f, 1f)] private float successAlpha = 0.0784f;  // about 20 / 255
//     [SerializeField] [Range(0f, 1f)] private float failureAlpha = 0.0150f;  // dim but still visible

//     [Header("Timing")]
//     [SerializeField] private float totalTime = 5f;
//     [SerializeField] private float branchTime = 2.9f;

//     [Header("Flicker")]
//     [SerializeField] private bool flickerEnabled = true;
//     [SerializeField] private float flickerAmount = 0.006f;
//     [SerializeField] private float flickerIntervalMin = 0.03f;
//     [SerializeField] private float flickerIntervalMax = 0.12f;

//     private Vector2 startAnchoredPosition;
//     private float baseAlpha;
//     private float flickerOffset;
//     private float flickerTimer;
//     private float nextFlickerTime;

//     private bool stayBright;
//     private Coroutine resultCoroutine;

//     private void Reset()
//     {
//         lightImage = GetComponent<Image>();
//         lightRectTransform = GetComponent<RectTransform>();
//     }

//     private void Awake()
//     {
//         if (lightImage == null)
//         {
//             lightImage = GetComponent<Image>();
//         }

//         if (lightRectTransform == null)
//         {
//             lightRectTransform = GetComponent<RectTransform>();
//         }

//         if (lightRectTransform != null)
//         {
//             startAnchoredPosition = lightRectTransform.anchoredPosition;
//         }

//         baseAlpha = normalAlpha;
//         flickerOffset = 0f;
//         set_alpha(baseAlpha);
//         set_next_flicker_time();
//     }

//     private void OnValidate()
//     {
//         if (branchTime > totalTime)
//         {
//             branchTime = totalTime;
//         }

//         if (flickerIntervalMin < 0.01f)
//         {
//             flickerIntervalMin = 0.01f;
//         }

//         if (flickerIntervalMax < flickerIntervalMin)
//         {
//             flickerIntervalMax = flickerIntervalMin;
//         }
//     }

//     private void Update()
//     {
//         update_sway();
//         update_flicker();
//         apply_alpha();
//     }

//     private void update_sway()
//     {
//         if (lightRectTransform == null)
//         {
//             return;
//         }

//         float xOffset = Mathf.Sin(Time.time * swaySpeed) * swayDistance;
//         lightRectTransform.anchoredPosition = new Vector2(
//             startAnchoredPosition.x + xOffset,
//             startAnchoredPosition.y
//         );
//     }

//     private void update_flicker()
//     {
//         if (!flickerEnabled)
//         {
//             flickerOffset = 0f;
//             return;
//         }

//         flickerTimer += Time.deltaTime;

//         if (flickerTimer >= nextFlickerTime)
//         {
//             flickerTimer = 0f;
//             flickerOffset = Random.Range(-flickerAmount, flickerAmount);
//             set_next_flicker_time();
//         }
//     }

//     private void set_next_flicker_time()
//     {
//         nextFlickerTime = Random.Range(flickerIntervalMin, flickerIntervalMax);
//     }

//     private void apply_alpha()
//     {
//         float finalAlpha = Mathf.Clamp01(baseAlpha + flickerOffset);
//         set_alpha(finalAlpha);
//     }

//     private void set_alpha(float alphaValue)
//     {
//         if (lightImage == null)
//         {
//             return;
//         }

//         Color imageColor = lightImage.color;
//         imageColor.a = alphaValue;
//         lightImage.color = imageColor;
//     }

//     public void play_success()
//     {
//         start_result_sequence(true);
//     }

//     public void play_failure()
//     {
//         start_result_sequence(false);
//     }

//     public void reset_light()
//     {
//         if (resultCoroutine != null)
//         {
//             StopCoroutine(resultCoroutine);
//             resultCoroutine = null;
//         }

//         stayBright = false;
//         baseAlpha = normalAlpha;
//     }

//     private void start_result_sequence(bool wasSuccessful)
//     {
//         if (resultCoroutine != null)
//         {
//             StopCoroutine(resultCoroutine);
//         }

//         resultCoroutine = StartCoroutine(result_sequence(wasSuccessful));
//     }

//     private IEnumerator result_sequence(bool wasSuccessful)
//     {
//         stayBright = false;

//         float elapsedTime = 0f;
//         float startAlpha = baseAlpha;

//         while (elapsedTime < totalTime)
//         {
//             elapsedTime += Time.deltaTime;

//             if (elapsedTime <= branchTime)
//             {
//                 float progress = elapsedTime / branchTime;
//                 baseAlpha = Mathf.Lerp(startAlpha, warmupAlpha, progress);
//             }
//             else
//             {
//                 float secondPartLength = totalTime - branchTime;
//                 float secondPartTime = elapsedTime - branchTime;
//                 float secondPartProgress = 0f;

//                 if (secondPartLength > 0f)
//                 {
//                     secondPartProgress = secondPartTime / secondPartLength;
//                 }

//                 if (wasSuccessful)
//                 {
//                     baseAlpha = Mathf.Lerp(warmupAlpha, successAlpha, secondPartProgress);
//                 }
//                 else
//                 {
//                     if (secondPartProgress < 0.5f)
//                     {
//                         float dimProgress = secondPartProgress / 0.5f;
//                         baseAlpha = Mathf.Lerp(warmupAlpha, failureAlpha, dimProgress);
//                     }
//                     else
//                     {
//                         float returnProgress = (secondPartProgress - 0.5f) / 0.5f;
//                         baseAlpha = Mathf.Lerp(failureAlpha, normalAlpha, returnProgress);
//                     }
//                 }
//             }

//             yield return null;
//         }

//         if (wasSuccessful)
//         {
//             stayBright = true;
//             baseAlpha = successAlpha;
//         }
//         else
//         {
//             stayBright = false;
//             baseAlpha = normalAlpha;
//         }

//         resultCoroutine = null;
//     }
// }