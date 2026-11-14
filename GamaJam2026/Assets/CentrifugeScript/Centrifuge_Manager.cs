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

    Tube[] occupant;

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
        Debug.Log("PlaceTube called for " + t.name);

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
        }
        else
        {
            t.transform.position = t.homePos;
        }

        CheckBalance();
    }

    int WeightAt(int i)
    {
        return occupant[i] == null ? 0 : occupant[i].weight;
    }

    void CheckBalance()
    {
        int half = slots.Length / 2;
        bool balanced = true;
        bool anyTube = false;

        for (int i = 0; i < slots.Length; i++)
            if (occupant[i] != null) anyTube = true;

        for (int i = 0; i < half; i++)
            if (WeightAt(i) != WeightAt(i + half)) balanced = false;

        Debug.Log("balanced = " + balanced + ", anyTube = " + anyTube);
        startButton.interactable = balanced && anyTube;
    }


    public void StartCentrifuge()
    {
        Debug.Log("Start pressed");

        for (int i = 0; i < occupant.Length; i++)
        {
            if (occupant[i] != null)
            {
                occupant[i].locked = true;
                occupant[i].transform.SetParent(rotor);
            }
        }
        startButton.interactable = false;
        rotorSpin.enabled = true;
        Debug.Log("Rotor spin enabled");
    }

}