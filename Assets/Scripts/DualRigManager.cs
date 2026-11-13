using UnityEngine;

/// <summary>
/// Chooses between the XR rig and the desktop fallback rig. On Android the XR
/// rig always wins; in the editor Tab toggles so the XR Device Simulator can be
/// used for testing. Only one AudioListener is ever active.
/// </summary>
public class DualRigManager : MonoBehaviour
{
    public enum ActiveRigMode
    {
        FirstPersonWASD,
        XRSimulatedVR
    }

    [Header("Rig References")]
    public GameObject firstPersonRig;
    public GameObject xrOriginRig;
    public GameObject xrDeviceSimulator;

    [Header("Configuration")]
    public ActiveRigMode activeMode = ActiveRigMode.FirstPersonWASD;

    public ActiveRigMode CurrentMode { get { return activeMode; } }

    private void Awake()
    {
        ResolveReferences();

        ActiveRigMode startMode = ActiveRigMode.FirstPersonWASD;
#if UNITY_ANDROID && !UNITY_EDITOR
        startMode = ActiveRigMode.XRSimulatedVR;
#endif
        ApplyActiveMode(startMode);
    }

    private void ResolveReferences()
    {
        if (firstPersonRig == null) firstPersonRig = GameObject.Find("FirstPersonRig");
        if (xrOriginRig == null)
        {
            xrOriginRig = GameObject.Find("XROriginRig");
            if (xrOriginRig == null) xrOriginRig = GameObject.Find("XR Origin (XR Rig)");
        }
        if (xrDeviceSimulator == null) xrDeviceSimulator = GameObject.Find("XR Device Simulator");
    }

    private void Update()
    {
        // Editor / desktop convenience only. Tab is not bound on device.
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ActiveRigMode next = activeMode == ActiveRigMode.FirstPersonWASD
                ? ActiveRigMode.XRSimulatedVR
                : ActiveRigMode.FirstPersonWASD;
            ApplyActiveMode(next);
        }
    }

    public void ApplyActiveMode(ActiveRigMode mode)
    {
        bool wasFirstPerson = activeMode == ActiveRigMode.FirstPersonWASD;
        activeMode = mode;
        bool isFirstPerson = mode == ActiveRigMode.FirstPersonWASD;

        if (wasFirstPerson != isFirstPerson && firstPersonRig != null && xrOriginRig != null)
        {
            if (isFirstPerson)
            {
                firstPersonRig.transform.position = xrOriginRig.transform.position;
                firstPersonRig.transform.rotation = xrOriginRig.transform.rotation;
            }
            else
            {
                xrOriginRig.transform.position = firstPersonRig.transform.position;
                xrOriginRig.transform.rotation = firstPersonRig.transform.rotation;
            }
        }

        if (firstPersonRig != null) SetRigActive(firstPersonRig, isFirstPerson);
        if (xrOriginRig != null) SetRigActive(xrOriginRig, !isFirstPerson);

#if UNITY_EDITOR
        if (xrDeviceSimulator != null) xrDeviceSimulator.SetActive(!isFirstPerson);
#else
        if (xrDeviceSimulator != null) xrDeviceSimulator.SetActive(false);
#endif
    }

    private static void SetRigActive(GameObject rig, bool active)
    {
        AudioListener listener = rig.GetComponentInChildren<AudioListener>(true);
        if (listener != null) listener.enabled = active;
        rig.SetActive(active);
    }

    /// <summary>Teleport both rigs to a new spot without loading a scene.</summary>
    public void Respawn(Vector3 position, Quaternion rotation)
    {
        MoveRig(firstPersonRig, position, rotation);
        MoveRig(xrOriginRig, position, rotation);
    }

    private static void MoveRig(GameObject rig, Vector3 position, Quaternion rotation)
    {
        if (rig == null) return;

        CharacterController controller = rig.GetComponentInChildren<CharacterController>(true);
        bool hadController = controller != null && controller.enabled;
        if (hadController) controller.enabled = false;

        rig.transform.SetPositionAndRotation(position, rotation);

        if (hadController) controller.enabled = true;
    }
}
