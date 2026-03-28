using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;
using SUNSET16.Interaction;
using System.Linq;
using Unity.VisualScripting;

public class Level_Manager : MonoBehaviour
{
    public TMP_Text Text_Output;
    public Lever lever;
    public Slot red_node_1;
    public Slot red_node_2;
    public Slot blue_node_1;
    public Slot blue_node_2;
    public Slot purple_node_1;
    public Slot purple_node_2;
    public Slot yellow_node_1;
    public Slot yellow_node_2;
    public Slot white_node_1;
    public Slot white_node_2;
    public Slot black_node_1;
    public Slot black_node_2;
    public List<Slot> allSlots = new List<Slot>();
    public List<Slot> action_stack = new List<Slot>();
    //public Stack<GameObject> action_wire_stack = new Stack<GameObject>();
    public GameObject bend_wire_prefab;
    //public Stack<GameObject> bend_wire_stack = new Stack<GameObject>();
    //[System.NonSerialized] public List<Slot> action_stack = new List<Slot>();
    [System.NonSerialized] public Stack<GameObject> action_wire_stack = new Stack<GameObject>();
    [System.NonSerialized] public Stack<GameObject> bend_wire_stack = new Stack<GameObject>();
    private List<Slot> red_slots = null;
    private List<Slot> yellow_slots = null;
    private List<Slot> purple_slots = null;
    private List<Slot> blue_slots = null;
    private List<Slot> white_slots = null;
    private List<Slot> black_slots = null;

    public Transform wire_spot; // drag your "Wire_spot" GameObject here
    public bool do_action;
    [SerializeField] private AudioClip clip_place_wire;
    [SerializeField] private AudioSource audio_place_wire;
    [SerializeField] private AudioSource audio_delete_wire;
    [SerializeField] private AudioClip clip_delete_wire;
    
    public bool red_done = false;
    public bool yellow_done = false;
    public bool blue_done = false;
    public bool purple_done = false;
    public bool white_done = false;
    public bool black_done = false;
    public bool leverBusy = false;
    public bool red_present = false;
    public bool yellow_present = false;
    public bool purple_present = false;
    public bool white_present = false;
    public bool black_present = false;
    public bool blue_present = false;

    [Header("Task Integration")]
    [Tooltip("Drag the Task1Object or Task2Object here — its TaskInteraction closes the overlay on puzzle success.")]
    [SerializeField] public TaskInteraction taskInteraction;
    [SerializeField] public PuzzleInteraction puzzleInteraction;
    private void Start()
    {
        if (red_node_1 != null && red_node_2 != null)
        {
            red_present = true;
            red_node_1.node_id = "Red";
            red_node_1.node_fulled = true;
            red_node_1.update_slot_visual();

            red_node_2.node_id = "Red";
            red_node_2.node_fulled = true;
            red_node_2.update_slot_visual();
        }

        if (yellow_node_1 != null && yellow_node_2 != null)
        {
            yellow_present = true;
            yellow_node_1.node_id = "Yellow";
            yellow_node_1.node_fulled = true;
            yellow_node_1.update_slot_visual();

            yellow_node_2.node_id = "Yellow";
            yellow_node_2.node_fulled = true;
            yellow_node_2.update_slot_visual();
        }

        if (purple_node_1 != null && purple_node_2 != null)
        {
            purple_present = true;
            purple_node_1.node_id = "Purple";
            purple_node_1.node_fulled = true;
            purple_node_1.update_slot_visual();

            purple_node_2.node_id = "Purple";
            purple_node_2.node_fulled = true;
            purple_node_2.update_slot_visual();
        }

        if (blue_node_1 != null && blue_node_2 != null)
        {
            blue_present = true;
            blue_node_1.node_id = "Blue";
            blue_node_1.node_fulled = true;
            blue_node_1.update_slot_visual();

            blue_node_2.node_id = "Blue";
            blue_node_2.node_fulled = true;
            blue_node_2.update_slot_visual();
        }

        if (white_node_1 != null && white_node_2 != null)
        {
            white_present = true;
            white_node_1.node_id = "White";
            white_node_1.node_fulled = true;
            white_node_1.update_slot_visual();

            white_node_2.node_id = "White";
            white_node_2.node_fulled = true;
            white_node_2.update_slot_visual();
        }

        if (black_node_1 != null && black_node_2 != null)
        {
            black_present = true;
            black_node_1.node_id = "Black";
            black_node_1.node_fulled = true;
            black_node_1.update_slot_visual();

            black_node_2.node_id = "Black";
            black_node_2.node_fulled = true;
            black_node_2.update_slot_visual();
        }
    }

        public void lever_click()
    {
        StartCoroutine(lever_click_routine());
    }

        public void place_wire_sound(){
                audio_place_wire.PlayOneShot(clip_place_wire);
            }

        public void connent_wrong_animation()
    {
        lever.Lever_Wrong_Connent();
    }
        public void connent_right_animation()
    {
        lever.Lever_Right_Connect();
    }
        public void connent_node_animation()
    {
        lever.Lever_Node_Connect();
    }

    private IEnumerator lever_click_routine()
    {
        if (leverBusy) yield break;
        leverBusy = true;

        Debug.Log("Lever click");
        if (Text_Output != null) Text_Output.text = "Lever click";

        red_done = !red_present || check_done(red_node_1, red_node_2, red_slots);
        yellow_done = !yellow_present || check_done(yellow_node_1, yellow_node_2, yellow_slots);
        blue_done = !blue_present || check_done(blue_node_1, blue_node_2, blue_slots);
        purple_done = !purple_present || check_done(purple_node_1, purple_node_2, purple_slots);
        white_done = !white_present || check_done(white_node_1, white_node_2, white_slots);
        black_done = !black_present || check_done(black_node_1, black_node_2, black_slots);

        if (red_done && yellow_done && purple_done && blue_done && white_done && black_done)
        {
            
            if (Text_Output != null) Text_Output.text = "Done!";
            lever.lever_Right();
            Debug.Log("This code ran");

            //if (taskInteraction != null) taskInteraction.CloseOverlay(); Moved to Lever!
        }
        else
        {
            if (Text_Output != null) Text_Output.text = "Not done";
            Debug.Log("This code ran for when it faled");
            lever.lever_Worng();
            red_done = false;
            yellow_done = false;
            blue_done = false;
            purple_done = false;
            white_done = false;
            black_done = false;
        }
    }

    public void delete_wire(string colorId)
    {
        if (string.IsNullOrEmpty(colorId)) return;

        // 1) Clear SLOT DATA for that color
        audio_delete_wire.PlayOneShot(clip_delete_wire);
        for (int i = 0; i < allSlots.Count; i++)
        {
            Slot s = allSlots[i];
            if (s == null) continue;

            if (s.wire_fulled && s.wire_id == colorId)
            {
                s.wire_fulled = false;
                s.wire_id = null;
                s.has_bend = false;
            }

            // optional: clear selection highlight if you use it
            s.selection_click = false;
        }

        // 2) Delete VISUAL CLONES under Wire_spot
        if (wire_spot == null)
        {
            Debug.LogWarning("delete_wire: wire_spot not assigned on Level1_Manager");
            return;
        }

        string prefix = colorId;

        for (int i = wire_spot.childCount - 1; i >= 0; i--)
        {
            Transform child = wire_spot.GetChild(i);
            if (child == null) continue;

            if (child.name.StartsWith(prefix))
            {
                Destroy(child.gameObject);
            }
        }

        Debug.Log("Deleted all wires of color: " + colorId);
        if (Text_Output != null) Text_Output.text = "Deleted all " + colorId + " wires";

        if (colorId == "Red" && red_slots != null) red_slots.Clear();
        else if (colorId == "Blue" && blue_slots != null) blue_slots.Clear(); //check if null
        else if (colorId == "Purple" && purple_slots != null) purple_slots.Clear();
        else if (colorId == "Yellow" && yellow_slots != null) yellow_slots.Clear();
        else if (colorId == "White" && white_slots != null) white_slots.Clear();
        else if (colorId == "Black" && black_slots != null) black_slots.Clear();
    }
    public bool check_connected(Slot target1_slot, string colorId, Slot target2_slot)
        {

            if (action_stack.Contains(target1_slot) == true && action_stack.Contains(target2_slot) == true)
            {
                Debug.Log("Done wire");
                if (colorId == "Red") {
                    red_slots = new List<Slot>(action_stack);
                    // Debug.Log("red list made");
                    // for (int i = 0; i < red_slots.Count; i++)
                    // {
                    //     Debug.Log(red_slots[i]);
                    // }
                    }
                else if (colorId == "Blue") blue_slots = new List<Slot>(action_stack);
                else if (colorId == "Purple") purple_slots = new List<Slot>(action_stack);
                else if (colorId == "Yellow") yellow_slots = new List<Slot>(action_stack);
                else if (colorId == "White") white_slots = new List<Slot>(action_stack);
                else if (colorId == "Black") black_slots = new List<Slot>(action_stack);

                action_stack.Clear();
                action_wire_stack.Clear();
                return true;
            }
            else
            {
                Debug.Log("NOT Done wire");
                action_stack.Clear();
                action_wire_stack.Clear();
                bend_wire_stack.Clear();
                delete_wire(colorId);
                return false;
            }

        }
    public bool check_done(Slot target1_slot, Slot target2_slot, List<Slot> line)
    {
        if (line == null) return false; //not there
        if (line.Contains(target1_slot) && line.Contains(target2_slot)) return true; //Done
        Debug.Log( "This wire is not done "+ target1_slot.wire_id);
        return false; //Not done
    }
    public Slot peek() //looks at NOT the top but sec from top
    {
        int i = action_stack.Count - 2;
        Slot item = null;

        if (i > 0) {
            item = action_stack[i];
        }
        return item;
    }

    public Slot action_stack_remove_last()
    {
        int i = action_stack.Count - 1;
        Slot item = action_stack[i];
        action_stack.Remove(item);
        back_wire();

        return item;
    }

    public void back_wire()
    {
        if (action_wire_stack.Count > 0)
        {
            GameObject wire = action_wire_stack.Pop();
            if (wire != null)
            {
                Destroy(wire);
            }
        }
    }

    // public void back_wire_bend()
    // {
    //     if (bend_wire_stack.Peek() != null)
    //     {
    //     GameObject bend_wire = bend_wire_stack.Pop();
    //     Destroy(bend_wire);
    //     }
    // }
    public void back_wire_bend()
{
    if (bend_wire_stack.Count > 0)
    {
        GameObject bend_wire = bend_wire_stack.Pop();
        if (bend_wire != null)
        {
            Destroy(bend_wire);
        }
    }
}

private Vector2Int get_step_direction(Slot from, Slot to)
{
    RectTransform from_rect = from.transform as RectTransform;
    RectTransform to_rect = to.transform as RectTransform;

    Vector2 from_position;
    Vector2 to_position;

    if (from_rect != null && to_rect != null)
    {
        from_position = from_rect.anchoredPosition;
        to_position = to_rect.anchoredPosition;
    }
    else
    {
        from_position = from.transform.localPosition;
        to_position = to.transform.localPosition;
    }

    Vector2 difference = to_position - from_position;

    if (Mathf.Abs(difference.x) > Mathf.Abs(difference.y))
    {
        if (difference.x > 0f)
        {
            return new Vector2Int(1, 0);
        }
        else
        {
            return new Vector2Int(-1, 0);
        }
    }
    else
    {
        if (difference.y > 0f)
        {
            return new Vector2Int(0, 1);
        }
        else
        {
            return new Vector2Int(0, -1);
        }
    }
}
public void is_wire_bent()
{
    if (action_stack.Count < 3)
    {
        return;
    }

    Slot last = action_stack[action_stack.Count - 1];
    Slot mid = action_stack[action_stack.Count - 2];
    Slot first = action_stack[action_stack.Count - 3];

    if (mid.has_bend)
    {
        return;
    }

    Vector2Int first_to_mid = get_step_direction(first, mid);
    Vector2Int mid_to_last = get_step_direction(mid, last);

    // straight line
    if (first_to_mid == mid_to_last)
    {
        return;
    }

    Quaternion spawnRot;
    float offset_x = 0f;
    float offset_y = 0f;

    // up then left
    if (first_to_mid == new Vector2Int(0, 1) && mid_to_last == new Vector2Int(-1, 0))
    {
        spawnRot = Quaternion.Euler(0f, 0f, 180f);
        offset_x = -6.91f;
        offset_y = -7f;
    }
    // left then up
    else if (first_to_mid == new Vector2Int(-1, 0) && mid_to_last == new Vector2Int(0, 1))
    {
        spawnRot = Quaternion.Euler(0f, 0f, 0f);
        offset_x = 6.91f;
        offset_y = 7f;
    }
    // up then right
    else if (first_to_mid == new Vector2Int(0, 1) && mid_to_last == new Vector2Int(1, 0))
    {
        spawnRot = Quaternion.Euler(0f, 0f, 270f);
        offset_x = 6.91f;
        offset_y = -7f;
    }
    // right then up
    else if (first_to_mid == new Vector2Int(1, 0) && mid_to_last == new Vector2Int(0, 1))
    {
        spawnRot = Quaternion.Euler(0f, 0f, 90f);
        offset_x = -6.91f;
        offset_y = 7f;
    }
    // down then left
    else if (first_to_mid == new Vector2Int(0, -1) && mid_to_last == new Vector2Int(-1, 0))
    {
        spawnRot = Quaternion.Euler(0f, 0f, 90f);
        offset_x = -6.91f;
        offset_y = 7f;
    }
    // left then down
    else if (first_to_mid == new Vector2Int(-1, 0) && mid_to_last == new Vector2Int(0, -1))
    {
        spawnRot = Quaternion.Euler(0f, 0f, 270f);
        offset_x = 6.91f;
        offset_y = -7f;
    }
    // down then right
    else if (first_to_mid == new Vector2Int(0, -1) && mid_to_last == new Vector2Int(1, 0))
    {
        spawnRot = Quaternion.Euler(0f, 0f, 0f);
        offset_x = 6.91f;
        offset_y = 7f;
    }
    // right then down
    else if (first_to_mid == new Vector2Int(1, 0) && mid_to_last == new Vector2Int(0, -1))
    {
        spawnRot = Quaternion.Euler(0f, 0f, 180f);
        offset_x = -6.91f;
        offset_y = -7f;
    }
    else
    {
        return;
    }

    if (bend_wire_prefab == null || mid.wire_spot == null)
    {
        return;
    }

    place_wire_sound();

    Vector3 midLocalPosition = mid.wire_spot.InverseTransformPoint(mid.transform.position);
    Vector3 localOffset = new Vector3(offset_x, offset_y, 0f);

    GameObject clone = Instantiate(bend_wire_prefab, mid.wire_spot);
    clone.name = mid.wire_id;

    RectTransform cloneRect = clone.GetComponent<RectTransform>();

    if (cloneRect != null)
    {
        cloneRect.localScale = Vector3.one;
        cloneRect.localRotation = spawnRot;
        cloneRect.localPosition = midLocalPosition + localOffset;
    }
    else
    {
        clone.transform.localScale = Vector3.one;
        clone.transform.localRotation = spawnRot;
        clone.transform.localPosition = midLocalPosition + localOffset;
    }

    bend_wire_stack.Push(clone);
    mid.has_bend = true;

    Bend_Wire_Manager script = clone.GetComponent<Bend_Wire_Manager>();
    if (script != null)
    {
        script.slot = mid;
        script.ColorWire(mid);
    }
}
// public void is_wire_bent()
// {
//     if (action_stack.Count < 3)
//     {
//         return;
//     }

//     Debug.Log("Is_wire_bent ran");

//     Slot last = action_stack[action_stack.Count - 1];
//     Slot mid = action_stack[action_stack.Count - 2];
//     Slot first = action_stack[action_stack.Count - 3];

//     float first_to_mid_x = mid.transform.position.x - first.transform.position.x;
//     float first_to_mid_y = mid.transform.position.y - first.transform.position.y;
//     Debug.Log(first_to_mid_x);
//     Debug.Log(first_to_mid_y);

//     float mid_to_last_x = last.transform.position.x - mid.transform.position.x;
//     float mid_to_last_y = last.transform.position.y - mid.transform.position.y;
//     Debug.Log(mid_to_last_x);
//     Debug.Log(mid_to_last_y);

//     Quaternion spawnRot;
//     float offset_x = 0f;
//     float offset_y = 0f;

//     // straight up/down
//     if (first_to_mid_x == 0f && mid_to_last_x == 0f)
//     {
//         return;
//     }
//     // straight left/right
//     if (first_to_mid_y == 0f && mid_to_last_y == 0f)
//     {
//         return;
//     }
//     // up then left
//     if (first_to_mid_y > 0 && mid_to_last_x < 0)
//     {
//         spawnRot = Quaternion.Euler(0f, 0f, 180f);
//         offset_x = -6.91f;
//         offset_y = -7f;
//     }
//     // left then up
//     else if (first_to_mid_x < 0 && mid_to_last_y > 0)
//     {
//         spawnRot = Quaternion.Euler(0f, 0f, 0f);
//         offset_x = 6.91f;
//         offset_y = 7f;
//     }
//     // up then right
//     else if (first_to_mid_y > 0 && mid_to_last_x > 0)
//     {
//         spawnRot = Quaternion.Euler(0f, 0f, 270f);
//         offset_x = 6.91f;
//         offset_y = -7f;
//     }
//     // right then up
//     else if (first_to_mid_x > 0 && mid_to_last_y > 0)
//     {
//         spawnRot = Quaternion.Euler(0f, 0f, 90f);
//         offset_x = -6.91f;
//         offset_y = 7f;
//     }
//     // down then left
//     else if (first_to_mid_y < 0 && mid_to_last_x < 0)
//     {
//         spawnRot = Quaternion.Euler(0f, 0f, 90f);
//         offset_x = -6.91f;
//         offset_y = 7f;
//     }
//     // left then down
//     else if (first_to_mid_x < 0 && mid_to_last_y < 0)
//     {
//         spawnRot = Quaternion.Euler(0f, 0f, 270f);
//         offset_x = 6.91f;
//         offset_y = -7f;
//     }
//     // down then right
//     else if (first_to_mid_y < 0 && mid_to_last_x > 0)
//     {
//         spawnRot = Quaternion.Euler(0f, 0f, 0f);
//         offset_x = 6.91f;
//         offset_y = 7f;
//     }
//     // right then down
//     else if (first_to_mid_x > 0 && mid_to_last_y < 0)
//     {
//         spawnRot = Quaternion.Euler(0f, 0f, 180f);
//         offset_x = -6.91f;
//         offset_y = -7f;
//     }
//     else
//     {
//         return;
//     }

//     if (bend_wire_prefab == null || mid.wire_spot == null)
//     {
//         return;
//     }

//     // Convert the mid node's world position into wire_spot local space
//     place_wire_sound();
//     Vector3 midLocalPosition = mid.wire_spot.InverseTransformPoint(mid.transform.position);
//     Vector3 localOffset = new Vector3(offset_x, offset_y, 0f);

//     GameObject clone = Instantiate(bend_wire_prefab, mid.wire_spot);
//     clone.name = mid.wire_id;

//     RectTransform cloneRect = clone.GetComponent<RectTransform>();

//     if (cloneRect != null)
//     {
//         cloneRect.localScale = Vector3.one;
//         cloneRect.localRotation = spawnRot;
//         cloneRect.localPosition = midLocalPosition + localOffset;
//     }
//     else
//     {
//         clone.transform.localScale = Vector3.one;
//         clone.transform.localRotation = spawnRot;
//         clone.transform.localPosition = midLocalPosition + localOffset;
//     }

//     bend_wire_stack.Push(clone);
//     mid.has_bend = true;

//     Bend_Wire_Manager script = clone.GetComponent<Bend_Wire_Manager>();
//     if (script != null)
//     {
//         script.slot = mid;
//         script.ColorWire(mid);
//     }

//     // If action_wire_stack is a List<GameObject>, use Add instead of Append
//     // action_wire_stack.Add(clone);
// }

// public void is_wire_bent()
// {
//     if (action_stack.Count < 3)
//     {
//         return;
//     }

//     Debug.Log("Is_wire_bent ran");
//     Slot last = action_stack[action_stack.Count - 1];
//     Slot mid = action_stack[action_stack.Count - 2];
//     Slot first = action_stack[action_stack.Count - 3];

//     float first_to_mid_x = mid.transform.position.x - first.transform.position.x;
//     float first_to_mid_y = mid.transform.position.y - first.transform.position.y;

//     float mid_to_last_x = last.transform.position.x - mid.transform.position.x;
//     float mid_to_last_y = last.transform.position.y - mid.transform.position.y;

//     Quaternion spawnRot;
//     float offset_x = 0f;
//     float offset_y = 0f;

//     // up then left
//     if (first_to_mid_y > 0 && mid_to_last_x < 0)
//     {
//         spawnRot = Quaternion.Euler(0f, 0f, 180f);
//         offset_x = -6.91f;
//         offset_y = -7f;
//     }
//     // left then up
//     else if (first_to_mid_x < 0 && mid_to_last_y > 0)
//     {
//         spawnRot = Quaternion.Euler(0f, 0f, 0f);
//         offset_x = 6.91f;
//         offset_y = 7f;
//     }
//     // up then right 
//     else if (first_to_mid_y > 0 && mid_to_last_x > 0)
//     {
//         spawnRot = Quaternion.Euler(0f, 0f, 270f);
//         offset_x = 6.91f;
//         offset_y = -7f;
//     }
//     // right then up 
//     else if (first_to_mid_x > 0 && mid_to_last_y > 0)
//     {
//         spawnRot = Quaternion.Euler(0f, 0f, 90f);
//         offset_x = -6.91f;
//         offset_y = 7f;
//     }
//     // down then left
//     else if (first_to_mid_y < 0 && mid_to_last_x < 0)
//     {
//         spawnRot = Quaternion.Euler(0f, 0f, 90f);
//         offset_x = -6.91f;
//         offset_y = 7f;
//     }
//     // left then down d
//     else if (first_to_mid_x < 0 && mid_to_last_y < 0)
//     {
//         spawnRot = Quaternion.Euler(0f, 0f, 270f);
//         offset_x = 6.91f;
//         offset_y = -7f;
//     }
//     // down then right d
//     else if (first_to_mid_y < 0 && mid_to_last_x > 0)
//     {
//         spawnRot = Quaternion.Euler(0f, 0f, 0f);
//         offset_x = 6.91f;
//         offset_y = 7f;
//     }
//     // right then down d
//     else if (first_to_mid_x > 0 && mid_to_last_y < 0)
//     {
//         spawnRot = Quaternion.Euler(0f, 0f, 180f);
//         offset_x = -6.91f;
//         offset_y = -7f;
//     }
//     else
//     {
//         return;
//     }

//     Vector3 spawnPos = mid.transform.position + new Vector3(offset_x, offset_y, 0f);
//     GameObject clone = Instantiate(bend_wire_prefab, spawnPos, spawnRot, mid.wire_spot);

//     bend_wire_stack.Push(clone);
//     clone.name = mid.wire_id;
//     mid.has_bend = true;
//     action_wire_stack.Append(clone);

//     Bend_Wire_Manager script = clone.GetComponent<Bend_Wire_Manager>();
//     if (script != null)
//     {
//         script.slot = mid;
//         script.ColorWire(mid);
//     }
// }



}
