using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;

public class MicroscopeTaskController : MonoBehaviour
{
    [Header("Sample Placement")]
    [FormerlySerializedAs("focusPopup")]
    [SerializeField] private GameObject microscopeTaskPanel;
    [SerializeField] private GameObject explorationUI;
    [SerializeField] private CameraViewManager cameraViewManager;

    [Header("Focus")]
    public Slider focusSlider;
    public TMP_Text focusStatus;
    [Range(0f, 100f)] public float resistanceStart = 50f;
    [Range(0f, 100f)] public float resistanceEnd = 75f;
    [Range(0.05f, 1f)] public float resistanceMultiplier = 0.25f;

    [Header("Focus Appearance")]
    [SerializeField] private Image specimenImage;
    [SerializeField] private Shader focusBlurShader;
    [Range(0f, 8f)] [SerializeField] private float maxBlurPixels = 5f;
    [Min(0.01f)] [SerializeField] private float blurDistanceForMaxBlur = 30f;

    [Header("Sample Selection")]
    public Button targetCellButton;
    public TMP_Text resultText;
    [SerializeField] private TMP_Text focusWarningText;
    [Min(0.1f)] [SerializeField] private float focusWarningDuration = 1.5f;

    [Header("Task Settings")]
    [Range(0, 100)] public int correctFocusValue = 67;
    public int focusTolerance = 3;

    private bool samplePlaced;
    private bool isFocused = false;
    private bool taskCompleted = false;
    private float lastFocusValue;
    private Material originalSpecimenMaterial;
    private Material runtimeBlurMaterial;
    private Coroutine focusWarningCoroutine;
    private static readonly int BlurAmountId = Shader.PropertyToID("_BlurAmount");

    private void Start()
    {
        correctFocusValue = Mathf.Clamp(correctFocusValue, 0, 100);
        focusTolerance = Mathf.Max(0, focusTolerance);
        resistanceStart = Mathf.Clamp(resistanceStart, 0f, 100f);
        resistanceEnd = Mathf.Clamp(resistanceEnd, resistanceStart, 100f);
        resistanceMultiplier = Mathf.Clamp(resistanceMultiplier, 0.05f, 1f);
        focusWarningDuration = Mathf.Max(0.1f, focusWarningDuration);

        if (microscopeTaskPanel == null)
            Debug.LogError("MicroscopeTaskController requires a microscope task panel.", this);
        else if (microscopeTaskPanel == gameObject ||
                 transform.IsChildOf(microscopeTaskPanel.transform))
            Debug.LogError("The task controller must remain outside the microscope task panel.", this);
        else
            microscopeTaskPanel.SetActive(false);

        if (focusSlider == null)
        {
            Debug.LogError("MicroscopeTaskController requires a focus slider.", this);
        }
        else
        {
            focusSlider.minValue = 0;
            focusSlider.maxValue = 100;
            focusSlider.wholeNumbers = true;
            focusSlider.SetValueWithoutNotify(0);
            focusSlider.interactable = false;
            lastFocusValue = focusSlider.value;
            focusSlider.onValueChanged.AddListener(HandleFocusChanged);
        }

        if (targetCellButton == null)
        {
            Debug.LogError("MicroscopeTaskController requires a target cell button.", this);
        }
        else
        {
            targetCellButton.interactable = false;
            targetCellButton.onClick.AddListener(SelectTargetCell);
        }

        if (focusWarningText == null)
            Debug.LogError("MicroscopeTaskController requires a focus warning text field.", this);
        else
            focusWarningText.gameObject.SetActive(false);

        if (focusStatus == null)
            Debug.LogError("MicroscopeTaskController requires a focus status text field.", this);
        if (resultText == null)
            Debug.LogError("MicroscopeTaskController requires a result text field.", this);

        InitializeSpecimenBlur();

        SetFocusStatus("Place a sample in the microscope.");
        SetResultText("Place the sample to begin.");
    }

    private void OnDestroy()
    {
        if (focusSlider != null)
            focusSlider.onValueChanged.RemoveListener(HandleFocusChanged);

        if (targetCellButton != null)
            targetCellButton.onClick.RemoveListener(SelectTargetCell);

        if (focusWarningCoroutine != null)
            StopCoroutine(focusWarningCoroutine);

        if (specimenImage != null && runtimeBlurMaterial != null &&
            specimenImage.material == runtimeBlurMaterial)
            specimenImage.material = originalSpecimenMaterial;

        if (runtimeBlurMaterial != null)
            Destroy(runtimeBlurMaterial);
    }

    private void OnDisable()
    {
        if (focusWarningCoroutine != null)
        {
            StopCoroutine(focusWarningCoroutine);
            focusWarningCoroutine = null;
        }

        if (focusWarningText != null)
            focusWarningText.gameObject.SetActive(false);
    }

    internal void OnSampleDropped()
    {
        if (taskCompleted || samplePlaced)
            return;

        samplePlaced = true;
        if (microscopeTaskPanel != null)
            microscopeTaskPanel.SetActive(true);
        if (focusSlider != null)
            focusSlider.interactable = true;
        if (targetCellButton != null)
            targetCellButton.interactable = true;

        SetFocusStatus("Sample placed. Adjust the focus.");
        SetResultText("Find the focus point, then select the target cell.");
    }

    private void HandleFocusChanged(float requestedValue)
    {
        if (taskCompleted || !samplePlaced || focusSlider == null)
            return;

        float adjustedValue = ApplyFocusResistance(lastFocusValue, requestedValue);
        focusSlider.SetValueWithoutNotify(adjustedValue);
        lastFocusValue = focusSlider.value;
        UpdateFocusState(lastFocusValue);
    }

    private float ApplyFocusResistance(float previousValue, float requestedValue)
    {
        float movementMin = Mathf.Min(previousValue, requestedValue);
        float movementMax = Mathf.Max(previousValue, requestedValue);
        bool crossesResistanceBand =
            movementMax >= resistanceStart && movementMin <= resistanceEnd;

        if (!crossesResistanceBand)
            return requestedValue;

        return previousValue + (requestedValue - previousValue) * resistanceMultiplier;
    }

    private void UpdateFocusState(float value)
    {
        isFocused = samplePlaced &&
                    Mathf.Abs(value - correctFocusValue) <= focusTolerance;

        if (isFocused)
        {
            SetFocusStatus("Specimen is in focus!");
            SetResultText("Select the target cell.");
        }
        else if (value >= resistanceStart && value <= resistanceEnd)
        {
            SetFocusStatus("Focus resistance detected. Turn slowly.");
            SetResultText("The focus knob is resisting. Keep adjusting.");
        }
        else
        {
            SetFocusStatus("Specimen is out of focus.");
            SetResultText("Keep adjusting the focus.");
        }

        UpdateSpecimenBlur(value);
        if (targetCellButton != null)
            targetCellButton.interactable = samplePlaced && !taskCompleted;
    }

    private void InitializeSpecimenBlur()
    {
        if (specimenImage == null)
        {
            Debug.LogError("MicroscopeTaskController requires the specimen image.", this);
            return;
        }

        if (focusBlurShader == null)
        {
            Debug.LogError("Assign the microscope UI blur shader.", this);
            return;
        }

        originalSpecimenMaterial = specimenImage.material;
        runtimeBlurMaterial = new Material(focusBlurShader);
        runtimeBlurMaterial.name = "Microscope Specimen Blur (Runtime)";
        specimenImage.material = runtimeBlurMaterial;
        UpdateSpecimenBlur(focusSlider != null ? focusSlider.value : 0f);
    }

    private void UpdateSpecimenBlur(float focusValue)
    {
        if (runtimeBlurMaterial == null)
            return;

        float distanceFromFocus = Mathf.Abs(focusValue - correctFocusValue);
        float blurProgress = Mathf.Clamp01(
            distanceFromFocus / Mathf.Max(0.01f, blurDistanceForMaxBlur));
        runtimeBlurMaterial.SetFloat(BlurAmountId, blurProgress * maxBlurPixels);
    }

    private void SetFocusStatus(string message)
    {
        if (focusStatus != null)
            focusStatus.text = message;
    }

    private void SetResultText(string message)
    {
        if (resultText != null)
            resultText.text = message;
    }

    private void SelectTargetCell()
    {
        if (!samplePlaced || taskCompleted)
            return;

        if (!isFocused)
        {
            ShowFocusWarning();
            return;
        }

        taskCompleted = true;
        if (targetCellButton != null)
            targetCellButton.interactable = false;
        SetResultText("Correct! Target cell located.");
        SetFocusStatus("Analysis complete!");
        if (microscopeTaskPanel != null)
            microscopeTaskPanel.SetActive(false);
        if (explorationUI != null)
            explorationUI.SetActive(false);
        Debug.Log("Microscope task completed!");

        if (cameraViewManager != null)
            cameraViewManager.CompleteCurrentTask();
        else
            Debug.LogWarning("Assign a Camera View Manager to close the task after completion.", this);
    }

    private void ShowFocusWarning()
    {
        if (focusWarningText == null)
            return;

        if (focusWarningCoroutine != null)
            StopCoroutine(focusWarningCoroutine);

        focusWarningText.text = "The sample is not in focus";
        focusWarningText.gameObject.SetActive(true);
        focusWarningCoroutine = StartCoroutine(HideFocusWarningAfterDelay());
    }

    private IEnumerator HideFocusWarningAfterDelay()
    {
        yield return new WaitForSecondsRealtime(focusWarningDuration);

        if (focusWarningText != null)
            focusWarningText.gameObject.SetActive(false);
        focusWarningCoroutine = null;
    }
}