using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
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

    [Header("Embedding in the 3D map")]
    public Camera minigameCamera;          // orthographic camera that renders this minigame
    public float spinDuration = 3f;        // seconds of spinning before the task counts as done
    public UnityEvent onCompleted;         // e.g. mark task done / return to room

    [Header("Progress")]
    public ProgressManager progressManager;          // auto-found if left empty
    public double progressAmount = 100.0 / 3.0;      // 1/3 of the cure bar

    public bool Completed { get; private set; }

    Tube[] occupant;
    float spinTimer = 0f;
    double progressGiven = 0.0;

    bool spinning = false;
    float currentSpeed = 40f;
    float spinAngle = 0f;
    Vector2 center;
    float radiusX, radiusY;
    Vector2[] startNorm;

    void Awake()
    {
        occupant = new Tube[slots.Length];

        // When embedded as a prefab, use the camera that lives inside the prefab.
        // (In the standalone Centrifuge scene there is none, so Camera.main is used.)
        if (minigameCamera == null && transform.parent != null)
            minigameCamera = transform.root.GetComponentInChildren<Camera>(true);
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
        // The button's onClick and our own click check could both fire; only start once.
        if (spinning) return;

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
        spinTimer = 0f;
        progressGiven = 0.0;
        spinning = true;

        if (progressManager == null)
            progressManager = FindObjectOfType<ProgressManager>();
        if (progressManager == null)
            Debug.LogWarning("Centrifuge_Manager: no ProgressManager found in scene.");
    }

    Tube dragging;

    Camera InputCamera()
    {
        return minigameCamera != null ? minigameCamera : Camera.main;
    }

    bool StartButtonUnderMouse()
    {
        if (startButton == null || !startButton.interactable || !startButton.gameObject.activeInHierarchy)
            return false;

        Canvas canvas = startButton.GetComponentInParent<Canvas>();
        Camera uiCam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? canvas.worldCamera
            : null;

        return RectTransformUtility.RectangleContainsScreenPoint(
            (RectTransform)startButton.transform, Input.mousePosition, uiCam);
    }

    void HandleTubeInput()
    {
        Camera cam = InputCamera();
        if (cam == null) return;

        if (Input.GetMouseButtonDown(0) && StartButtonUnderMouse())
        {
            // Handle the start button ourselves so other UI in the map can't swallow the click.
            StartCentrifuge();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 p = cam.ScreenToWorldPoint(Input.mousePosition);
            foreach (Collider2D c in Physics2D.OverlapPointAll(p))
            {
                Tube t = c.GetComponent<Tube>();
                if (t != null && !t.locked && System.Array.IndexOf(tubes, t) >= 0)
                {
                    dragging = t;
                    t.BeginDrag();
                    break;
                }
            }
        }

        if (dragging == null) return;

        if (Input.GetMouseButton(0))
            dragging.Drag();

        if (Input.GetMouseButtonUp(0))
        {
            dragging.EndDrag();
            dragging = null;
        }
    }

    void Update()
    {
        HandleTubeInput();

        if (!spinning) return;

        if (!Completed)
        {
            spinTimer += Time.deltaTime;

            // Fill the progress bar gradually while spinning, reaching exactly progressAmount at the end.
            double target = progressAmount * Mathf.Clamp01(spinTimer / spinDuration);
            if (progressManager != null && target > progressGiven)
            {
                progressManager.AddProgress(target - progressGiven);
                progressGiven = target;
            }

            if (spinTimer >= spinDuration)
            {
                Completed = true;
                onCompleted?.Invoke();
            }
        }

        // Spin up while running, then slow down to a stop once the time is up.
        float targetSpeed = Completed ? 0f : orbitSpeed;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, speedUp * Time.deltaTime);
        if (Completed && currentSpeed <= 0f)
        {
            spinning = false;
            return;
        }
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