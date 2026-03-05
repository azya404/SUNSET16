using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using Unity.VisualScripting;

public class Slot : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    //IPointerEnterHandler,
    IPointerClickHandler
{

    public Transform wire_spot;   // drag the "Wire_spot" GameObject here in Inspector
    public TMP_Text Text_Output;
    public Slot up_slot;
    public Slot down_slot;
    public Slot left_slot;
    public Slot right_slot;
    public Node attachednode;
    public Player_Drag Draging;
    public GameObject wire_prefab;
    public UnityEngine.Vector3  offset_positon;
    public int offset_rotate;
    public bool node_fulled = false;
    public bool wire_fulled = false;
    public bool slot_fulled = false; //used when check side slots
    public Level1_Manager level;




    public string wire_id; 

    public static Slot selectedSlot = null; 
    public bool IsFull => node_fulled || wire_fulled; //if the slot is full of any of those then it cant be added

    public bool selection_click = false, second_click = false;

    private string GetConnectionIdFromThisOrOther(Slot otherSlot)
    {
        // Prefer THIS slot as the source (so extending from a wire works naturally)
        if (attachednode != null) return attachednode.id; //works from node → empty
        if (wire_fulled == true && string.IsNullOrEmpty(wire_id) == false) return wire_id; //works from wire → empty

        // Fallback: maybe the other slot is the source
        if (otherSlot != null && otherSlot.attachednode != null) return otherSlot.attachednode.id; // works from node → wire only if same color
        if (otherSlot != null && otherSlot.wire_fulled == true && string.IsNullOrEmpty(otherSlot.wire_id) == false) return otherSlot.wire_id; // works from wire → wire only if same color

        return null;
    }
    private Slot lastProcessedSlot = null;

    private void Update()
    {
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

    // public void place_wire(Slot otherSlot, float rotationZ, Vector3 offset)
    // {
    //     if (otherSlot == null) return;

    //     string connectionId = GetConnectionIdFromThisOrOther(otherSlot);
    //     if (string.IsNullOrEmpty(connectionId)) return;

    //     // mark data
    //     wire_fulled = true;
    //     otherSlot.wire_fulled = true;
    //     wire_id = connectionId;
    //     otherSlot.wire_id = connectionId;

    //     if (wire_prefab == null)
    //     {
    //         Debug.LogWarning("wire_prefab not assigned on " + name);
    //         return;
    //     }

    //     // spawn in WORLD space
    //     Vector3 spawnPos = transform.position + offset;
    //     Quaternion spawnRot = Quaternion.Euler(0f, 0f, rotationZ);

    //     GameObject clone = Instantiate(wire_prefab, spawnPos, spawnRot);

    //     // (optional) name it for deleting later
    //     clone.name = "Wire_" + connectionId + "_" + name + "_to_" + otherSlot.name;

    //     // call function on clone (ONLY if Wire_Manager is on the prefab)
    //     Wire_Manager script = clone.GetComponent<Wire_Manager>();
    //     if (script != null)
    //     {
    //         script.ColorWire(otherSlot);
    //     }
    // }

    // public void place_wire(Slot otherSlot, float rotationZ, Vector3 offset)
    // {
    //     if (otherSlot == null)
    //     {
    //         Debug.LogWarning("place_wire: otherSlot is null");
    //         if (Text_Output != null) Text_Output.text = "place_wire: otherSlot is null";
    //         return;
    //     }

    //     if (wire_prefab == null)
    //     {
    //         Debug.LogWarning("place_wire: wire_prefab not assigned on " + name);
    //         if (Text_Output != null) Text_Output.text = "wire_prefab missing on " + name;
    //         return;
    //     }

    //     // Decide what color/id this connection should be (node OR existing wire)
    //     string connectionId = GetConnectionIdFromThisOrOther(otherSlot);
    //     if (string.IsNullOrEmpty(connectionId))
    //     {
    //         Debug.LogWarning("place_wire: no node/wire id to extend from");
    //         if (Text_Output != null) Text_Output.text = "No wire/node id to extend from";
    //         return;
    //     }

    //     // If this slot already has a wire id, it must match
    //     if (wire_fulled && !string.IsNullOrEmpty(wire_id) && wire_id != connectionId)
    //     {
    //         Debug.Log("place_wire: this slot has different wire color, can't connect.");
    //         if (Text_Output != null) Text_Output.text = "Different wire color (this slot)";
    //         return;
    //     }

    //     // If the other slot already has a wire id, it must match
    //     if (otherSlot.wire_fulled && !string.IsNullOrEmpty(otherSlot.wire_id) && otherSlot.wire_id != connectionId)
    //     {
    //         Debug.Log("place_wire: other slot has different wire color, can't connect.");
    //         if (Text_Output != null) Text_Output.text = "Different wire color (other slot)";
    //         return;
    //     }

    //     // Mark both ends as wire-filled and stamp the id
    //     wire_fulled = true;
    //     otherSlot.wire_fulled = true;
    //     wire_id = connectionId;
    //     otherSlot.wire_id = connectionId;

    //     // Spawn in WORLD space
    //     Vector3 spawnPos = transform.position + offset;
    //     Quaternion spawnRot = Quaternion.Euler(0f, 0f, rotationZ);

    //     GameObject clone = Instantiate(wire_prefab, spawnPos, spawnRot);
    //     clone.name = "Wire_" + connectionId + "_" + name + "_to_" + otherSlot.name;

    //     // Get Wire_Manager (root OR child)
    //     Wire_Manager script = clone.GetComponent<Wire_Manager>();
    //     if (script == null) script = clone.GetComponentInChildren<Wire_Manager>();

    //     if (script != null)
    //     {
    //         // Better to use the id you already computed
    //         // If your Wire_Manager has a wire_id field, do this:
    //         // script.wire_id = connectionId;

    //         // If ColorWire takes a Slot, pass THIS slot (source) or whichever you want.
    //         // But the most reliable is to make ColorWire take the string id.
    //         script.ColorWire(this);  // <-- change to script.ColorWire(connectionId) if you update Wire_Manager
    //     }
    //     else
    //     {
    //         Debug.LogWarning("place_wire: clone has no Wire_Manager component.");
    //     }
    // }
        
        
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
                Debug.LogWarning("place_wire: wire_spot not assigned on " + name + " (drag Wire_spot into the slot)");
                return;
            }

            string connectionId = GetConnectionIdFromThisOrOther(otherSlot);
            if (string.IsNullOrEmpty(connectionId)) return;

            // If this slot already has a wire id, it must match
            if (wire_fulled && !string.IsNullOrEmpty(wire_id) && wire_id != connectionId) return;

            // If the other slot already has a wire id, it must match
            if (otherSlot.wire_fulled && !string.IsNullOrEmpty(otherSlot.wire_id) && otherSlot.wire_id != connectionId) return;

            // mark data
            wire_fulled = true;
            otherSlot.wire_fulled = true;
            wire_id = connectionId;
            otherSlot.wire_id = connectionId;

            Vector3 spawnPos = transform.position + offset;
            Quaternion spawnRot = Quaternion.Euler(0f, 0f, rotationZ);

            // parent under Wire_spot when instantiating
            GameObject clone = Instantiate(wire_prefab, spawnPos, spawnRot, wire_spot);

            clone.name = wire_id;

            Wire_Manager script = clone.GetComponent<Wire_Manager>();
            script.connection_one = this;
            script.connection_two = otherSlot;
            if (script == null) script = clone.GetComponentInChildren<Wire_Manager>();

            if (script != null)
            {
                script.ColorWire(this); // or better: script.ColorWire(connectionId) if you update Wire_Manager
            }
        }
        // private void OnMouseDown() 
        //     {
        //         if (node_fulled == true && wire_fulled == false){
        //             Draging.begin();
        //             selection_click = true;
        //             Debug.Log("Click");
        //         }
        //     }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            if (node_fulled && !wire_fulled)
            {
                Draging.begin();
                selection_click = true;
                Debug.Log("Click");
            }
        }

        // private void OnMouseUp() {
        //     Draging.end();
        //     Debug.Log("Stop Click");
        //     selection_click = false;
        //     // Red
        //     level.red_done = level.check_connected(level.red_node_1, "Red", level.red_node_2);
        //     if (!level.red_done && wire_id == "Red")
        //     {
        //         level.delete_wire("Red");
        //     }

        //     // Yellow
        //     level.yellow_done = level.check_connected(level.yellow_node_1, "Yellow", level.yellow_node_2);
        //     if (!level.yellow_done && wire_id == "Yellow")
        //     {
        //         level.delete_wire("Yellow");
        //     }

        //     // Blue
        //     level.blue_done = level.check_connected(level.blue_node_1, "Blue", level.blue_node_2);
        //     if (!level.blue_done && wire_id == "Blue")
        //     {
        //         level.delete_wire("Blue");
        //     }

        //     // Green
        //     level.green_done = level.check_connected(level.green_node_1, "Green", level.green_node_2);
        //     if (!level.green_done && wire_id == "Green")
        //     {
        //         level.delete_wire("Green");
        //     }
                    
        // }
public void OnPointerUp(PointerEventData eventData)
    {
        if (Draging != null) Draging.end();
        Debug.Log("Stop Click");
        selection_click = false;

        // Only re-check and delete for THIS slot's color (less spam)
        if (level == null) return;

        if (wire_id == "Red")
        {
            level.red_done = level.check_connected(level.red_node_1, "Red", level.red_node_2);
            if (!level.red_done) level.delete_wire("Red");
        }
        else if (wire_id == "Yellow")
        {
            level.yellow_done = level.check_connected(level.yellow_node_1, "Yellow", level.yellow_node_2);
            if (!level.yellow_done) level.delete_wire("Yellow");
        }
        else if (wire_id == "Blue")
        {
            level.blue_done = level.check_connected(level.blue_node_1, "Blue", level.blue_node_2);
            if (!level.blue_done) level.delete_wire("Blue");
        }
        else if (wire_id == "Green")
        {
            level.green_done = level.check_connected(level.green_node_1, "Green", level.green_node_2);
            if (!level.green_done) level.delete_wire("Green");
        }
    }

    // private void OnCollisionEnter2D(Collision2D collision)
    // {
    //     //Neighbor second click. Checks if the slot is there and where the first click is!
    //     if (up_slot != null && up_slot.selection_click == true || down_slot != null && down_slot.selection_click == true || right_slot != null && right_slot.selection_click == true || left_slot != null && left_slot.selection_click == true)
    //     {
    //         // first click cell is UP
    //         if (down_slot != null && down_slot.selection_click == true)
    //         {
    //             if (wire_fulled == false && node_fulled == false)
    //             {
    //                 Debug.Log("Place wire");
    //                 Text_Output.text = "Place wire";
    //                 offset_positon = new UnityEngine.Vector3(0f, -37f, 0f);
    //                 offset_rotate = 90;
    //                 level.place_wire_sound();
    //                 place_wire(down_slot, offset_rotate, offset_positon);
    //                 down_slot.selection_click = false;
    //                 selection_click = true;
    //             }
    //             else if (node_fulled == true && wire_fulled == true)
    //             {
    //                 Debug.Log("Wire spot full");
    //                 Text_Output.text = "Wire spot full";
    //                 Draging.end();
    //             }
    //             else if (node_fulled == true && attachednode.id == down_slot.wire_id){
    //                 Debug.Log("fulled node");
    //                 Text_Output.text = "fulled node";
    //                 offset_positon = new UnityEngine.Vector3(0f, -37f, 0f);
    //                 offset_rotate = 90;
    //                 level.place_wire_sound();
    //                 place_wire(down_slot, offset_rotate, offset_positon);
    //                 Draging.end();
    //                 down_slot.selection_click = false;
    //                 selection_click = false;
    //             }
    //             else
    //             {
    //                 Debug.Log("spot is full");
    //                 Text_Output.text = "spot is full";
    //                 Draging.end();
    //             }

    //         }

    //         // first click cell is DOWN
    //         if (up_slot != null && up_slot.selection_click == true)
    //         {
    //             //first check if slot has a wire or a node, secd checks if the there is a node there and the wire id is the same (same wire)
    //             if (wire_fulled == false && node_fulled == false) 
    //             {
    //                 Debug.Log("Place wire");
    //                 Text_Output.text = "Place wire";
    //                 offset_positon = new UnityEngine.Vector3(0f,37f, 0f);
    //                 offset_rotate = -90;
    //                 level.place_wire_sound();
    //                 place_wire(up_slot, offset_rotate, offset_positon);
    //                 up_slot.selection_click = false;
    //                 selection_click = true;
    //             }
    //             else if (node_fulled == true && wire_fulled == true)
    //             {
    //                 Debug.Log("Wire spot full");
    //                 Text_Output.text = "Wire spot full";
    //                 Draging.end();
    //             }
    //             else if (node_fulled == true && attachednode.id == up_slot.wire_id){
    //                 Debug.Log("Place wire");
    //                 Text_Output.text = "Place awire";
    //                 offset_positon = new UnityEngine.Vector3(0f,37f, 0f);
    //                 offset_rotate = -90;
    //                 level.place_wire_sound();
    //                 place_wire(up_slot, offset_rotate, offset_positon);
    //                 Draging.end();
    //                 up_slot.selection_click = false;
    //                 selection_click = false;
    //             }
    //             else
    //             {
    //                 Debug.Log("spot is full");
    //                 Text_Output.text = "spot is full";
    //                 Draging.end();
    //             }
    //         }

    //         // first click cell is LEFT
    //         if (right_slot != null && right_slot.selection_click == true)
    //         {
    //             if (wire_fulled == false && node_fulled == false)
    //             {
    //                 Debug.Log("Place wire");
    //                 Text_Output.text = "Place wire";
    //                 offset_positon = new UnityEngine.Vector3(35f,0f, 0f);
    //                 offset_rotate = 0;
    //                 level.place_wire_sound();
    //                 place_wire(right_slot, offset_rotate, offset_positon);
    //                 right_slot.selection_click = false;
    //                 selection_click = true;
    //             }
    //             else if (node_fulled == true && wire_fulled == true)
    //             {
    //                 Debug.Log("Wire spot full");
    //                 Text_Output.text = "Wire spot full";
    //                 Draging.end();
    //             }
                
    //             else if (node_fulled == true && attachednode.id == right_slot.wire_id){
    //                 Debug.Log("Place wire");
    //                 Text_Output.text = "Place wire";
    //                 offset_positon = new UnityEngine.Vector3(35f,0f, 0f);
    //                 offset_rotate = 0;
    //                 level.place_wire_sound();
    //                 place_wire(right_slot, offset_rotate, offset_positon);
    //                 Draging.end();
    //                 right_slot.selection_click = false;
    //                 selection_click = false;
    //             }
    //             else
    //             {
    //                 Debug.Log("spot is full");
    //                 Text_Output.text = "spot is full";
    //                 Draging.end();
    //             }
    //         }

    //         // first click cell is RIGHT
    //         if (left_slot != null && left_slot.selection_click == true)
    //         {
    //             if (wire_fulled == false && node_fulled == false)
    //             {
    //                 Debug.Log("Place wire");
    //                 Text_Output.text = "Place wire";
    //                 offset_positon = new UnityEngine.Vector3(-35f,0f, 0f);
    //                 offset_rotate = 0;
    //                 level.place_wire_sound();
    //                 place_wire(left_slot, offset_rotate, offset_positon);
    //                 left_slot.selection_click = false;
    //                 selection_click = true;
    //             }
    //             else if (node_fulled == true && wire_fulled == true)
    //             {
    //                 Debug.Log("Wire spot full");
    //                 Text_Output.text = "Wire spot full";
    //                 Draging.end();
    //             }
    //             else if (node_fulled == true && attachednode.id == left_slot.wire_id){
    //                 Debug.Log("Place wire");
    //                 Text_Output.text = "Place wire";
    //                 offset_positon = new UnityEngine.Vector3(-35f,0f, 0f);
    //                 offset_rotate = 0;
    //                 level.place_wire_sound();
    //                 place_wire(left_slot, offset_rotate, offset_positon);
    //                 Draging.end();
    //                 left_slot.selection_click = false;
    //                 selection_click = false;
    //             }
    //             else
    //             {
    //                 Debug.Log("spot is full");
    //                 Text_Output.text = "spot is full";
    //                 Draging.end();
    //             }
    //         }
    //     }
    // }
    private void TryPlaceFromSelectedNeighbor()
        {
        // public void OnPointerEnter(PointerEventData eventData)
        // {

            // only do this if we are currently dragging
            if (Draging == null) return;
            if (!Draging.gameObject.activeSelf) return; // begin() sets active true, end() sets false

            // Neighbor second click. Checks if the slot is there and where the first click is!
            if ((up_slot != null && up_slot.selection_click) ||
                (down_slot != null && down_slot.selection_click) ||
                (right_slot != null && right_slot.selection_click) ||
                (left_slot != null && left_slot.selection_click))
            {
                // first click cell is UP (meaning: the selected slot is below me)
                if (down_slot != null && down_slot.selection_click)
                {
                    if (!wire_fulled && !node_fulled)
                    {
                        Debug.Log("Place wire");
                        if (Text_Output != null) Text_Output.text = "Place wire";

                        offset_positon = new Vector3(0f, -37f, 0f);
                        offset_rotate = 90;

                        if (level != null) level.place_wire_sound();
                        place_wire(down_slot, offset_rotate, offset_positon);

                        down_slot.selection_click = false;
                        selection_click = true;
                    }
                    else if (node_fulled && wire_fulled)
                    {
                        Debug.Log("Wire spot full");
                        if (Text_Output != null) Text_Output.text = "Wire spot full";
                        Draging.end();
                    }
                    else if (node_fulled && attachednode != null && attachednode.id == down_slot.wire_id)
                    {
                        Debug.Log("fulled node");
                        if (Text_Output != null) Text_Output.text = "fulled node";

                        offset_positon = new Vector3(0f, -37f, 0f);
                        offset_rotate = 90;

                        if (level != null) level.place_wire_sound();
                        place_wire(down_slot, offset_rotate, offset_positon);

                        Draging.end();
                        down_slot.selection_click = false;
                        selection_click = false;
                    }
                    else
                    {
                        Debug.Log("spot is full");
                        if (Text_Output != null) Text_Output.text = "spot is full";
                        Draging.end();
                    }
                }

                // first click cell is DOWN (selected slot is above me)
                if (up_slot != null && up_slot.selection_click)
                {
                    if (!wire_fulled && !node_fulled)
                    {
                        Debug.Log("Place wire");
                        if (Text_Output != null) Text_Output.text = "Place wire";

                        offset_positon = new Vector3(0f, 37f, 0f);
                        offset_rotate = -90;

                        if (level != null) level.place_wire_sound();
                        place_wire(up_slot, offset_rotate, offset_positon);

                        up_slot.selection_click = false;
                        selection_click = true;
                    }
                    else if (node_fulled && wire_fulled)
                    {
                        Debug.Log("Wire spot full");
                        if (Text_Output != null) Text_Output.text = "Wire spot full";
                        Draging.end();
                    }
                    else if (node_fulled && attachednode != null && attachednode.id == up_slot.wire_id)
                    {
                        Debug.Log("Place wire");
                        if (Text_Output != null) Text_Output.text = "Place wire";

                        offset_positon = new Vector3(0f, 37f, 0f);
                        offset_rotate = -90;

                        if (level != null) level.place_wire_sound();
                        place_wire(up_slot, offset_rotate, offset_positon);

                        Draging.end();
                        up_slot.selection_click = false;
                        selection_click = false;
                    }
                    else
                    {
                        Debug.Log("spot is full");
                        if (Text_Output != null) Text_Output.text = "spot is full";
                        Draging.end();
                    }
                }

                // first click cell is LEFT (selected slot is to my right)
                if (right_slot != null && right_slot.selection_click)
                {
                    if (!wire_fulled && !node_fulled)
                    {
                        Debug.Log("Place wire");
                        if (Text_Output != null) Text_Output.text = "Place wire";

                        offset_positon = new Vector3(35f, 0f, 0f);
                        offset_rotate = 0;

                        if (level != null) level.place_wire_sound();
                        place_wire(right_slot, offset_rotate, offset_positon);

                        right_slot.selection_click = false;
                        selection_click = true;
                    }
                    else if (node_fulled && wire_fulled)
                    {
                        Debug.Log("Wire spot full");
                        if (Text_Output != null) Text_Output.text = "Wire spot full";
                        Draging.end();
                    }
                    else if (node_fulled && attachednode != null && attachednode.id == right_slot.wire_id)
                    {
                        Debug.Log("Place wire");
                        if (Text_Output != null) Text_Output.text = "Place wire";

                        offset_positon = new Vector3(35f, 0f, 0f);
                        offset_rotate = 0;

                        if (level != null) level.place_wire_sound();
                        place_wire(right_slot, offset_rotate, offset_positon);

                        Draging.end();
                        right_slot.selection_click = false;
                        selection_click = false;
                    }
                    else
                    {
                        Debug.Log("spot is full");
                        if (Text_Output != null) Text_Output.text = "spot is full";
                        Draging.end();
                    }
                }

                // first click cell is RIGHT (selected slot is to my left)
                if (left_slot != null && left_slot.selection_click)
                {
                    if (!wire_fulled && !node_fulled)
                    {
                        Debug.Log("Place wire");
                        if (Text_Output != null) Text_Output.text = "Place wire";

                        offset_positon = new Vector3(-35f, 0f, 0f);
                        offset_rotate = 0;

                        if (level != null) level.place_wire_sound();
                        place_wire(left_slot, offset_rotate, offset_positon);

                        left_slot.selection_click = false;
                        selection_click = true;
                    }
                    else if (node_fulled && wire_fulled)
                    {
                        Debug.Log("Wire spot full");
                        if (Text_Output != null) Text_Output.text = "Wire spot full";
                        Draging.end();
                    }
                    else if (node_fulled && attachednode != null && attachednode.id == left_slot.wire_id)
                    {
                        Debug.Log("Place wire");
                        if (Text_Output != null) Text_Output.text = "Place wire";

                        offset_positon = new Vector3(-35f, 0f, 0f);
                        offset_rotate = 0;

                        if (level != null) level.place_wire_sound();
                        place_wire(left_slot, offset_rotate, offset_positon);

                        Draging.end();
                        left_slot.selection_click = false;
                        selection_click = false;
                    }
                    else
                    {
                        Debug.Log("spot is full");
                        if (Text_Output != null) Text_Output.text = "spot is full";
                        Draging.end();
                    }
                }
            }
    
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Only allow deleting if this slot is a node (or has a wire_id)
            string colorId = null;
            if (attachednode != null) colorId = attachednode.id;
            else if (wire_fulled) colorId = wire_id;

            if (!string.IsNullOrEmpty(colorId))
            {
                FindObjectOfType<Level1_Manager>().delete_wire(colorId);
            }
        }   
    }

    private void Start()
    {
        if (attachednode != null)
        {
            node_fulled = true;
        }
    }

    public void DeleteConnectedWires(Slot startSlot, string color)
    {
    if (startSlot == null) return; //thing to delcte

    int steps = 0;
    int maxSteps = 500;

    // stack-based search so it deletes branches too!
    List<Slot> stack = new List<Slot>();
    HashSet<Slot> visited = new HashSet<Slot>();

    stack.Add(startSlot);

        while (stack.Count > 0 && steps < maxSteps)
        {
            steps++;

            Slot current = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);

            if (current == null) continue;
            if (visited.Contains(current)) continue;
            visited.Add(current);
            current.selection_click = false;

            // Only delete slots that are actually part of this color wire
            if (current.wire_fulled == true && current.wire_id == color)
            {
                // delete wire data on this slot
                current.wire_fulled = false;
                current.wire_id = null;


                // Visit neighbors (only follow the same color)
                if (current.down_slot != null && !visited.Contains(current.down_slot) &&
                    current.down_slot.wire_fulled == true && current.down_slot.wire_id == color)
                {
                    stack.Add(current.down_slot);
                }

                if (current.up_slot != null && !visited.Contains(current.up_slot) &&
                    current.up_slot.wire_fulled == true && current.up_slot.wire_id == color)
                {
                    stack.Add(current.up_slot);
                }

                if (current.left_slot != null && !visited.Contains(current.left_slot) &&
                    current.left_slot.wire_fulled == true && current.left_slot.wire_id == color)
                {
                    stack.Add(current.left_slot);
                }

                if (current.right_slot != null && !visited.Contains(current.right_slot) &&
                    current.right_slot.wire_fulled == true && current.right_slot.wire_id == color)
                {
                    stack.Add(current.right_slot);
                }
            }
        }
    }
    // public void DeleteWireVisualsTouchingThisSlot(string colorId)
    // {
    //     if (wireParent == null)
    //     {
    //         Debug.LogWarning("wireParent is null");
    //         return;
    //     }

    //     string prefix = "Wire_" + colorId + "_";
    //     string wire = name; // this slot's GameObject name

    //     for (int i = wireParent.childCount - 1; i >= 0; i--)
    //     {
    //         Transform child = wireParent.GetChild(i);

    //         // Only delete wires of this color
    //         if (!child.name.StartsWith(prefix))
    //             continue;

    //         // Delete if the wire name mentions this slot name
    //         if (child.name.Contains(wire))
    //         {
    //             Destroy(child.gameObject);
    //         }
    //     }
    // }

}

    




