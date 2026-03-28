using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using Unity.VisualScripting;
using System.Linq;

public class Slot : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    //IPointerEnterHandler,
    IPointerClickHandler
{
    public Image img;       
    public Sprite purple_node;
    public Sprite red_node;
    public Sprite yellow_node;
    public Sprite blue_node;
    public Sprite white_node;
    public Sprite black_node;
    public Sprite empty_node;
    public Transform wire_spot;   // drag the "Wire_spot" GameObject here in Inspector
    public TMP_Text Text_Output;
    public Slot up_slot;
    public Slot down_slot;
    public Slot left_slot;
    public Slot right_slot;
    //public string attachednode = null;
    public string node_id = null;
    public Player_Drag Draging;
    public GameObject wire_prefab;
    public UnityEngine.Vector3 offset_positon;
    public int offset_rotate;
    public bool node_fulled = false;
    public bool wire_fulled = false;
    public bool slot_fulled = false; //used when check side slots
    public bool has_bend = false;
    public Level_Manager level;
    public string wire_id; 
    public static Slot selectedSlot = null; 
    public bool IsFull => node_fulled || wire_fulled; //if the slot is full of any of those then it cant be added

    public bool selection_click = false, second_click = false;

    
    public void update_slot_visual()
    {
        if (node_id == "Red") img.sprite = red_node;
        else if (node_id == "Purple") img.sprite = purple_node;
        else if (node_id == "White") img.sprite = white_node;
        else if (node_id == "Black") img.sprite = black_node;
        else if (node_id == "Blue") img.sprite = blue_node;
        else if (node_id == "Yellow") img.sprite = yellow_node;
        Debug.Log("wires_upated");
    }
    
    private string GetConnectionIdFromThisOrOther(Slot otherSlot)
    {
        // Prefer THIS slot as the source (so extending from a wire works naturally)
        if (node_fulled == true) return node_id; //works from node → empty
        if (wire_fulled == true && string.IsNullOrEmpty(wire_id) == false) return wire_id; //works from wire → empty

        // Fallback: maybe the other slot is the source
        if (otherSlot != null && otherSlot.node_fulled == true) return otherSlot.node_id; // works from node → wire only if same color
        if (otherSlot != null && otherSlot.wire_fulled == true && string.IsNullOrEmpty(otherSlot.wire_id) == false) return otherSlot.wire_id; // works from wire → wire only if same color

        return null;
    }
    private Slot lastProcessedSlot = null;

    private void Update()
    {
        if (level.leverBusy == true) return;
        else {
        if (Draging == null) return;
        if (!Draging.gameObject.activeSelf) return;

        Slot hovered = Draging.currentHoverSlot;
        if (hovered == null) return;

        // Only react when the hovered slot CHANGES (prevents spam)
        if (hovered == lastProcessedSlot) return;
        lastProcessedSlot = hovered;

        // Only place wires when we hover THIS slot
        if (hovered != this) return;

        // Now call your existing neighbor-placement logic:
        TryPlaceFromSelectedNeighbor();
        }
    }
    public void place_wire(Slot otherSlot, float rotationZ, Vector3 offset)
    {
        if (otherSlot == null) return;

        if (wire_prefab == null)
        {
            Debug.LogWarning("place_wire: wire_prefab not assigned on " + name);
            return;
        }

        if (wire_spot == null)
        {
            Debug.LogWarning("place_wire: wire_spot not assigned on " + name);
            return;
        }

        string connectionId = GetConnectionIdFromThisOrOther(otherSlot);
        if (string.IsNullOrEmpty(connectionId)) return;

        if (wire_fulled && !string.IsNullOrEmpty(wire_id) && wire_id != connectionId) return;
        if (otherSlot.wire_fulled && !string.IsNullOrEmpty(otherSlot.wire_id) && otherSlot.wire_id != connectionId) return;

        wire_fulled = true;
        otherSlot.wire_fulled = true;
        wire_id = connectionId;
        otherSlot.wire_id = connectionId;

        Quaternion localRotation = Quaternion.Euler(0f, 0f, rotationZ);

        GameObject clone = Instantiate(wire_prefab, wire_spot);
        clone.name = wire_id;
        level.action_wire_stack.Push(clone);

        RectTransform cloneRect = clone.GetComponent<RectTransform>();

        if (cloneRect != null)
        {
            cloneRect.localScale = Vector3.one;
            cloneRect.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
            cloneRect.localPosition = offset;
        }
        else
        {
            clone.transform.localScale = Vector3.one;
            clone.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
            clone.transform.localPosition = offset;
        }

        Wire_Manager script = clone.GetComponent<Wire_Manager>();
        if (script == null) script = clone.GetComponentInChildren<Wire_Manager>();

        if (script != null)
        {
            script.connection_one = this;
            script.connection_two = otherSlot;
            script.ColorWire(this);
        }
    }
            
    public void OnPointerDown(PointerEventData eventData) //holding click
        {
            if (level.leverBusy == true) return;
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (node_fulled && !wire_fulled)
                {
                    Draging.begin();
                    selection_click = true;
                    Debug.Log("Click");
                    level.action_stack.Add(this);
                }
            }
    }
    public void OnPointerUp(PointerEventData eventData) //when they let go of click
        {
            if (Draging.is_dragging == true) {
                if (Draging != null) Draging.end();
                Debug.Log("Stop Click");
                selection_click = false;

                // Only re-check and delete for THIS slot's color (less spam)
                if (level == null) return;

                if (wire_id == "Red")
                {
                    level.check_connected(level.red_node_1, "Red", level.red_node_2);
                }
                else if (wire_id == "Yellow")
                {
                    level.check_connected(level.yellow_node_1, "Yellow", level.yellow_node_2);
                }
                else if (wire_id == "Blue")
                {
                    level.check_connected(level.blue_node_1, "Blue", level.blue_node_2);
                }
                else if (wire_id == "Purple")
                {
                    level.check_connected(level.purple_node_1, "Purple", level.purple_node_2);
                }
                else if (wire_id == "White")
                {
                    level.check_connected(level.white_node_1, "White", level.white_node_2);
                }
                else if (wire_id == "Black")
                {
                    level.check_connected(level.black_node_1, "Black", level.black_node_2);
                }
                else
                {
                    level.action_stack.Clear();
                }
            }
    }

    
    public void cursor_off()
    {
        Slot s = level.action_stack[level.action_stack.Count - 1];
        
        if (Draging != null) Draging.end();
        Debug.Log("Stop Click");
        s.selection_click = false;

        // Only re-check and delete for THIS slot's color (less spam)
        if (level == null) return;

        if (s.wire_id == "Red")
        {
            level.check_connected(level.red_node_1, "Red", level.red_node_2);
        }
        else if (s.wire_id == "Yellow")
        {
            level.check_connected(level.yellow_node_1, "Yellow", level.yellow_node_2);
        }
        else if (s.wire_id == "Blue")
        {
            level.check_connected(level.blue_node_1, "Blue", level.blue_node_2);
        }
        else if (wire_id == "Purple")
        {
            level.check_connected(level.purple_node_1, "Purple", level.purple_node_2);
        }
        else if (wire_id == "White")
        {
            level.check_connected(level.white_node_1, "White", level.white_node_2);
        }
        else if (wire_id == "Black")
        {
            level.check_connected(level.black_node_1, "Black", level.black_node_2);
        }
    }
    private Vector3 GetHalfwayOffset(Slot otherSlot){
        if (otherSlot == null || wire_spot == null)
        {
            return Vector3.zero;
        }

        Vector3 thisWorldPosition = transform.position;
        Vector3 otherWorldPosition = otherSlot.transform.position;

        Vector3 middleWorldPosition = (thisWorldPosition + otherWorldPosition) * 0.5f;

        return wire_spot.InverseTransformPoint(middleWorldPosition);
    }
        private void TryPlaceFromSelectedNeighbor()
        {

            // only do this if we are currently dragging
            if (Draging == null) return;
            if (!Draging.gameObject.activeSelf) return; // begin() sets active true, end() sets false

            // Neighbor second click. Checks if the slot is there and where the first click is!
            if ((up_slot != null && up_slot.selection_click) ||
                (down_slot != null && down_slot.selection_click) ||
                (right_slot != null && right_slot.selection_click) ||
                (left_slot != null && left_slot.selection_click))
            {
                
                if (level.action_stack.Count == 2 && level != null) level.place_wire_sound(); //sound from starting Node
                
                if (this == level.peek())
                {   
                    Debug.Log("this ran");
                    Slot removed = level.action_stack_remove_last();
                    removed.selection_click = false;
                    removed.wire_fulled = false;
                    removed.wire_id = null;
                    removed.has_bend = false;
                    if (has_bend == true) {
                        has_bend = false;
                        level.back_wire_bend();
                    }
                    selection_click = true;
                    level.connent_right_animation();
                }
                // first click cell is UP (meaning: the selected slot is below me)
                if (down_slot != null && down_slot.selection_click)
                {
                    if (!wire_fulled && !node_fulled)
                    {
                        Debug.Log("Place wire");
                        if (Text_Output != null) Text_Output.text = "Place wire";

                        offset_positon = GetHalfwayOffset(down_slot);
                        offset_rotate = 90;

                        level.action_stack.Add(this);
                        place_wire(down_slot, offset_rotate, offset_positon);
                        level.is_wire_bent();
                        down_slot.selection_click = false;
                        selection_click = true;
                        level.connent_right_animation();
                    }
                    else if (node_fulled && wire_fulled)
                    {
                        Debug.Log("Wire spot full");
                        if (Text_Output != null) Text_Output.text = "Wire spot full";
                        cursor_off();
                        level.connent_wrong_animation();
                    }
                    else if (node_fulled && node_id == down_slot.wire_id)
                    {
                        Debug.Log("Place wire On Node");
                        if (Text_Output != null) Text_Output.text = "fulled node";

                        offset_positon = GetHalfwayOffset(down_slot);
                        offset_rotate = 90;

                        level.action_stack.Add(this);
                        place_wire(down_slot, offset_rotate, offset_positon);
                        level.is_wire_bent();
                        cursor_off();
                        down_slot.selection_click = false;
                        selection_click = false;
                        level.connent_node_animation();
                    }
                    else
                    {
                        Debug.Log("spot is full");
                        if (Text_Output != null) Text_Output.text = "spot is full";
                        cursor_off();
                        level.connent_wrong_animation();
                    }
                }

                // first click cell is DOWN (selected slot is above me)
                if (up_slot != null && up_slot.selection_click)
                {
                    if (!wire_fulled && !node_fulled)
                    {
                        Debug.Log("Place wire");
                        if (Text_Output != null) Text_Output.text = "Place wire";

                        offset_positon = GetHalfwayOffset(up_slot);
                        offset_rotate = 90;

                        level.action_stack.Add(this);
                        place_wire(up_slot, offset_rotate, offset_positon);
                        level.is_wire_bent();
                        up_slot.selection_click = false;
                        selection_click = true;
                        level.connent_right_animation();
                    }
                    else if (node_fulled && wire_fulled)
                    {
                        Debug.Log("Wire spot full");
                        if (Text_Output != null) Text_Output.text = "Wire spot full";
                        cursor_off();
                        level.connent_wrong_animation();
                    }
                    else if (node_fulled && node_id == up_slot.wire_id)
                    {
                        Debug.Log("Place wire On Node");
                        if (Text_Output != null) Text_Output.text = "Place wire";

                        offset_positon = GetHalfwayOffset(up_slot);
                        offset_rotate = 90;

                        level.action_stack.Add(this);
                        place_wire(up_slot, offset_rotate, offset_positon);
                        level.is_wire_bent();
                        cursor_off();
                        up_slot.selection_click = false;
                        selection_click = false;
                        level.connent_node_animation();
                    }
                    else
                    {
                        Debug.Log("spot is full");
                        if (Text_Output != null) Text_Output.text = "spot is full";
                        cursor_off();
                        level.connent_wrong_animation();
                    }
                }

                // first click cell is LEFT (selected slot is to my right)
                if (right_slot != null && right_slot.selection_click)
                {
                    if (!wire_fulled && !node_fulled)
                    {
                        Debug.Log("Place wire");
                        if (Text_Output != null) Text_Output.text = "Place wire";

                        offset_positon = GetHalfwayOffset(right_slot);
                        offset_rotate = 0;

                        level.action_stack.Add(this);
                        place_wire(right_slot, offset_rotate, offset_positon);
                        level.is_wire_bent();
                        right_slot.selection_click = false;
                        selection_click = true;
                        level.connent_right_animation();
                    }
                    else if (node_fulled && wire_fulled)
                    {
                        Debug.Log("Wire spot full");
                        if (Text_Output != null) Text_Output.text = "Wire spot full";
                        cursor_off();
                        level.connent_wrong_animation();
                    }
                    else if (node_fulled && node_id == right_slot.wire_id)
                    {
                        Debug.Log("Place wire On Node");
                        if (Text_Output != null) Text_Output.text = "Place wire";

                        offset_positon = GetHalfwayOffset(right_slot);
                        offset_rotate = 0;

                        level.action_stack.Add(this);
                        place_wire(right_slot, offset_rotate, offset_positon);
                        level.is_wire_bent();
                        cursor_off();
                        right_slot.selection_click = false;
                        selection_click = false;
                        level.connent_node_animation();
                    }
                    else
                    {
                        Debug.Log("spot is full");
                        if (Text_Output != null) Text_Output.text = "spot is full";
                        cursor_off();
                        level.connent_wrong_animation();
                    }
                }

                // first click cell is RIGHT (selected slot is to my left)
                if (left_slot != null && left_slot.selection_click)
                {
                    if (!wire_fulled && !node_fulled)
                    {
                        Debug.Log("Place wire");
                        if (Text_Output != null) Text_Output.text = "Place wire";

                        offset_positon = GetHalfwayOffset(left_slot);
                        offset_rotate = 180;

                        level.action_stack.Add(this);
                        place_wire(left_slot, offset_rotate, offset_positon);
                        level.is_wire_bent();
                        left_slot.selection_click = false;
                        selection_click = true;
                        level.connent_right_animation();
                    }
                    else if (node_fulled && wire_fulled)
                    {
                        Debug.Log("Wire spot full");
                        if (Text_Output != null) Text_Output.text = "Wire spot full";
                        cursor_off();
                        level.connent_wrong_animation();
                    }
                    else if (node_fulled && node_id == left_slot.wire_id)
                    {
                        Debug.Log("Place wire On Node");
                        if (Text_Output != null) Text_Output.text = "Place wire";

                        offset_positon = GetHalfwayOffset(left_slot);
                        offset_rotate = 180;

                        level.action_stack.Add(this);
                        place_wire(left_slot, offset_rotate, offset_positon);
                        level.is_wire_bent();
                        cursor_off();
                        left_slot.selection_click = false;
                        selection_click = false;
                        level.connent_node_animation();
                    }
                    else
                    {
                        Debug.Log("spot is full");
                        if (Text_Output != null) Text_Output.text = "spot is full";
                        cursor_off();
                        level.connent_wrong_animation();
                    }
                }
            }
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (level.leverBusy == true) return;
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Only allow deleting if this slot is a node (or has a wire_id)
            string colorId = null;
            if (node_fulled == true) colorId = node_id;
            else if (wire_fulled) colorId = wire_id;

            if (!string.IsNullOrEmpty(colorId))
            {
                FindObjectOfType<Level_Manager>().delete_wire(colorId);
            }
        }   
    }
}