using UnityEngine;
using UnityEngine.Rendering.Universal;

// Put this on the minigame's camera.
// It turns the camera into a URP Overlay camera and adds it to the
// Main Camera's stack, so the 2D minigame draws on top of the 3D room.
[RequireComponent(typeof(Camera))]
public class StackOnMainCamera : MonoBehaviour
{
    [Tooltip("Leave empty to use the camera tagged MainCamera.")]
    [SerializeField] private Camera baseCamera;

    private Camera overlayCamera;

    private void Awake()
    {
        overlayCamera = GetComponent<Camera>();
        overlayCamera.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
    }

    private void OnEnable()
    {
        if (baseCamera == null)
            baseCamera = Camera.main;

        if (baseCamera == null)
        {
            Debug.LogError("StackOnMainCamera: no Main Camera found to stack onto.");
            return;
        }

        var stack = baseCamera.GetUniversalAdditionalCameraData().cameraStack;
        if (!stack.Contains(overlayCamera))
            stack.Add(overlayCamera);
    }

    private void OnDisable()
    {
        if (baseCamera == null)
            return;

        var stack = baseCamera.GetUniversalAdditionalCameraData().cameraStack;
        stack.Remove(overlayCamera);
    }
}
