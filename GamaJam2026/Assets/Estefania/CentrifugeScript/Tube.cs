using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Tube : MonoBehaviour
{
    public int weight = 10;
    public Centrifuge_Manager manager;

    [HideInInspector] public Vector3 homePos;
    [HideInInspector] public bool locked = false;

    Vector3 offset;

    void Start()
    {
        if (manager == null)
            manager = FindObjectOfType<Centrifuge_Manager>();
    }

    void OnMouseDown()
    {
        Debug.Log("Clicked " + name);
        if (locked) return;
        offset = transform.position - MouseWorld();
    }

    void OnMouseDrag()
    {
        if (locked) return;
        transform.position = MouseWorld() + offset;
    }

    void OnMouseUp()
    {
        if (locked) return;
        if (manager == null)
        {
            Debug.LogError("Tube " + name + " can't find the Centrifuge_Manager");
            return;
        }
        manager.PlaceTube(this);
    }

    Vector3 MouseWorld()
    {
        Vector3 p = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        p.z = 0f;
        return p;
    }
}