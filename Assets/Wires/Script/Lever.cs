using System.Collections;
using System.Collections.Generic;
using SUNSET16.Core;
using SUNSET16.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Lever : MonoBehaviour, IPointerClickHandler
{
    public Light_Image resultLight;
    public Image img;       
    public Sprite spriteOn;
    public Sprite spriteOff;
    public int Lives = 4;
    [SerializeField] private AudioSource audio_lever_click;
    [SerializeField] private AudioClip clip_lever_click;
    [SerializeField] private AudioSource audioSource_success;
    [SerializeField] private AudioClip clip_success;
    [SerializeField] private AudioSource audioSource_fail;
    [SerializeField] private AudioClip clip_fail;
    [SerializeField] private AudioSource audioSource_zap;
    [SerializeField] private AudioClip clip_zap;
    [SerializeField] private AudioSource audioSource_node_connection;
    [SerializeField] private AudioClip clip_node_connection;
    public Animator lever_animator;

    public Level_Manager level;


    private IEnumerator PlayAndWait(AudioClip clip, AudioSource audioSource)
        {
            if (audioSource == null || clip == null) yield break;

            audioSource.PlayOneShot(clip);

            // wait until it's done
            yield return new WaitForSeconds(clip.length);

            // code here runs AFTER sound finishes
            Debug.Log("Sound finished!");
        }
    public void OnPointerClick(PointerEventData eventData)
    {

        if (level.leverBusy == false)
        {
            img.sprite = spriteOn;
            //audio_lever_click.PlayOneShot(clip_lever_click);
            level.lever_click();
            Debug.Log(Lives);
        }
    }


    void Start()
    {
        if (img == null) img = GetComponent<Image>();
        if (img != null && spriteOff != null) img.sprite = spriteOff;
    }


    public void lever_Right()
    {
        resultLight.play_success();
        lever_animator.Play("Lever_Done_Right");
        StartCoroutine(PlayRightSequence());
    }

    private IEnumerator PlayRightSequence()
    {
        yield return StartCoroutine(PlayAndWait(clip_success, audioSource_success));

        DOLOSManager.Instance.taskCompleteCount++;

        if (level.taskInteraction != null)
        {
            level.taskInteraction.CloseOverlay();
            DOLOSManager.Instance.TriggerAnnouncement();
        }
        if (level.puzzleInteraction != null)
        {
            level.puzzleInteraction.CloseOverlay();
            string id = "usb_log_" + PillStateManager.Instance.GetPillsRefusedCount();
            DialogueUIManager.Instance.UnlockEntry(id);
            if (RoomManager.Instance.GetCurrentRoomName().Contains("Crematorium"))
            {
                DialogueUIManager.Instance.UnlockEntry("usb_albert_death");
            }
        }
    }

        private IEnumerator PlayWrongSequence()
    {
        yield return StartCoroutine(PlayAndWait(clip_fail, audioSource_fail));
        yield return StartCoroutine(PlayAndWait(clip_lever_click, audio_lever_click));
        yield return level.leverBusy = false;
    }
    public void lever_Worng()
    {
        resultLight.play_failure();
        lever_animator.Play("Lever_Done_Wrong");
        StartCoroutine(PlayWrongSequence());
        //StartCoroutine(PlayAndWait(clip_lever_click, audio_lever_click));
        resultLight.reset_light();
    }

    public void Lever_Wrong_Connent()
    {
        lever_animator.Play("Lever_Wrong_Connect");
        StartCoroutine(PlayAndWait(clip_zap, audioSource_zap));
    }
    public void Lever_Node_Connect()
    {
        lever_animator.Play("Lever_Right_Connect");
        StartCoroutine(PlayAndWait(clip_node_connection, audioSource_node_connection));
    }

    public void Lever_Right_Connect()
    {
        lever_animator.Play("Lever_Connect");
    }
}
