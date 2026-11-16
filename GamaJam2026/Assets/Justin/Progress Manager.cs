using System.Text;
using TMPro;
using UnityEngine;

public class ProgressManager : MonoBehaviour
{
    // 0 to 100
    [SerializeField] public double CureProgress = 0.0;

    [SerializeField] private TextMeshProUGUI progressText;

    [Header("Bar Settings")]
    [SerializeField] private string label = "PROGRESS:";
    [SerializeField] private int barLength = 44;
    [SerializeField] private char barChar = '/';
    [SerializeField] private Color emptyColor = new Color(0.25f, 0.25f, 0.25f);

    private double lastShownProgress = -1;

    void Start()
    {
        UpdateText();
    }

    void Update()
    {
        // Only rebuild the string when the value actually changes
        if (CureProgress != lastShownProgress)
            UpdateText();
    }

    public void AddProgress(double amount)
    {
        SetProgress(CureProgress + amount);
    }

    public void SetProgress(double value)
    {
        CureProgress = Mathf.Clamp((float)value, 0f, 100f);
    }

    private void UpdateText()
    {
        if (progressText == null) return;

        lastShownProgress = CureProgress;
        float t = (float)(CureProgress / 100.0);

        // Red (hue 0) -> Yellow -> Green (hue 0.33)
        Color fillColor = Color.HSVToRGB(Mathf.Lerp(0f, 0.33f, t), 1f, 1f);
        string fillHex = ColorUtility.ToHtmlStringRGB(fillColor);
        string emptyHex = ColorUtility.ToHtmlStringRGB(emptyColor);

        int filled = Mathf.RoundToInt(t * barLength);

        StringBuilder sb = new StringBuilder();
        sb.Append(label).Append(' ');
        sb.Append("<color=#").Append(fillHex).Append('>');
        sb.Append(barChar, filled);
        sb.Append("</color>");
        sb.Append("<color=#").Append(emptyHex).Append('>');
        sb.Append(barChar, barLength - filled);
        sb.Append("</color>");
        sb.Append("  <color=#").Append(fillHex).Append('>');
        sb.Append(Mathf.FloorToInt(t * 100f)).Append('%');
        sb.Append("</color>");

        progressText.text = sb.ToString();
    }
}
