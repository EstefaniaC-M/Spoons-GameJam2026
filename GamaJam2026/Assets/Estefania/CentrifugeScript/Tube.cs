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

    // Called by Centrifuge_Manager, which raycasts through the minigame camera.
    // (OnMouseDown doesn't work reliably when this is drawn by a stacked overlay camera.)
    public void BeginDrag()
    {
        if (locked) return;

        grabbed = true;
        ShowAsVial();
        offset = transform.position - MouseWorld();
    }

    public void Drag()
    {
        if (locked || !grabbed) return;
        transform.position = MouseWorld() + offset;
    }

    public void EndDrag()
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
        // Use the minigame's own 2D camera, not the 3D room camera.
        Camera cam = (manager != null && manager.minigameCamera != null)
            ? manager.minigameCamera
            : Camera.main;
        Vector3 p = cam.ScreenToWorldPoint(Input.mousePosition);
        p.z = 0f;
        return p;
    }
}