using System.Collections;
using Cinemachine;
using UnityEngine;

public class CameraViewManager : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera roomCamera;
    [SerializeField] private CinemachineBrain brain;
    [SerializeField] private GameObject backButton;

    private CinemachineVirtualCamera currentTaskCamera;
    private CinemachinePOV roomPOV;

    private GameObject currentMinigame;
    private Coroutine openMinigameRoutine;

    private string horizontalInputName;
    private string verticalInputName;

    // Set by LockToCamera (e.g. the death sequence); blocks all camera changes.
    private bool locked;

    public bool IsInTask => currentTaskCamera != null;

    private void Update()
    {
        if (!locked && Input.GetKeyDown(KeyCode.Alpha1))
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

    public void ShowTaskCamera(CinemachineVirtualCamera taskCamera, GameObject minigame)
    {
        // Only enter tasks from the room view.
        if (locked || currentTaskCamera != null)
            return;

        if (taskCamera == null || taskCamera == roomCamera)
            return;

        if (brain != null && brain.IsBlending)
            return;

        SetRoomLookEnabled(false);

        currentTaskCamera = taskCamera;
        currentTaskCamera.Priority = 20;

        if (backButton != null)
            backButton.SetActive(true);

        if (minigame != null)
        {
            currentMinigame = minigame;
            openMinigameRoutine = StartCoroutine(OpenMinigameAfterBlend(minigame));
        }
    }

    private IEnumerator OpenMinigameAfterBlend(GameObject minigame)
    {
        // The brain only starts the blend on its next update, so wait a frame first.
        yield return null;

        while (brain != null && brain.IsBlending)
            yield return null;

        minigame.SetActive(true);
        openMinigameRoutine = null;
    }

    // Forces the view to this camera from any state (room, another task, mid-blend)
    // and keeps it there: no back button, no look, no other tasks.
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

    private void CloseMinigame()
    {
        if (openMinigameRoutine != null)
        {
            StopCoroutine(openMinigameRoutine);
            openMinigameRoutine = null;
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
