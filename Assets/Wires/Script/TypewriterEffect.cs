// using System.Collections;
// using TMPro;
// using UnityEngine;

// public class Terminal_Message : MonoBehaviour
// {
//     public TMP_Text text_output;
//     public AudioSource audio_source;
//     public AudioClip send_message_sound;

//     [TextArea(10, 30)]
//     public string long_message =
// @"[AEGIS-077 Instructional Slab]

// Technician,

// Complete the circuit by connecting all wires node-to-node based on colour.

// Wires can be routed in all four cardinal directions, but wires of different colours cannot cross paths. At the DOLOS Corporation, disordered wiring is a cause for concern.

// In the event you select the incorrect wire, letting go of the aforementioned wire resets it.

// In the event that you find an unoptimal path and thus need to get rid of a completed wire, right-clicking the wire to resets it.

// Signed,
// DOLOS-XIII Management System
// On behalf of the DOLOS Corporation, a subsidiary of Erebus Holdings
// “Safeguarding Tomorrow’s Future”";

//     public float flash_time = 3f;
//     public float flash_speed = 0.2f;
//     public float type_speed = 0.01f;

//     private void Start()
//     {
//         StartCoroutine(flash_then_type());
//     }

//     private IEnumerator flash_then_type()
//     {
//         float timer = 0f;
//         bool show_colon = true;

//         while (timer < flash_time)
//         {
//             if (show_colon == true)
//             {
//                 text_output.text = "//:";
//             }
//             else
//             {
//                 text_output.text = "//";
//             }

//             show_colon = !show_colon;

//             yield return new WaitForSeconds(flash_speed);
//             timer = timer + flash_speed;
//         }

//         text_output.text = "//:";

//         if (audio_source != null && send_message_sound != null)
//         {
//             audio_source.PlayOneShot(send_message_sound);
//         }

//         yield return new WaitForSeconds(0.05f);

//         text_output.text = "";

//         int index = 0;

//         while (index < long_message.Length)
//         {
//             text_output.text = text_output.text + long_message[index];
//             index = index + 1;
//             yield return new WaitForSeconds(type_speed);
//         }
//     }
// }

// using System.Collections;
// using TMPro;
// using UnityEngine;

// public class Terminal_Message : MonoBehaviour
// {
//     public TMP_Text text_output;
//     public AudioSource audio_source;
//     public AudioClip send_message_sound;

//     [TextArea(10, 30)]
//     public string long_message =
// @"[AEGIS-077 Instructional Slab]

// Welcome Technician,

// Complete the circuit by connecting all wires node-to-node based on colour.
// Wires can be placed in all 4 cardinal directions
// If you select the incorrect wire, let go to reset it.
// To delete undesired wires, right-click.

// Different coloured wires CANNOT cross paths. 
// At the DOLOS Corporation, disordered wiring is a cause for concern.

// Signed,
// DOLOS-XIII Management System";

//     public float flash_time = 3f;
//     public float flash_speed = 0.2f;
//     public float type_speed = 0.01f;

//     private void Start()
//     {
//         StartCoroutine(flash_then_type());
//     }

//     private IEnumerator flash_then_type()
//     {
//         float timer = 0f;
//         bool show_colon = true;

//         // Flash silently
//         while (timer < flash_time)
//         {
//             if (show_colon == true)
//             {
//                 text_output.text = "//:";
//             }
//             else
//             {
//                 text_output.text = "//";
//             }

//             show_colon = !show_colon;

//             yield return new WaitForSeconds(flash_speed);
//             timer = timer + flash_speed;
//         }

//         text_output.text = "//:";
//         yield return new WaitForSeconds(0.05f);

//         text_output.text = "";

//         // Play sound only when typing starts
//         if (audio_source != null && send_message_sound != null)
//         {
//             audio_source.PlayOneShot(send_message_sound);
//         }

//         int index = 0;

//         while (index < long_message.Length)
//         {
//             text_output.text = text_output.text + long_message[index];
//             index = index + 1;
//             yield return new WaitForSeconds(type_speed);
//         }
//     }
// }

using System.Collections;
using TMPro;
using UnityEngine;

public class Terminal_Message : MonoBehaviour
{
    public TMP_Text text_output;
    public AudioSource audio_source;
    public AudioClip send_message_sound;

    [TextArea(10, 30)]
    public string long_message =
@"[AEGIS-077 Instructional Slab]

Welcome Technician,

Complete the circuit by connecting all wires node-to-node based on colour.
Wires can be placed in all 4 cardinal directions
If you select the incorrect wire, let go to reset it.
To delete undesired wires, right-click.

Different coloured wires CANNOT cross paths. 
At the DOLOS Corporation, disordered wiring is a cause for concern.

Signed,
DOLOS-XIII Management System";

    public float flash_time = 3f;
    public float flash_speed = 0.2f;
    public float type_speed = 0.01f;

    private void Start()
    {
        StartCoroutine(flash_then_type());
    }

    private IEnumerator flash_then_type()
    {
        float timer = 0f;
        bool show_colon = true;

        // Flash silently
        while (timer < flash_time)
        {
            if (show_colon == true)
            {
                text_output.text = "//:";
            }
            else
            {
                text_output.text = "//";
            }

            show_colon = !show_colon;

            yield return new WaitForSeconds(flash_speed);
            timer = timer + flash_speed;
        }

        text_output.text = "//:";
        yield return new WaitForSeconds(0.05f);

        text_output.text = "";

        // Start sound when typing starts
        if (audio_source != null && send_message_sound != null)
        {
            audio_source.clip = send_message_sound;
            audio_source.loop = true;
            audio_source.Play();
        }

        int index = 0;

        while (index < long_message.Length)
        {
            text_output.text = text_output.text + long_message[index];
            index = index + 1;
            yield return new WaitForSeconds(type_speed);
        }

        // Stop sound when typing is done
        if (audio_source != null)
        {
            audio_source.Stop();
            audio_source.loop = false;
            audio_source.clip = null;
        }
    }
}