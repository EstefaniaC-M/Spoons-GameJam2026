using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class MicroscopeTaskVisibilityController : MonoBehaviour
{
    [SerializeField] private GameObject taskObjectsRoot;

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

    private void OnMouseDown()
    {
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        ShowTask();
    }

    public void ShowTask()
    {
        if (taskObjectsRoot != null)
            taskObjectsRoot.SetActive(true);
    }

    public void HideTask()
    {
        if (taskObjectsRoot != null)
            taskObjectsRoot.SetActive(false);
    }
}
