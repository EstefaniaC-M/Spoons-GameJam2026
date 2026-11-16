using System.Collections;
using Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

// Starts when the beast arrives on the watched node (i.e. that node's
// Beast Present Objects get enabled): footsteps -> turn to the map ->
// monster sound -> death menu.
public class DeathSequence : MonoBehaviour
{
    [Header("Trigger")]
    [Tooltip("The sequence starts when the beast reaches this node.")]
    [SerializeField] private TrackerNode deathNode;

    [Header("Camera")]
    [SerializeField] private CameraViewManager cameraManager;
    [Tooltip("The map camera (same one the map's TaskCameraClick uses).")]
    [SerializeField] private CinemachineVirtualCamera mapCamera;
    [Tooltip("Optional. Used to wait for the turn to finish. Defaults to the main camera's brain.")]
    [SerializeField] private CinemachineBrain brain;

    [Header("Audio")]
    [Tooltip("Plays the death sounds. If empty, one is added to this object.")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip footstepsSound;
    [SerializeField] private AudioClip monsterSound;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [Tooltip("Stopped and disabled when the sequence starts (music, ambience, etc.).")]
    [SerializeField] private AudioSource[] audioToTurnOff = new AudioSource[0];

    [Header("Timing")]
    [Tooltip("Extra seconds after the footsteps finish, before turning.")]
    [SerializeField, Min(0f)] private float delayAfterFootsteps = 0f;
    [Tooltip("Seconds to hold on the map after the turn, before the monster sound.")]
    [SerializeField, Min(0f)] private float delayBeforeMonster = 0.5f;
    [Tooltip("Extra seconds after the monster sound finishes, before loading the scene.")]
    [SerializeField, Min(0f)] private float delayBeforeLoad = 0f;

    [Header("Scene")]
    [Tooltip("Must be added to File > Build Settings.")]
    [SerializeField] private string deathMenuScene = "Death Menu";

    private bool triggered;

    public bool IsPlaying => triggered;

    private void Update()
    {
        if (!triggered && deathNode != null && deathNode.HasBeast)
            Play();
    }

    // Also callable from a UnityEvent (e.g. BeastTracker's On Player Reached).
    public void Play()
    {
        if (triggered)
            return;
        triggered = true;
        StartCoroutine(Sequence());
    }

    private IEnumerator Sequence()
    {
        TurnOffAudio();

        // 1. Footsteps.
        yield return PlayAndWait(footstepsSound);
        if (delayAfterFootsteps > 0f)
            yield return new WaitForSeconds(delayAfterFootsteps);

        // 2. Turn to the map and lock the camera there.
        if (cameraManager != null && mapCamera != null)
        {
            cameraManager.LockToCamera(mapCamera);

            if (brain == null && Camera.main != null)
                brain = Camera.main.GetComponent<CinemachineBrain>();

            // The brain only starts the blend on its next update.
            yield return null;
            while (brain != null && brain.IsBlending)
                yield return null;
        }
        else
        {
            Debug.LogWarning("DeathSequence: assign Camera Manager and Map Camera to turn to the map.", this);
        }

        if (delayBeforeMonster > 0f)
            yield return new WaitForSeconds(delayBeforeMonster);

        // 3. Monster.
        yield return PlayAndWait(monsterSound);
        if (delayBeforeLoad > 0f)
            yield return new WaitForSeconds(delayBeforeLoad);

        // 4. Death menu.
        if (string.IsNullOrEmpty(deathMenuScene))
            Debug.LogError("DeathSequence: Death Menu Scene is empty.", this);
        else
            SceneManager.LoadScene(deathMenuScene);
    }

    private void TurnOffAudio()
    {
        if (audioToTurnOff == null)
            return;

        foreach (AudioSource source in audioToTurnOff)
        {
            // Skip our own source so the death sounds still play.
            if (source == null || source == audioSource)
                continue;
            source.Stop();
            source.enabled = false; // Stops anything that would restart it.
        }
    }

    private IEnumerator PlayAndWait(AudioClip clip)
    {
        if (clip == null)
            yield break;

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        audioSource.PlayOneShot(clip, volume);
        yield return new WaitForSeconds(clip.length);
    }
}
