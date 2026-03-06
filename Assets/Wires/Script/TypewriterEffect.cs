using UnityEngine;
using TMPro;
using System.Collections;

public class TypewriterEffect : MonoBehaviour
{
    public TMP_Text textComponent;
    public float typingSpeed = 0.05f;

    [Header("Typing Sound")]
    [SerializeField] private AudioSource typingAudioSource; // assign in Inspector
    [SerializeField] private AudioClip typingLoopClip;      // short looping clip

    private string fullText;

    void Start()
    {
        fullText = textComponent.text;
        textComponent.maxVisibleCharacters = 0;
        StartCoroutine(TypeText());
    }

    IEnumerator TypeText()
    {
        StartTypingSound();

        for (int i = 0; i <= fullText.Length; i++)
        {
            textComponent.maxVisibleCharacters = i;
            yield return new WaitForSeconds(typingSpeed);
        }

        StopTypingSound();
    }

    private void StartTypingSound()
    {
        if (typingAudioSource == null || typingLoopClip == null) return;

        typingAudioSource.clip = typingLoopClip;
        typingAudioSource.loop = true;
        if (!typingAudioSource.isPlaying)
            typingAudioSource.Play();
    }

    private void StopTypingSound()
    {
        if (typingAudioSource == null) return;

        typingAudioSource.loop = false;
        typingAudioSource.Stop();
        typingAudioSource.clip = null;
    }

    private void OnDisable()
    {
        // safety: if the object is turned off mid-typing
        StopTypingSound();
    }
}