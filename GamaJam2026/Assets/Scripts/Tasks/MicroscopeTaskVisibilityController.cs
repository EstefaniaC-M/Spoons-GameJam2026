using UnityEngine;

public class MicroscopeTaskVisibilityController : MonoBehaviour
{
    [SerializeField] private GameObject taskObjectsRoot;
    private bool taskCompleted;

    private void Awake()
    {
        if (taskObjectsRoot == null)
        {
            Debug.LogError("MicroscopeTaskVisibilityController requires a task objects root.", this);
            return;
        }

        if (taskObjectsRoot == gameObject || transform.IsChildOf(taskObjectsRoot.transform))
        {
            Debug.LogError("The task objects root must not contain its visibility controller.", this);
            return;
        }

        taskObjectsRoot.SetActive(false);
    }

    public bool ShowTask()
    {
        if (taskCompleted || taskObjectsRoot == null)
            return false;

        taskObjectsRoot.SetActive(true);
        return true;
    }

    public void HideTask()
    {
        if (taskObjectsRoot != null)
            taskObjectsRoot.SetActive(false);
    }

    public void MarkCompleted()
    {
        taskCompleted = true;
        HideTask();
    }
}
