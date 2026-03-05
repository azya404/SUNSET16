using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;

public class Level1_Manager : MonoBehaviour
{
    public TMP_Text Text_Output;
    public Lever lever;
    public Slot red_node_1;
    public Slot red_node_2;
    public Slot blue_node_1;
    public Slot blue_node_2;
    public Slot green_node_1;
    public Slot green_node_2;
    public Slot yellow_node_1;
    public Slot yellow_node_2;
    public List<Slot> allSlots = new List<Slot>();
    public Transform wire_spot; // drag your "Wire_spot" GameObject here
    public List<Slot> action_stack;
    public bool do_action;
    [SerializeField] private AudioSource audioSource_success;
    [SerializeField] private AudioClip clip_success;
    [SerializeField] private AudioSource audioSource_fail;
    [SerializeField] private AudioClip clip_fail;

    [SerializeField] private AudioSource audio_place_wire;
    [SerializeField] private AudioClip clip_place_wire;

    [SerializeField] private AudioSource audio_delete_wire;
    [SerializeField] private AudioClip clip_delete_wire;

    public bool red_done = false;
    public bool yellow_done = false;
    public bool green_done = false;
    public bool blue_done = false;
    public bool leverBusy = false;
    
    private IEnumerator PlayAndWait(AudioClip clip, AudioSource audioSource)
    {
        if (audioSource == null || clip == null) yield break;

        audioSource.PlayOneShot(clip);

        // wait until it's done
        yield return new WaitForSeconds(clip.length);

        // code here runs AFTER sound finishes
        Debug.Log("Sound finished!");
    }

    public void lever_click()
{
    StartCoroutine(lever_click_routine());
}

    public void place_wire_sound(){
            audio_place_wire.PlayOneShot(clip_place_wire);
        }


private IEnumerator lever_click_routine()
{
    if (leverBusy) yield break;
    leverBusy = true;

    Debug.Log("Lever click");
    if (Text_Output != null) Text_Output.text = "Lever click";

    red_done = check_connected(red_node_1, "Red", red_node_2);
    yellow_done = check_connected(yellow_node_1, "Yellow", yellow_node_2);
    blue_done = check_connected(blue_node_1, "Blue", blue_node_2);
    green_done = check_connected(green_node_1, "Green", green_node_2);

    if (red_done && yellow_done && green_done && blue_done)
    {
        if (Text_Output != null) Text_Output.text = "Done!";
        yield return StartCoroutine(PlayAndWait(clip_success, audioSource_success));
    }
    else
    {
        if (Text_Output != null) Text_Output.text = "Not done";
        yield return StartCoroutine(PlayAndWait(clip_fail, audioSource_fail));
    }

    leverBusy = false;
    lever.pull_back();
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
}

//     public bool check_connected(Slot current_slot, string color, Slot end)
// {
//     Debug.Log("Check step");

//     Slot previous = null;
//     int steps = 0;
//     int maxSteps = 500; // safety so you never infinite loop

//     while (current_slot != null && steps < maxSteps)
//     {
//         if (current_slot == end)
//             return true;

//         steps++;

//         Slot next = null;

//         // Try each direction, but don't go back to the previous slot
//         if (current_slot.down_slot != null &&
//             current_slot.down_slot != previous &&
//             current_slot.down_slot.wire_id == color)
//         {
//             next = current_slot.down_slot;
//         }
//         else if (current_slot.up_slot != null &&
//                  current_slot.up_slot != previous &&
//                  current_slot.up_slot.wire_id == color)
//         {
//             next = current_slot.up_slot;
//         }
//         else if (current_slot.left_slot != null &&
//                  current_slot.left_slot != previous &&
//                  current_slot.left_slot.wire_id == color)
//         {
//             next = current_slot.left_slot;
//         }
//         else if (current_slot.right_slot != null &&
//                  current_slot.right_slot != previous &&
//                  current_slot.right_slot.wire_id == color)
//         {
//             next = current_slot.right_slot;
//         }
//         else
//         {
//             Debug.Log("No wires found from " + current_slot.name);
//             return false;
//         }

//         previous = current_slot;
//         current_slot = next;
//     }

//     Debug.Log("Stopped: too many steps or current_slot became null");
//     Text_Output.text = "Stopped: too many steps or current_slot became null";
//     return false;

//     }
    
public bool check_connected(Slot current_slot, string color, Slot end)
{
    Debug.Log("Check step");

    if (current_slot == null || end == null) return false;

    // If the endpoints aren't even filled with this color wire, it's not connected.
    if (!current_slot.wire_fulled || current_slot.wire_id != color) return false;
    if (!end.wire_fulled || end.wire_id != color) return false;

    Slot previous = null;
    int steps = 0;
    int maxSteps = 500;

    while (current_slot != null && steps < maxSteps)
    {
        if (current_slot == end)
            return true;

        steps++;

        Slot next = null;

        // Down
        if (current_slot.down_slot != null &&
            current_slot.down_slot != previous &&
            current_slot.down_slot.wire_fulled &&
            current_slot.down_slot.wire_id == color)
        {
            next = current_slot.down_slot;
        }
        // Up
        else if (current_slot.up_slot != null &&
                 current_slot.up_slot != previous &&
                 current_slot.up_slot.wire_fulled &&
                 current_slot.up_slot.wire_id == color)
        {
            next = current_slot.up_slot;
        }
        // Left
        else if (current_slot.left_slot != null &&
                 current_slot.left_slot != previous &&
                 current_slot.left_slot.wire_fulled &&
                 current_slot.left_slot.wire_id == color)
        {
            next = current_slot.left_slot;
        }
        // Right
        else if (current_slot.right_slot != null &&
                 current_slot.right_slot != previous &&
                 current_slot.right_slot.wire_fulled &&
                 current_slot.right_slot.wire_id == color)
        {
            next = current_slot.right_slot;
        }
        else
        {
            Debug.Log("No filled wire found from " + current_slot.name + " for color " + color);
            return false;
        }

        previous = current_slot;
        current_slot = next;
    }

    Debug.Log("Stopped: too many steps or current_slot became null");
    if (Text_Output != null)
        Text_Output.text = "Stopped: too many steps or current_slot became null";

    return false;
}
}
