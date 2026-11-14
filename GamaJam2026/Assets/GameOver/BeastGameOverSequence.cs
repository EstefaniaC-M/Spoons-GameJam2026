using System.Collections;
using Cinemachine;
using UnityEngine;

// Attach to an active GameOverController object, outside any hidden task UI.
public class BeastGameOverSequence : MonoBehaviour
{
    [Header("Cinemachine")]
    [SerializeField] private CinemachineBrain brain;
    [SerializeField] private CinemachineVirtualCamera roomCamera;
    [SerializeField] private CinemachineVirtualCamera gameOverCamera;
    [SerializeField] private Transform doorLookPoint;

    [Header("Beast entrance")]
    [SerializeField] private Transform beast;
    [SerializeField] private Transform beastStartPoint;
    [SerializeField] private Transform beastEndPoint;

    [Header("Optional door hinge")]
    [SerializeField] private Transform doorHinge;
    [SerializeField] private float doorOpenAngle = 90f;

    [Header("Timing (seconds)")]
    [SerializeField, Min(0.01f)] private float cameraPanDuration = 1.2f;
    [SerializeField, Min(0f)] private float pauseAtDoor = 0.25f;
    [SerializeField, Min(0.01f)] private float doorOpenDuration = 0.2f;
    [SerializeField, Min(0.01f)] private float beastChargeDuration = 0.6f;
    [SerializeField, Min(0f)] private float pauseBeforeGameOver = 0.4f;

    [Header("Disable player controls during the ending")]
    [Tooltip("Assign CameraViewManager, RoomCameraEdgePan, and any active minigame control components.")]
    [SerializeField] private Behaviour[] controlsToDisable;
    [Tooltip("Optional task UI panels to hide when the ending begins.")]
    [SerializeField] private GameObject[] taskPanelsToHide;

    [Header("Game over")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private bool pauseGameAtEnd = true;

    private bool started;

    private void Awake()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        if (beast != null)
            beast.gameObject.SetActive(false);
        if (gameOverCamera != null)
            gameOverCamera.Priority = 0;
    }

    // Connect BeastTracker > On Player Reached to this method.
    public void PlaySequence()
    {
        if (started)
            return;
        if (!Application.isPlaying || !isActiveAndEnabled)
            return;

        if (brain == null || roomCamera == null || gameOverCamera == null ||
            doorLookPoint == null || beast == null || beastStartPoint == null ||
            beastEndPoint == null || gameOverPanel == null)
        {
            Debug.LogError("BeastGameOverSequence: assign the camera, entrance, and Game Over Panel fields.", this);
            return;
        }
        if (roomCamera == gameOverCamera || !brain.isActiveAndEnabled)
        {
            Debug.LogError("Use a separate GameOverCamera and an enabled Cinemachine Brain.", this);
            return;
        }

        started = true;
        DisablePlayerControls();
        StartCoroutine(RunSequence());
    }

    private void DisablePlayerControls()
    {
        if (controlsToDisable != null)
        {
            foreach (Behaviour control in controlsToDisable)
            {
                // Keep the sequence and its rendering components running.
                if (control == null || control == this || control == brain ||
                    control == roomCamera || control == gameOverCamera || control is Camera)
                    continue;
                control.enabled = false;
            }
        }

        if (taskPanelsToHide != null)
        {
            foreach (GameObject panel in taskPanelsToHide)
            {
                if (panel != null && !transform.IsChildOf(panel.transform))
                    panel.SetActive(false);
            }
        }
    }

    private IEnumerator RunSequence()
    {
        // Copy the room's view, including the last POV viewing direction.
        // This camera must have Body/Aim/Noise set to Do Nothing/Do Nothing/None.
        gameOverCamera.transform.SetPositionAndRotation(
            roomCamera.State.FinalPosition,
            roomCamera.State.FinalOrientation);
        gameOverCamera.m_Lens = roomCamera.m_Lens;
        gameOverCamera.gameObject.SetActive(true);
        gameOverCamera.enabled = true;

        // Keep the ending and its camera blend running if a task paused time.
        brain.m_IgnoreTimeScale = true;

        // Beat every other registered camera's priority.
        int priority = 1000;
        for (int i = 0; i < CinemachineCore.Instance.VirtualCameraCount; i++)
        {
            CinemachineVirtualCameraBase other = CinemachineCore.Instance.GetVirtualCamera(i);
            if (other != gameOverCamera)
                priority = Mathf.Max(priority, other.Priority + 1);
        }
        gameOverCamera.Priority = priority;

        // Give the Brain a LateUpdate to begin the return-to-room blend.
        yield return null;
        while (brain.IsBlending)
            yield return null;

        if (brain.ActiveVirtualCamera != gameOverCamera)
        {
            Debug.LogError("GameOverCamera did not take control. Check Brain filtering, Solo, and Timeline overrides.", this);
            FinishGameOver();
            yield break;
        }

        Vector3 lookDirection = doorLookPoint.position - gameOverCamera.transform.position;
        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion from = gameOverCamera.transform.rotation;
            Quaternion to = Quaternion.LookRotation(lookDirection, Vector3.up);
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, cameraPanDuration);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                gameOverCamera.transform.rotation = Quaternion.Slerp(from, to, t);
                yield return null;
            }
            gameOverCamera.transform.rotation = to;
        }

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, pauseAtDoor));

        if (doorHinge != null)
        {
            Quaternion closed = doorHinge.localRotation;
            Quaternion open = closed * Quaternion.Euler(0f, doorOpenAngle, 0f);
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, doorOpenDuration);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                doorHinge.localRotation = Quaternion.Slerp(closed, open, t);
                yield return null;
            }
            doorHinge.localRotation = open;
        }

        // A separate room capsule, not the tracker's node/marker object.
        beast.SetPositionAndRotation(beastStartPoint.position, beastStartPoint.rotation);
        beast.gameObject.SetActive(true);
        float travel = 0f;
        float chargeDuration = Mathf.Max(0.01f, beastChargeDuration);
        while (travel < chargeDuration)
        {
            travel += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(travel / chargeDuration);
            // Accelerating movement gives the entrance a quick rush.
            beast.position = Vector3.Lerp(beastStartPoint.position, beastEndPoint.position, t * t);
            yield return null;
        }
        beast.position = beastEndPoint.position;

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, pauseBeforeGameOver));
        FinishGameOver();
    }

    private void FinishGameOver()
    {
        gameOverPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (pauseGameAtEnd)
            Time.timeScale = 0f;
    }

    [ContextMenu("Test Ending (Play mode)")]
    private void TestEnding()
    {
        PlaySequence();
    }
}
