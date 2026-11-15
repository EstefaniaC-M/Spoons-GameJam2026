using Cinemachine;
using UnityEngine;

[RequireComponent(typeof(CinemachineVirtualCamera))]
public class RoomCameraPan : MonoBehaviour
{
    [SerializeField] private CinemachineBrain brain;

    [Tooltip("Fraction of screen width where the camera stays still.")]
    [SerializeField, Range(0.1f, 0.95f)]
    private float deadZoneWidth = 0.6f;

    [Tooltip("Horizontal rotation speed in degrees per second.")]
    [SerializeField, Min(0f)]
    private float panSpeed = 45f;

    private CinemachineVirtualCamera roomCamera;
    private CinemachinePOV pov;
    private float fixedVerticalAngle;

    private void Awake()
    {
        roomCamera = GetComponent<CinemachineVirtualCamera>();
        pov = roomCamera.GetCinemachineComponent<CinemachinePOV>();

        if (pov == null)
        {
            Debug.LogError("RoomCamera needs Aim set to POV.", this);
            enabled = false;
            return;
        }

        // Replace automatic mouse look with our own control.
        pov.m_HorizontalAxis.m_InputAxisName = "";
        pov.m_VerticalAxis.m_InputAxisName = "";

        pov.m_HorizontalAxis.Reset();
        pov.m_VerticalAxis.Reset();

        fixedVerticalAngle = pov.m_VerticalAxis.Value;
    }

    private void Update()
    {
        // Only control the room view when it is fully active.
        if (brain == null ||
            brain.IsBlending ||
            brain.ActiveVirtualCamera != roomCamera)
        {
            return;
        }

        if (!Application.isFocused)
            return;

        // Hold the vertical viewing angle steady.
        pov.m_VerticalAxis.Value = fixedVerticalAngle;

        Vector3 mouse = Input.mousePosition;

        // Ignore the cursor when outside the game window.
        if (mouse.x < 0f || mouse.x > Screen.width ||
            mouse.y < 0f || mouse.y > Screen.height)
        {
            return;
        }

        // Convert horizontal cursor position to a value from 0 to 1.
        float mouseX = mouse.x / Screen.width;

        float leftBoundary = (1f - deadZoneWidth) / 2f;
        float rightBoundary = 1f - leftBoundary;

        float direction = 0f;

        if (mouseX < leftBoundary)
            direction = -1f;
        else if (mouseX > rightBoundary)
            direction = 1f;

        float newAngle = pov.m_HorizontalAxis.Value
            + direction * panSpeed * Time.deltaTime;

        pov.m_HorizontalAxis.Value = Mathf.Clamp(
            newAngle,
            pov.m_HorizontalAxis.m_MinValue,
            pov.m_HorizontalAxis.m_MaxValue);
    }
}