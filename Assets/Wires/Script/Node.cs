using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node : MonoBehaviour
{
    [SerializeField] private string Node_id;
    
    public Slot slot_spot;
    public string id => Node_id;

    // Start is called before the first frame update
}
