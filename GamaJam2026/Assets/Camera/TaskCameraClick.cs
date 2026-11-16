using Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;

public class TaskCameraClick : MonoBehaviour
{
    [SerializeField] private CameraViewManager cameraManager;
    [SerializeField] private CinemachineVirtualCamera taskCamera;
    [SerializeField] private MicroscopeTaskVisibilityController taskVisibilityController;

    private void OnMouseDown()
    {
        // Don't select a task through a UI button.
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (cameraManager != null)
            cameraManager.ShowTaskCamera(taskCamera, taskVisibilityController);
    }
}