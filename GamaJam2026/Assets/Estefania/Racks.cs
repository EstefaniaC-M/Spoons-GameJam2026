using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rack : MonoBehaviour
{
    public bool isResultRack;
    public Tube[] tubes;
    public Transform[] slots;

    void Start()
    {
        for (int i = 0; i < tubes.Length; i++)
        {
            tubes[i].Setup(slots[i].position, isResultRack);
        }
    }
}