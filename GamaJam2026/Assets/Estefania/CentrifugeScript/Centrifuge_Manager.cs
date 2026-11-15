using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

public class Centrifuge_Manager : MonoBehaviour
{
    public Transform[] slots;      // Top, Right, Bottom, Left
    public Tube[] tubes;
    public Button startButton;
    public Transform rotor;        
    public Spin rotorSpin;         
    public float snapDistance = 1f;

    public float orbitSpeed = 800f;  
    public float speedUp = 300f;  

    Tube[] occupant;

    bool spinning = false;
    float currentSpeed = 40f;
    float spinAngle = 0f;
    Vector2 center;
    float radiusX, radiusY;
    Vector2[] startNorm;

    void Awake()
    {
        occupant = new Tube[slots.Length];
    }

    void Start()
    {
        foreach (Tube t in tubes)
        {
            t.homePos = t.transform.position;
            t.manager = this;
        }

        startButton.onClick.AddListener(StartCentrifuge);
        startButton.interactable = false;
    }

    public void PlaceTube(Tube t)
    {
        for (int i = 0; i < occupant.Length; i++)
            if (occupant[i] == t) occupant[i] = null;

        int best = -1;
        float bestDist = snapDistance;
        for (int i = 0; i < slots.Length; i++)
        {
            if (occupant[i] != null) continue;
            float d = Vector2.Distance(t.transform.position, slots[i].position);
            if (d < bestDist) { best = i; bestDist = d; }
        }

        if (best >= 0)
        {
            occupant[best] = t;
            t.transform.position = slots[best].position;
            t.ShowAsCap();
        }
        else
        {
            t.transform.position = t.homePos;
            t.ShowAsVial();
        }

        CheckBalance();
    }

    int WeightAt(int i)
    {
        return occupant[i] == null ? 0 : occupant[i].weight;
    }

    int OppositeOf(int i)
    {
        Vector2 c = Vector2.zero;
        foreach (Transform s in slots) c += (Vector2)s.position;
        c /= slots.Length;

        // the point on the other side of the middle
        Vector2 mirror = 2f * c - (Vector2)slots[i].position;

        int best = i;
        float bestD = float.MaxValue;
        for (int j = 0; j < slots.Length; j++)
        {
            if (j == i) continue;
            float d = Vector2.Distance(mirror, slots[j].position);
            if (d < bestD) { bestD = d; best = j; }
        }
        return best;
    }

    void CheckBalance()
    {
        int placed = 0;
        bool balanced = true;
        string log = "";

        for (int i = 0; i < slots.Length; i++)
        {
            if (occupant[i] != null) placed++;
            if (WeightAt(i) != WeightAt(OppositeOf(i))) balanced = false;
            log += slots[i].name + "=" + WeightAt(i) + "   ";
        }

        bool allPlaced = placed == slots.Length;
        Debug.Log(log + "-> balanced = " + balanced + ", all placed = " + allPlaced);

        startButton.interactable = balanced && allPlaced;
    }

    public void StartCentrifuge()
    {
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        foreach (Transform s in slots)
        {
            minX = Mathf.Min(minX, s.position.x);
            maxX = Mathf.Max(maxX, s.position.x);
            minY = Mathf.Min(minY, s.position.y);
            maxY = Mathf.Max(maxY, s.position.y);
        }
        center = new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);
        radiusX = Mathf.Max((maxX - minX) / 2f, 0.01f);
        radiusY = Mathf.Max((maxY - minY) / 2f, 0.01f);

        startNorm = new Vector2[occupant.Length];
        for (int i = 0; i < occupant.Length; i++)
        {
            if (occupant[i] == null) continue;

            occupant[i].ShowAsCap();
            occupant[i].locked = true;

            Vector2 p = occupant[i].transform.position;
            startNorm[i] = new Vector2((p.x - center.x) / radiusX, (p.y - center.y) / radiusY);
        }

        spinAngle = 0f;
        currentSpeed = 0f;
        startButton.interactable = false;
        spinning = true;
    }

    void Update()
    {
        if (!spinning) return;

        currentSpeed = Mathf.MoveTowards(currentSpeed, orbitSpeed, speedUp * Time.deltaTime);
        spinAngle += currentSpeed * Time.deltaTime;

        float r = spinAngle * Mathf.Deg2Rad;
        float cos = Mathf.Cos(r);
        float sin = Mathf.Sin(r);

        for (int i = 0; i < occupant.Length; i++)
        {
            if (occupant[i] == null) continue;

            Vector2 n = startNorm[i];
            float nx = n.x * cos - n.y * sin;
            float ny = n.x * sin + n.y * cos;

            occupant[i].transform.position = new Vector3(
                center.x + nx * radiusX,
                center.y + ny * radiusY,
                occupant[i].transform.position.z);
        }
    }
}