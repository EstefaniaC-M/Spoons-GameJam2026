using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Spin : MonoBehaviour
{
    public float speed = 360f; // degrees per second

    void Update()
    {
        transform.Rotate(0f, 0f, speed * Time.deltaTime);
    }
}
