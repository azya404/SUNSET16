using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Lever : MonoBehaviour, IPointerClickHandler
{
    public Image img;       
    public Sprite spriteOn;
    public Sprite spriteOff;
    public int Lives = 4;
    [SerializeField] private AudioSource audio_lever_click;
    [SerializeField] private AudioClip clip_lever_click;
    public BrightnessFlicker bright;

    public Level1_Manager level;

    public void OnPointerClick(PointerEventData eventData)
    {

        if (img.sprite == spriteOff && level.leverBusy == false)
        {
            img.sprite = spriteOn;
            audio_lever_click.PlayOneShot(clip_lever_click);
            level.lever_click();
            bright.StartFlicker();
            Debug.Log(Lives);
        }
    }

    public void pull_back()
    {
        img.sprite = spriteOff;
        audio_lever_click.PlayOneShot(clip_lever_click);
    }

    void Start()
    {
        if (img == null) img = GetComponent<Image>();
        if (img != null && spriteOff != null) img.sprite = spriteOff;
    }
}
