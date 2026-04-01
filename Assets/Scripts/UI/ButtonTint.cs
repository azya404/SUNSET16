using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SUNSET16.Core;
using SUNSET16.UI;

public class ButtonHoverTint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image targetImage;

    public Color normalColor = Color.white;
    public Color hoverColor = new Color(0.7059f, 0.7059f, 0.7059f, 1f);

    [SerializeField] private bool isChat;
    [SerializeField] private bool isLore;
    [SerializeField] private bool isNext;
    [SerializeField] private bool isPrev;
    [SerializeField] private bool isOther;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!DialogueUIManager.Instance.clickDisabled && ((isChat && !DialogueUIManager.Instance.chatOpen) || (isLore && DialogueUIManager.Instance.chatOpen) || (isNext && !DialogueUIManager.Instance.nextDisabled) || (isPrev && !DialogueUIManager.Instance.prevDisabled) || isOther))
            targetImage.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!DialogueUIManager.Instance.clickDisabled && ((isChat && !DialogueUIManager.Instance.chatOpen) || (isLore && DialogueUIManager.Instance.chatOpen) || (isNext && !DialogueUIManager.Instance.nextDisabled) || (isPrev && !DialogueUIManager.Instance.prevDisabled) || isOther))
            targetImage.color = normalColor;
    }
}