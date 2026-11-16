using Cinemachine;
using UnityEngine;

public class CameraViewManager : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera roomCamera;
    [SerializeField] private CinemachineBrain brain;
    [SerializeField] private GameObject backButton;

    private CinemachineVirtualCamera currentTaskCamera;
    private MicroscopeTaskVisibilityController currentTaskVisibilityController;
    private CinemachinePOV roomPOV;

    private string horizontalInputName;
    private string verticalInputName;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ReturnToRoom();
        }
    }

    private void Awake()
    {
        roomCamera.Priority = 10;

        roomPOV = roomCamera.GetCinemachineComponent<CinemachinePOV>();

        if (roomPOV != null)
        {
            horizontalInputName = roomPOV.m_HorizontalAxis.m_InputAxisName;
            verticalInputName = roomPOV.m_VerticalAxis.m_InputAxisName;
        }

        if (backButton != null)
            backButton.SetActive(false);
    }

    public void ShowTaskCamera(CinemachineVirtualCamera taskCamera)
    {
        ShowTaskCamera(taskCamera, null);
    }

    public void ShowTaskCamera(
        CinemachineVirtualCamera taskCamera,
        MicroscopeTaskVisibilityController taskVisibilityController)
    {
        // Only enter tasks from the room view.
        if (currentTaskCamera != null)
            return;

        if (taskCamera == null || taskCamera == roomCamera)
            return;

        if (brain != null && brain.IsBlending)
            return;

        if (taskVisibilityController != null && !taskVisibilityController.ShowTask())
            return;

        SetRoomLookEnabled(false);

        currentTaskCamera = taskCamera;
        currentTaskVisibilityController = taskVisibilityController;
        currentTaskCamera.Priority = 20;

        if (backButton != null)
            backButton.SetActive(true);
    }

    public void ReturnToRoom()
    {
        if (currentTaskCamera == null)
            return;

        // RoomCamera's priority of 10 now wins again.
        currentTaskCamera.Priority = 0;
        currentTaskCamera = null;
        if (currentTaskVisibilityController != null)
        {
            currentTaskVisibilityController.HideTask();
            currentTaskVisibilityController = null;
        }

        SetRoomLookEnabled(true);

        if (backButton != null)
            backButton.SetActive(false);
    }

    public void CompleteCurrentTask()
    {
        if (currentTaskVisibilityController != null)
            currentTaskVisibilityController.MarkCompleted();

        ReturnToRoom();
    }

    private void SetRoomLookEnabled(bool allowLook)
    {
        if (roomPOV == null)
            return;

        // Empty axis names stop automatic mouse input.
        roomPOV.m_HorizontalAxis.m_InputAxisName =
            allowLook ? horizontalInputName : "";

        roomPOV.m_VerticalAxis.m_InputAxisName =
            allowLook ? verticalInputName : "";

        // Clear remaining input and rotation momentum.
        roomPOV.m_HorizontalAxis.Reset();
        roomPOV.m_VerticalAxis.Reset();
    }
}