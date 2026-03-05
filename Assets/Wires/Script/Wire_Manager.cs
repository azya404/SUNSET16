using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Wire_Manager : MonoBehaviour, IPointerClickHandler
{
    public Image img;
    public Sprite red_wire;
    public Sprite blue_wire;
    public Sprite yellow_wire;
    public Sprite green_wire;
    public Slot connection_one;
    public Slot connection_two;
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
            img.sprite = red_wire;
        }
        else if (slotWireColor != null && slotWireColor.wire_id == "Blue")
        {
            img.sprite = blue_wire;
        }
        else if (slotWireColor != null && slotWireColor.wire_id == "Yellow")
        {
            img.sprite = yellow_wire;
        }
        else if (slotWireColor != null && slotWireColor.wire_id == "Green")
        {
            img.sprite = green_wire;
        }
        else if (slotWireColor != null && slotWireColor.wire_id == "Purple")
        {
            //img.sprite = purple_wire;
        }
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Only allow deleting if this slot is a node (or has a wire_id)
            
            string colorId = null;
            if (connection_one.attachednode != null) colorId = connection_one.attachednode.id;
            else if (connection_one.wire_fulled) colorId = connection_one.wire_id;

            if (!string.IsNullOrEmpty(colorId))
            {
                FindObjectOfType<Level1_Manager>().delete_wire(colorId);
            }
        }   
    }
}
