/*
persistent hotbar that lives in CoreScene alongside HUDController and DOLOSManager
*/
using UnityEngine;
using UnityEngine.UI;
using SUNSET16.Core;

namespace SUNSET16.UI
{
    public class InteractionHotbarController : Singleton<InteractionHotbarController>
    {
        [Header("Hotbar Visuals")]
        [SerializeField] private Image characterSprite;

        protected override void Awake()
        {
            base.Awake();
            // character sprite is always on - no alpha, no animation, no logic needed
            // just confirm the ref is wired so we catch missing Inspector assignments early
            if (characterSprite == null)
                Debug.LogError("[HOTBAR] characterSprite is not assigned in the Inspector.");
        }
    }
}
