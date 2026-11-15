using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum ChemicalType { Red, Blue, Yellow, Purple, Green }

[System.Serializable]
public struct ChemicalInfo
{
    public ChemicalType type;
    public Color color;

    public ChemicalInfo(ChemicalType type, Color color)
    {
        this.type = type;
        this.color = color;
    }

    public static ChemicalInfo Get(ChemicalType type)
    {
        switch (type)
        {
            case ChemicalType.Red: return new ChemicalInfo(type, Color.red);
            case ChemicalType.Blue: return new ChemicalInfo(type, Color.blue);
            case ChemicalType.Yellow: return new ChemicalInfo(type, Color.yellow);
            case ChemicalType.Purple: return new ChemicalInfo(type, new Color(0.6f, 0.2f, 0.8f));
            default: return new ChemicalInfo(type, Color.green);
        }
    }
}

public class Tube : MonoBehaviour
{
    public ChemicalType chemical;
    public int weight = 10;                      

    [HideInInspector] public bool isResultTube;
    [HideInInspector] public bool locked;
    [HideInInspector] public Vector3 homePos;     
    [HideInInspector] public Centrifuge_Manager manager;

    private SpriteRenderer sr;
    private bool dragging;
    private Vector3 offset;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void Setup(Vector3 slotPosition, bool isResult)
    {
        homePos = slotPosition;
        transform.position = slotPosition;
        isResultTube = isResult;
        locked = isResult;

        if (isResult) sr.color = Color.white;                 
        else sr.color = ChemicalInfo.Get(chemical).color;     
    }

    public void ShowColor(Color c)
    {
        sr.color = c;
    }

    public void ShowFilled()
    {
        sr.color = ChemicalInfo.Get(chemical).color;
    }

    void OnMouseDown()
    {
        if (locked) return;
        dragging = true;
        offset = transform.position - MouseWorld();
    }

    void OnMouseDrag()
    {
        if (!dragging) return;
        transform.position = MouseWorld() + offset;
    }

    void OnMouseUp()
    {
        if (!dragging) return;
        dragging = false;

        foreach (Collider2D hit in Physics2D.OverlapPointAll(MouseWorld()))
        {
            Tube other = hit.GetComponent<Tube>();
            if (other != null && other.isResultTube)
            {
                Mixer.Instance.Pour(this, other);
                break;
            }
        }

        if (manager != null)
            manager.PlaceTube(this);
        else
            transform.position = homePos;
    }

    Vector3 MouseWorld()
    {
        Vector3 p = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        p.z = 0;
        return p;
    }
}