using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// Put this on a placeholder now, then on a clickable 3D node later.
[DisallowMultipleComponent]
public class TrackerNode : MonoBehaviour
{
    [Header("Graph")]
    [Tooltip("Drag in the nodes this one has a corridor to. Only one side needs to list it.")]
    [SerializeField] private TrackerNode[] neighbors = new TrackerNode[0];

    public IReadOnlyList<TrackerNode> Neighbors => neighbors ?? new TrackerNode[0];

    [Header("Visuals")]
    [Tooltip("The node renderer used to show beast, player, and light colors.")]
    [SerializeField] private Renderer lightIndicator;
    [Header("Node colors")]
    [SerializeField] private Color beastColor = Color.red;
    [SerializeField] private Color litColor = new Color(1f, 0.75f, 0.1f);
    [Tooltip("Glow strength for state colors on emissive materials.")]
    [SerializeField, Min(0f)] private float emissionIntensity = 2f;
    [Tooltip("Optional actual light to turn on at this location.")]
    [SerializeField] private Light roomLight;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    private BeastTracker owner;
    private Material indicatorMaterial;
    private Color unlitColor;
    private Color unlitEmission;
    private bool hasEmission;

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
            unlitColor = GetBaseColor();
            hasEmission = indicatorMaterial.HasProperty(EmissionId);
            if (hasEmission)
                unlitEmission = indicatorMaterial.GetColor(EmissionId);
        }
        else
        {
            Debug.LogWarning(name + ": no Renderer found, so this node can't change color. " +
                "Assign Light Indicator.", this);
        }
    }

    private Color GetBaseColor()
    {
        if (indicatorMaterial.HasProperty(BaseColorId))
            return indicatorMaterial.GetColor(BaseColorId);
        if (indicatorMaterial.HasProperty(ColorId))
            return indicatorMaterial.GetColor(ColorId);
        return Color.white;
    }

    // Works with URP (_BaseColor) and Built-in (_Color), and recolors the glow.
    private void ApplyColor(Color baseColor, Color emission)
    {
        if (indicatorMaterial.HasProperty(BaseColorId))
            indicatorMaterial.SetColor(BaseColorId, baseColor);
        if (indicatorMaterial.HasProperty(ColorId))
            indicatorMaterial.SetColor(ColorId, baseColor);
        if (hasEmission)
        {
            indicatorMaterial.EnableKeyword("_EMISSION");
            indicatorMaterial.SetColor(EmissionId, emission);
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
            ApplyColor(beastColor, beastColor * emissionIntensity);
        else if (IsLit)
            ApplyColor(litColor, litColor * emissionIntensity);
        else
            ApplyColor(unlitColor, unlitEmission);
    }

    private void OnDestroy()
    {
        if (indicatorMaterial != null)
            Destroy(indicatorMaterial);
    }

    // Scene-view check: every corridor from the Neighbors list shows as a line.
    private void OnDrawGizmos()
    {
        if (neighbors == null)
            return;
        Gizmos.color = new Color(0.3f, 0.8f, 0.6f);
        foreach (TrackerNode neighbor in neighbors)
        {
            if (neighbor != null && neighbor != this)
                Gizmos.DrawLine(transform.position, neighbor.transform.position);
        }
    }
}
