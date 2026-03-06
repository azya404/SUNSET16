using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Drag : MonoBehaviour
{
    private bool dragging = false;
    private Vector3 start_position;
    private Vector3 offset;
    // Start is called before the first frame update

    // Update is called once per frame
    void Update()
    {
        if (dragging)
        {
            transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition) + offset;
        }
    
    }
    private void OnMouseDown()
    {
        start_position = transform.position;
        offset =  transform.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);
        dragging = true;
    }

    private void OnMouseUp()
    {
        dragging = false;
        transform.position = start_position;
    }

    
}
