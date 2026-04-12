using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HazardAnimation : MonoBehaviour
{
    public SpriteRenderer target_sprite;
    public float fade_duration = 3f;

    private void Update()
    {
        if (target_sprite == null)
        {
            return;
        }

        float time_value = Mathf.PingPong(Time.time, fade_duration);
        float progress = time_value / fade_duration;

        byte alpha_value = (byte)Mathf.Lerp(50f, 255f, progress);

        Color32 current_color = target_sprite.color;
        current_color.a = alpha_value;
        target_sprite.color = current_color;
    }
}