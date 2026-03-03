using UnityEngine;

namespace SUNSET16.Core
{
    /// <summary>
    /// A single DOLOS ship-wide announcement (text + optional audio).
    /// DOLOS is one-directional: player cannot respond.
    /// Assign via: SUNSET16 > DOLOS Announcement.
    /// </summary>
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
