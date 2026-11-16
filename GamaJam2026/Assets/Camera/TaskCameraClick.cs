using Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;

public class TaskCameraClick : MonoBehaviour
{
    [SerializeField] private CameraViewManager cameraManager;
    [SerializeField] private CinemachineVirtualCamera taskCamera;

    [Tooltip("Optional 2D minigame root shown once the camera has finished moving in.")]
    [SerializeField] private GameObject minigame;

    private void OnMouseDown()
    {
        // Don't select a task through a UI button.
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (cameraManager != null)
            cameraManager.ShowTaskCamera(taskCamera, minigame);
    }
}
