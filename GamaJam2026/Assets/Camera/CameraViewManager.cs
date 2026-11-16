using System.Collections;
using Cinemachine;
using UnityEngine;

public class CameraViewManager : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera roomCamera;
    [SerializeField] private CinemachineBrain brain;
    [SerializeField] private GameObject backButton;

    private CinemachineVirtualCamera currentTaskCamera;
    private GameObject currentMinigame;
    private MicroscopeTaskVisibilityController currentTaskVisibilityController;
    private Coroutine openMinigameRoutine;
    private CinemachinePOV roomPOV;
    private bool locked;

    private string horizontalInputName;
    private string verticalInputName;

    public bool IsInTask => currentTaskCamera != null;

    private void Update()
    {
        if (!locked && Input.GetKeyDown(KeyCode.Alpha1))
            ReturnToRoom();
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
        ShowTaskCamera(taskCamera, (GameObject)null);
    }

    public void ShowTaskCamera(CinemachineVirtualCamera taskCamera, GameObject minigame)
    {
        ShowTaskCamera(taskCamera, minigame, null);
    }

    public void ShowTaskCamera(
        CinemachineVirtualCamera taskCamera,
        MicroscopeTaskVisibilityController taskVisibilityController)
    {
        ShowTaskCamera(taskCamera, null, taskVisibilityController);
    }

    private void ShowTaskCamera(
        CinemachineVirtualCamera taskCamera,
        GameObject minigame,
        MicroscopeTaskVisibilityController taskVisibilityController)
    {
        if (locked || currentTaskCamera != null)
            return;

        if (taskCamera == null || taskCamera == roomCamera)
            return;

        if (brain != null && brain.IsBlending)
            return;

        if (taskVisibilityController != null && taskVisibilityController.IsCompleted)
            return;

        SetRoomLookEnabled(false);

        currentTaskCamera = taskCamera;
        currentMinigame = minigame;
        currentTaskVisibilityController = taskVisibilityController;
        currentTaskCamera.Priority = 20;

        if (backButton != null)
            backButton.SetActive(true);

        if (minigame != null || taskVisibilityController != null)
            openMinigameRoutine =
                StartCoroutine(OpenMinigameAfterBlend(minigame, taskVisibilityController));
    }

    private IEnumerator OpenMinigameAfterBlend(
        GameObject minigame,
        MicroscopeTaskVisibilityController taskVisibilityController)
    {
        // The brain starts the blend on its next update, so wait a frame first.
        yield return null;

        while (brain != null && brain.IsBlending)
            yield return null;

        if (currentTaskCamera == null || locked)
        {
            openMinigameRoutine = null;
            yield break;
        }

        if (taskVisibilityController != null && !taskVisibilityController.ShowTask())
        {
            openMinigameRoutine = null;
            ReturnToRoom();
            yield break;
        }

        if (minigame != null)
            minigame.SetActive(true);

        openMinigameRoutine = null;
    }

    public void LockToCamera(CinemachineVirtualCamera camera)
    {
        locked = true;
        CloseMinigame();
        SetRoomLookEnabled(false);

        if (backButton != null)
            backButton.SetActive(false);

        if (currentTaskCamera != null && currentTaskCamera != camera)
            currentTaskCamera.Priority = 0;

        currentTaskCamera = camera;
        if (camera != null)
            camera.Priority = 20;
    }

    public void ReturnToRoom()
    {
        if (locked || currentTaskCamera == null)
            return;

        CloseMinigame();

        // RoomCamera's priority of 10 now wins again.
        currentTaskCamera.Priority = 0;
        currentTaskCamera = null;

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

    private void CloseMinigame()
    {
        if (openMinigameRoutine != null)
        {
            StopCoroutine(openMinigameRoutine);
            openMinigameRoutine = null;
        }

        if (currentTaskVisibilityController != null)
        {
            currentTaskVisibilityController.HideTask();
            currentTaskVisibilityController = null;
        }

        if (currentMinigame != null)
        {
            currentMinigame.SetActive(false);
            currentMinigame = null;
        }
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
