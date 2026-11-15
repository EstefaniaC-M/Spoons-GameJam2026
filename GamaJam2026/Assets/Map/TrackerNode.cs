using UnityEngine;
using UnityEngine.EventSystems;

// Put this on a placeholder now, then on a clickable 3D node later.
[DisallowMultipleComponent]
public class TrackerNode : MonoBehaviour
{
    [Tooltip("The node renderer used to show beast, player, and light colors.")]
    [SerializeField] private Renderer lightIndicator;
    [Header("Node colors")]
    [SerializeField] private Color beastColor = Color.red;
    [SerializeField] private Color playerColor = Color.cyan;
    [SerializeField] private Color litColor = new Color(1f, 0.75f, 0.1f);
    [Tooltip("Optional actual light to turn on at this location.")]
    [SerializeField] private Light roomLight;

    private BeastTracker owner;
    private Material indicatorMaterial;
    private Color unlitColor;

    public bool IsLit { get; private set; }
    public bool HasBeast { get; private set; }
    public bool HasPlayer { get; private set; }

    private void Awake()
    {
        if (lightIndicator == null)
            lightIndicator = GetComponent<Renderer>();
        if (lightIndicator != null)
        {
            // Use a private material instance; leave the shared asset unchanged.
            indicatorMaterial = lightIndicator.material;
            unlitColor = indicatorMaterial.color;
        }
    }

    public void Bind(BeastTracker tracker)
    {
        owner = tracker;
        HasBeast = false;
        HasPlayer = false;
        SetLit(false);
    }

    private void OnMouseDown()
    {
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
            return;

        TurnOnLight();
    }

    // Also usable by a future UI button or another interaction system.
    public void TurnOnLight()
    {
        if (owner != null)
            owner.TryActivateLight(this);
    }

    public void SetLit(bool lit)
    {
        IsLit = lit;
        if (roomLight != null)
            roomLight.enabled = lit;
        UpdateAppearance();
    }

    public void SetBeastPresent(bool present)
    {
        HasBeast = present;
        UpdateAppearance();
    }

    public void SetPlayerPresent(bool present)
    {
        HasPlayer = present;
        UpdateAppearance();
    }

    private void UpdateAppearance()
    {
        if (indicatorMaterial == null)
            return;

        // One place owns the color, so a light timer cannot erase occupancy.
        if (HasBeast)
            indicatorMaterial.color = beastColor;
        else if (HasPlayer)
            indicatorMaterial.color = playerColor;
        else if (IsLit)
            indicatorMaterial.color = litColor;
        else
            indicatorMaterial.color = unlitColor;
    }

    private void OnDestroy()
    {
        if (indicatorMaterial != null)
            Destroy(indicatorMaterial);
    }
}
