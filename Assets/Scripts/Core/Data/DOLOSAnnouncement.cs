/*
ScriptableObject for a single DOLOS ship PA announcement
has the text, an optional audio clip, and how long to display it

create these in the editor under SUNSET16/DOLOS Announcement
then drop them into DOLOSManager.TriggerAnnouncement() calls in whatever
script is scheduling the day's announcements

if audioClip is assigned, make sure displayDuration >= the clip length
otherwise the text disappears before the VO finishes
*/
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

        [Tooltip("Optional voice-over clip played alongside the text. Leave null for text-only.")]
        public AudioClip audioClip;

        [Tooltip("How long to display the text. If audioClip is assigned, set this >= audio length.")]
        public float displayDuration = 4f;
    }
}
