using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Bend_Wire_Manager : MonoBehaviour, IPointerClickHandler
{
    public Image img;
    public Sprite bend_red_wire;
    public Sprite bend_blue_wire;
    public Sprite bend_yellow_wire;
    public Sprite bend_purple_wire;
    public Sprite bend_white_wire;
    public Sprite bend_black_wire;
    public Slot slot;
    //public Sprite purple_wire;
    // Start is called before the first frame update
    
    private void Awake()
    {
        img = GetComponent<Image>();
    }

    public void ColorWire(Slot slotWireColor)
    {
        if (slotWireColor != null && slotWireColor.wire_id == "Red")
        {
            img.sprite = bend_red_wire;
        }
        else if (slotWireColor != null && slotWireColor.wire_id == "Blue")
        {
            img.sprite = bend_blue_wire;
        }
        else if (slotWireColor != null && slotWireColor.wire_id == "Yellow")
        {
            img.sprite = bend_yellow_wire;
        }
        else if (slotWireColor != null && slotWireColor.wire_id == "Purple")
        {
            img.sprite = bend_purple_wire;
        }
        else if (slotWireColor != null && slotWireColor.wire_id == "White")
        {
            img.sprite = bend_white_wire;
        }
        else if (slotWireColor != null && slotWireColor.wire_id == "Black")
        {
            img.sprite = bend_black_wire;
        }
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Only allow deleting if this slot is a node (or has a wire_id)
            
            string colorId = null;
            if (slot.wire_fulled != false) colorId = slot.node_id;
            
            else if (slot.wire_fulled) colorId = slot.wire_id;

            if (!string.IsNullOrEmpty(colorId))
            {
                FindObjectOfType<Level_Manager>().delete_wire(colorId);
            }
        }   
    }
}
