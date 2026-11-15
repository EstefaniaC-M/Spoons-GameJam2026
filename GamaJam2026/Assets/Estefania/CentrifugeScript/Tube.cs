using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Tube : MonoBehaviour
{
    public int weight = 10;
    public Centrifuge_Manager manager;

    public Sprite capSprite;       
    public float capScale = 1f;   
    public Vector2 capOffset;      
    public float capGrabSize = 1f;

    [HideInInspector] public Vector3 homePos;
    [HideInInspector] public bool locked = false;

    Vector3 offset;
    SpriteRenderer sr;
    BoxCollider2D box;
    Sprite vialSprite;
    Vector3 vialScale;
    Vector2 vialBoxSize;
    Vector2 vialBoxOffset;
    bool isCap = false;
    bool grabbed = false;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        box = GetComponent<BoxCollider2D>();
        vialSprite = sr.sprite;
        vialScale = transform.localScale;
        if (box != null)
        {
            vialBoxSize = box.size;
            vialBoxOffset = box.offset;
        }
    }

    void Start()
    {
        if (manager == null)
            manager = FindObjectOfType<Centrifuge_Manager>();
    }

    public void ShowAsCap()
    {
        if (capSprite == null || isCap) return;
        isCap = true;
        sr.sprite = capSprite;
        transform.localScale = vialScale * capScale;
        transform.position += (Vector3)capOffset;
        if (box != null)
        {
            box.offset = Vector2.zero;
            box.size = Vector2.one * (capGrabSize / transform.localScale.x);
        }
    }

    public void ShowAsVial()
    {
        if (!isCap) return;
        isCap = false;
        sr.sprite = vialSprite;
        transform.localScale = vialScale;
        transform.position -= (Vector3)capOffset;
        if (box != null)
        {
            box.size = vialBoxSize;
            box.offset = vialBoxOffset;
        }
    }

    void OnMouseDown()
    {
        if (locked) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        grabbed = true;
        ShowAsVial();
        offset = transform.position - MouseWorld();
    }

    void OnMouseDrag()
    {
        if (locked || !grabbed) return;
        transform.position = MouseWorld() + offset;
    }

    void OnMouseUp()
    {
        if (locked || !grabbed) return;
        grabbed = false;

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