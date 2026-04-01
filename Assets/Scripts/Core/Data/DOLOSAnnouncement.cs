/*
ScriptableObject for a single DOLOS ship PA announcement
has the text, an optional audio clip, and how long to display it

create these in the editor under SUNSET16/DOLOS Announcement
then drop them into DOLOSManager.TriggerAnnouncement() calls in whatever
script is scheduling the day's announcements

if audioClip is assigned, make sure displayDuration >= the clip length
otherwise the text disappears before the VO finishes
*/
using System.Collections.Generic;
using UnityEngine;

namespace SUNSET16.Core
{
    [CreateAssetMenu(fileName = "NewDOLOSAnnouncement", menuName = "SUNSET16/DOLOS Announcement")]
    public class DOLOSAnnouncement : ScriptableObject
    {
        [Tooltip("Unique ID for this announcement (e.g. 'dolos_day1_morning').")]
        public string announcementId;

        [Tooltip("Text shown on screen during the announcement.")]
        [TextArea(2, 5)]
        public string text;

        [Tooltip("Size of the text shown on screen during the announcement.")]
        public int fontSize;

        [Tooltip("Optional voice-over clip played alongside the text. Leave null for text-only.")]
        public AudioClip audioClip;

        [Tooltip("How long to display the text. If audioClip is assigned, set this >= audio length.")]
        public float displayDuration = 4f;

        [Tooltip("How long to display each line. If audioClip is assigned, set this roughly equal to the audio length for each line.")]
        public List<int> lineDurations;

        [Tooltip("How loud the given voiceline is. It's a range between 0.0 and 10.0 where 0.1 is 10%, 1.0 is 100%, and 10.0 is 1000%")]
        [Range(0.0f, 10.0f)] public float volume = 1.0f;
    }
}
