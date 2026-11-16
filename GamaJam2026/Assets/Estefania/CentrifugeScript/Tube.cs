using System.Collections;
using System.Collections.Generic;

using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

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

    // centrifuge caps
    public Sprite capSprite;
    public float capScale = 1f;
    public Vector2 capOffset;
    public float capGrabSize = 1f;

    // chemical scene
    public bool hideWhenResting;          // tick for the invisible click targets
    public Sprite heldSprite;             // the big solo tube
    public float heldScale = 1f;
    public float pourAngle = 110f;
    public Vector2 pourOffset = new Vector2(1.2f, 1.5f);
    public float pourTime = 0.8f;

    public SpriteRenderer liquidBottom;   // result tubes only
    public SpriteRenderer liquidTop;      // result tubes only

    private Vector3 offset;
    private SpriteRenderer sr;
    private BoxCollider2D box;

    private Sprite vialSprite;
    private Vector3 vialScale;
    private int vialOrder;
    private Vector2 vialBoxSize;
    private Vector2 vialBoxOffset;

    private bool isCap = false;
    private bool grabbed = false;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        box = GetComponent<BoxCollider2D>();

        if (sr != null)
        {
            vialSprite = sr.sprite;
            vialOrder = sr.sortingOrder;
        }

        vialScale = transform.localScale;

        if (box != null)
        {
            vialBoxSize = box.size;
            vialBoxOffset = box.offset;
        }
    }

    public void Setup(Vector3 slotPosition, bool isResult)
    {
        homePos = slotPosition;
        transform.position = slotPosition;
        isResultTube = isResult;
        locked = isResult;

        sr.color = Color.white;
        sr.enabled = !hideWhenResting;
        ClearLiquid();
    }

    // ---------- liquid layers ----------
    static void SetLiquid(SpriteRenderer r, Color c)
    {
        if (r != null) r.color = c;
    }

    public void ClearLiquid()
    {
        SetLiquid(liquidBottom, Color.clear);
        SetLiquid(liquidTop, Color.clear);
    }

    public void ShowLayers(Color bottom, Color top, bool hasTop)
    {
        if (liquidBottom == null)          // no liquid shapes: tint the tube itself
        {
            sr.enabled = true;
            sr.color = hasTop ? top : bottom;
            return;
        }
        SetLiquid(liquidBottom, bottom);
        SetLiquid(liquidTop, hasTop ? top : Color.clear);
    }

    public void ShowFilled()
    {
        Color c = ChemicalInfo.Get(chemical).color;
        if (liquidBottom == null)
        {
            sr.enabled = true;
            sr.color = c;
            return;
        }
        SetLiquid(liquidBottom, c);
        SetLiquid(liquidTop, c);
    }

    // ---------- centrifuge cap swap ----------
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

    // ---------- chemical scene pop-out ----------
    void PickUp()
    {
        sr.sprite = heldSprite;
        sr.enabled = true;
        transform.localScale = vialScale * heldScale;
        sr.sortingOrder = 50;
    }

    void PutDown()
    {
        sr.sprite = vialSprite;
        sr.enabled = !hideWhenResting;
        transform.localScale = vialScale;
        sr.sortingOrder = vialOrder;
        transform.rotation = Quaternion.identity;
    }

    void OnMouseDown()
    {
        if (locked) return;

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
            return;

        grabbed = true;

        if (manager == null && heldSprite != null)
        {
            PickUp();
            offset = Vector3.zero;
            transform.position = MouseWorld();
        }
        else
        {
            ShowAsVial();
            offset = transform.position - MouseWorld();
        }
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

        if (manager != null)
        {
            manager.PlaceTube(this);
            return;
        }

        Tube target = null;
        foreach (Collider2D hit in Physics2D.OverlapPointAll(MouseWorld()))
        {
            Tube other = hit.GetComponent<Tube>();
            if (other != null && other.isResultTube)
            {
                target = other;
                break;
            }
        }

        if (target != null)
        {
            StartCoroutine(PourInto(target));
        }
        else
        {
            transform.position = homePos;
            PutDown();
        }
    }

    IEnumerator PourInto(Tube target)
    {
        locked = true;

        Vector3 startPos = transform.position;
        Vector3 pourPos = target.transform.position + (Vector3)pourOffset;
        pourPos.z = startPos.z;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.35f;
            float k = Mathf.SmoothStep(0f, 1f, t);
            transform.position = Vector3.Lerp(startPos, pourPos, k);
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, pourAngle, k));
            yield return null;
        }

        yield return new WaitForSeconds(pourTime);

        if (Mixer.Instance != null)
            Mixer.Instance.Pour(this, target);
        else
            Debug.LogError("There is no Mixer in the scene");

        yield return new WaitForSeconds(0.3f);

        Vector3 from = transform.position;
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.35f;
            float k = Mathf.SmoothStep(0f, 1f, t);
            transform.position = Vector3.Lerp(from, homePos, k);
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(pourAngle, 0f, k));
            yield return null;
        }

        transform.position = homePos;
        PutDown();
        locked = false;
    }

    Vector3 MouseWorld()
    {
        Vector3 p = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        p.z = 0;
        return p;
    }
}